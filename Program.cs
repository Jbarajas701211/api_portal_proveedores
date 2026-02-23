
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApiProveedores.Models;
using ApiProveedores.Services.PubSub;
using ApiProveedores.Helper;
using ApiProveedores.Parser;
using System.Text.Json.Serialization;
using System.Security.Claims;
using ApiProveedores.Http;
using ApiProveedores.Services;
using ApiProveedores.Services.Helper;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ApiProveedores.Http.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ApiProveedores.Services.Reportes;
using ApiProveedores.Services.Citas;
using ApiProveedores.Services.Citas.Validators;

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

var envSecret = Environment.GetEnvironmentVariable("CITAS_API_CORE_JWT_SECRET_KEY");
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
    var keyResource = builder.Configuration["CITAS_API_CORE_LLAVE_CIFRADO"];

    var (projectId, locationId, keyRingId, keyId) = KmsKeyParser.ParseKeyResource(keyResource);
    return new GoogleKmsHelper(projectId, locationId, keyRingId, keyId);
});

builder.Services.AddAuthorization();

builder.Services.AddDbContext<PortalDbContext>(options =>
    options.UseNpgsql($"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}")
);

// configuracion de cache
builder.Services.AddMemoryCache();


// topicos
var topicPnj = Environment.GetEnvironmentVariable("CITAS_API_CORE_PNJ_COLA")
              ?? throw new Exception("CITAS_API_CORE_PNJ_COLA no definida");
var topicResumen = Environment.GetEnvironmentVariable("CITAS_API_CORE_DATA_PROCESSING_COLA")
              ?? throw new Exception("CITAS_API_CORE_DATA_PROCESSING_COLA no definida");
builder.Services.AddScoped<PublisherPnjService>(_ =>
    new PublisherPnjService(topicPnj));
builder.Services.AddScoped<PublisherResumenService>(_ =>
    new PublisherResumenService(topicResumen));


// servicios
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuariosService>();
builder.Services.AddScoped<HelperTraceService>();
builder.Services.AddScoped<ProveedoresService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<NotificacionesService>();
builder.Services.AddScoped<CapacidadService>();
builder.Services.AddScoped<ActualizaResumenService>();
builder.Services.AddScoped<OrdenService>();
builder.Services.AddScoped<DiaNoLaborableService>();
builder.Services.AddScoped<TiendaService>();
builder.Services.AddScoped<CentroDistribucionService>();
builder.Services.AddScoped<ParametroSistemaService>();
builder.Services.AddScoped<EntregaService>();
builder.Services.AddScoped<IncidenciaService>();
builder.Services.AddScoped<DetalleOrdenService>();
builder.Services.AddScoped<OrigenCapacidadService>();
builder.Services.AddScoped<HelperOrdenService>();
builder.Services.AddScoped<HelperCita>();


// Validaciones
builder.Services.AddScoped<RegistroCitaValidator>();
builder.Services.AddScoped<EliminarCitaValidator>();
builder.Services.AddScoped<ActualizarDatosTransportistaCitaValidator>();
builder.Services.AddScoped<RegistroDetalleCitaValidator>();
builder.Services.AddScoped<ActualizarDetalleCitaValidator>(); 
builder.Services.AddScoped<GenerarFolioCitaValidator>();
builder.Services.AddScoped<SolicitarAutorizacionValidator>();
builder.Services.AddScoped<AutorizarDenegarValidator>();
builder.Services.AddScoped<EntregaValidator>();
builder.Services.AddScoped<IncidenciaValidator>();
builder.Services.AddScoped<IncidenciaMasivaValidator>();
builder.Services.AddScoped<IncidenciaSolicitaUrlValidator>();
builder.Services.AddScoped<ActualizarDatosCitaValidator>();
builder.Services.AddScoped<EliminarDetalleCitaValidator>();
builder.Services.AddScoped<CancelacionValidator>();



// Servicios relacionados con la cita.
builder.Services.AddScoped<CitaService>();
builder.Services.AddScoped<DetalleCitaService>();
builder.Services.AddScoped<DatosTransportistaService>();
builder.Services.AddScoped<CantidadesTeoricasService>();


// kpis
builder.Services.AddScoped<KpiProveedoresService>(); 

// Reporteria
builder.Services.AddSingleton<GenericPubSubPublisher>();
builder.Services.AddScoped<ReporteResumenOrdenesService>();
builder.Services.AddScoped<ReporteDetalleOrdenService>();


builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionHandler>();
});


builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
