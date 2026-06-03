using ApiProveedores.Helper;
using ApiProveedores.Http;
using ApiProveedores.Http.Filters;
using ApiProveedores.Interfaces;
using ApiProveedores.Models;
using ApiProveedores.Parser;
using ApiProveedores.Services;
using ApiProveedores.Services.Helper;
using ApiProveedores.Services.PubSub;
using ApiProveedores.Services.Reportes;
using Marti.PortalCitas.Onest.Services.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marti.Pac.Factory.DependencyInjection;
using Marti.Pac.Sw.Configurations;
using Marti.Pac.Sw.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var dbHost = Environment.GetEnvironmentVariable("PROVEEDORES_API_CORE_DB_HOST");
var dbName = Environment.GetEnvironmentVariable("PROVEEDORES_API_CORE_DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("PROVEEDORES_API_CORE_DB_USER");
var dbPass = Environment.GetEnvironmentVariable("PROVEEDORES_API_CORE_DB_PASSWORD");
var dbPort = Environment.GetEnvironmentVariable("PROVEEDORES_API_CORE_DB_PORT");



builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
    options.IncludeScopes = false;
});

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddSimpleConsole();


var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();

var envSecret = Environment.GetEnvironmentVariable("PROVEEDORES_API_CORE_JWT_SECRET_KEY");
if (!string.IsNullOrWhiteSpace(envSecret))
{
    jwtSettings.SecretKey = envSecret;
}

builder.Services.AddSingleton(jwtSettings);


builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddScoped<ApiProveedores.Http.Filters.CustomJwtAuthFilter>();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

});

builder.Services.AddSingleton<GoogleKmsHelper>(sp =>
{
    var keyResource = builder.Configuration["PROVEEDORES_API_CORE_LLAVE_CIFRADO"];

    var (projectId, locationId, keyRingId, keyId) = KmsKeyParser.ParseKeyResource(keyResource);
    return new GoogleKmsHelper(projectId, locationId, keyRingId, keyId);
});

builder.Services.AddAuthorization();

builder.Services.AddDbContext<PortalDbContext>(options =>
    options.UseNpgsql($"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}")
);

// configuracion de cache
builder.Services.AddMemoryCache();


//topicos
var topicPnj = Environment.GetEnvironmentVariable("PROVEEDORES_API_CORE_NOTIFICADOR_COLA")
              ?? throw new Exception("PROVEEDORES_API_CORE_NOTIFICADOR_COLA no definida");
builder.Services.AddScoped<PublisherPnjService>(_ =>
    new PublisherPnjService(topicPnj));

// servicios
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuariosService>();
builder.Services.AddScoped<HelperTraceService>();
builder.Services.AddScoped<ProveedoresService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<NotificacionesService>();
builder.Services.AddScoped<DiaNoLaborableService>();
builder.Services.AddScoped<ParametroSistemaService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<EmpresaService>();
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<FacturaService>();
builder.Services.AddScoped<OrdenCompraService>();
builder.Services.AddScoped<AvisosService>();
builder.Services.AddScoped<GenerarCuerpoEmailHelper>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ReporteService>();

builder.Services.AddSingleton<IServiceHttp, ServiceHttp>();

// HttpClientFactory
builder.Services.AddHttpClient("ApiClientGenerico", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<IServiceHttp, ServiceHttp>();


// Pac (SwOptions: sección Pac:Sw; variables de entorno PAC_* como respaldo)
builder.Services.AddPacServices(builder.Configuration);
builder.Services.PostConfigure<SwOptions>(options =>
{
    var baseUrl = Environment.GetEnvironmentVariable("PAC_URL_BASE");
    if (!string.IsNullOrWhiteSpace(baseUrl))
        options.BaseUrl = baseUrl;

    var user = Environment.GetEnvironmentVariable("PAC_USERNAME");
    if (!string.IsNullOrWhiteSpace(user))
        options.User = user;

    var password = Environment.GetEnvironmentVariable("PAC_PASSWORD");
    if (!string.IsNullOrWhiteSpace(password))
        options.Password = password;
});
builder.Services.AddScoped<SwPacService>();

// Reporteria
builder.Services.AddSingleton<GenericPubSubPublisher>();
builder.Services.AddScoped<ReporteResumenOrdenesService>();


builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionHandler>();
}).AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);


builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Proveedores",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
var app = builder.Build();


if (app.Environment.IsDevelopment() || true) // activa Swagger siempre
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseMiddleware<XUserTokenMiddleware>();
app.UseCors("AllowLocalhost");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
