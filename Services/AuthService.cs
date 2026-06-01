
using ApiProveedores.Dto.Http;
using ApiProveedores.Dto.PubSub;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Helper;
using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Services.Helper;
using ApiProveedores.Services.PubSub;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class AuthService
{
    private readonly HelperTraceService _helperTraceService;
    private readonly GoogleKmsHelper _googleKmsHelper;
    private readonly PublisherPnjService _pubSubPublisher;
    private readonly PortalDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private string _frontUrl;

    public AuthService(PortalDbContext context, PublisherPnjService pubSubPublisher,
        GoogleKmsHelper googleKmsHelper, HelperTraceService helperTraceService, ILogger<AuthService> logger)
    {
        _context = context;
        _pubSubPublisher = pubSubPublisher;
        _googleKmsHelper = googleKmsHelper;
        _frontUrl = Environment.GetEnvironmentVariable("PORTAL_PROVEEDORES_URL");
        _helperTraceService = helperTraceService;
        _logger = logger;
    }
    public async Task<ApiResponseDto<bool>> AltaDeCuenta(AltaCuentaRequest request) {

        try
        {
            _logger.LogInformation("Iniciando proceso de alta de cuenta para email: {Email}", request.Email);
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.CorreoElectronico == request.Email);
            if (user != null)
                return new ApiResponseDto<bool>()
                {
                    Message = "El email ya está registrado.",
                    Success = false,
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Data = false
                };

            if (!string.IsNullOrWhiteSpace(request.RfcProveedor) && request.Rol == RolUsuario.PROVEEDOR)
            {
                var proveedor = await _context.Proveedores.AnyAsync(p => p.Rfc == request.RfcProveedor);
                if (request.Rol == RolUsuario.PROVEEDOR && !proveedor)
                    return new ApiResponseDto<bool>()
                    {
                        Message = "El RFC del proveedor no existe en el sistema.",
                        Success = false,
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        Data = false
                    };
                var userRfc = await _context.Usuarios.AnyAsync(u => u.RfcProveedor == request.RfcProveedor);
                if (userRfc)
                {
                    _logger.LogError("Intento de alta de cuenta con RFC {Rfc} que ya tiene una cuenta asociada.", request.RfcProveedor);
                    return new ApiResponseDto<bool>()
                    {
                        Message = "Ya existe una cuenta asociada al RFC del proveedor.",
                        Success = false,
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        Data = false
                    };
                }
            }

            var usuarioNuevo = new Usuario();
            usuarioNuevo.Nombre = request.Nombre.ToUpperInvariant();
            usuarioNuevo.ApellidoPaterno = request.ApellidoPaterno;
            usuarioNuevo.ApellidoMaterno = request.ApellidoMaterno;
            usuarioNuevo.usuario = request.Usuario;
            usuarioNuevo.CorreoElectronico = request.Email.ToLowerInvariant();
            usuarioNuevo.RfcProveedor = request.RfcProveedor;
            byte[] data = Encoding.UTF8.GetBytes($"{request.Password}");
            byte[] hash = SHA256.HashData(data);
            var hex = Convert.ToHexString(hash).ToLowerInvariant();
            usuarioNuevo.Password = hex;
            usuarioNuevo.Estatus = false;


            // Bloqueo de cuenta
            var codigoActivacion = CodigoHelper.GenerarCodigoAlfanumerico();

            DateTime fechaExpiracion = DateTime.UtcNow.AddHours(2);
            string estampaTiempo = fechaExpiracion.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string resultado = $"{request.Email}|{codigoActivacion}|{estampaTiempo}";
            string sign = await _googleKmsHelper.EncryptAsync(resultado);
            string url = $"{_frontUrl}/activar_cuenta?sign={Uri.EscapeDataString(sign)}";

            //var mensaje = new MessageWrapper<MessageCuentaNueva>
            //{
            //    Payload = new MessageCuentaNueva
            //    {
            //        NombreCompleto = usuarioNuevo.Nombre,
            //        Email = usuarioNuevo.CorreoElectronico,
            //        Code = codigoActivacion,
            //        Url = url

            //    },
            //    TypeNotification = "cuenta_nueva"
            //};

            var mensaje = new NotificacionEmail
            {
                ClaveAplicativo = "app-portal-proveedores",
                NombreTemplate = "alta_cuenta",
                EmailDestino = usuarioNuevo.CorreoElectronico,
                Data = new
                {
                    payload = new
                    {
                        nombre = usuarioNuevo.Nombre,
                        code = codigoActivacion,
                        to = usuarioNuevo.CorreoElectronico,
                        url = url
                    }
                }
            };

            usuarioNuevo.CodigoActivacion = codigoActivacion;
            var userSaved = _context.Usuarios.Add(usuarioNuevo);
            await _context.SaveChangesAsync();

            var usuarioRol = new UsuarioRol
            {
                IdUsuario = userSaved.Entity.IdUsuario,
                IdRol = (int)request.Rol
            };
            var usuarioRolBd = _context.UsuarioRol.Add(usuarioRol);
            await _context.SaveChangesAsync();


            // Genera evento de usuario
            await _helperTraceService.SaveTraceUsuarios(userSaved.Entity.IdUsuario, EventoUsuario.AltaCuenta);

            await _pubSubPublisher.EnviarNotificacionAsync(mensaje);

            _logger.LogInformation("Proceso de alta de cuenta finalizado exitosamente para email: {Email}", request.Email);
            return new ApiResponseDto<bool>()
            {
                Message = "Cuenta creada exitosamente. Por favor revisa tu correo para activar tu cuenta.",
                Success = true,
                StatusCode = System.Net.HttpStatusCode.OK,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al intentar crear la cuenta para email: {Email}", request.Email);
            throw;
        }
        
    }


    public async Task SolicitudDeRecuperacionDeCuenta(RecuperacionRequest request)
    {
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.CorreoElectronico == request.Email);
        if (user == null)
            throw new LoginException("La cuenta invalida.");


        if (!user.Estatus)
        {
            await _helperTraceService.SaveTraceUsuarios(user.IdUsuario, EventoUsuario.IntentoFallido, "La cuenta no esta activa.");
            throw new LoginException("La cuenta no esta activa.");
        }

        // Bloqueo de cuenta
        var code = CodigoHelper.GenerarCodigoAlfanumerico();
        user.CodigoActivacion = code;
        await _context.SaveChangesAsync();


        DateTime fechaExpiracion = DateTime.UtcNow.AddHours(2);
        string estampaTiempo = fechaExpiracion.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string resultado = $"{request.Email}|{code}|{estampaTiempo}";
        string sing = await _googleKmsHelper.EncryptAsync(resultado);
        string url = $"{_frontUrl}/RecoveryAccount?sign={Uri.EscapeDataString(sing)}";

        var mensaje = new MessageWrapper<MessageSolicitudDeRecuperacion>
        {
            Payload = new MessageSolicitudDeRecuperacion { 
                NombreCompleto = user.Nombre,
                Email = user.CorreoElectronico,
                Code = code,
                Url = url

            },
            TypeNotification = "recuperacion_de_cuenta"
        };

        await _pubSubPublisher.EnviarNotificacionAsync(mensaje);

        // Genera evento de usuario
        await _helperTraceService.SaveTraceUsuarios(user.IdUsuario, EventoUsuario.RecuperacionDeCuenta);
    }

    public async Task<Usuario> ValidacionFirma(string sing)
    {
        var firmaDescifrada = string.Empty;
        try {
            firmaDescifrada = await _googleKmsHelper.DecryptAsync(sing);
        } catch
        {
            throw new FirmaCuentaException();
        }

        var partes = firmaDescifrada.Split('|');
        if (partes.Length != 3)
        {
            throw new FirmaCuentaException();
        }

        var correo = partes[0];
        var codigo = partes[1];
        var fechaStr = partes[2];

        if (DateTime.TryParse(fechaStr, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var fechaExpiracion))
        {
            if (DateTime.UtcNow > fechaExpiracion)
                throw new FirmaCuentaException("Firma de autorización caducada.");

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.CorreoElectronico == correo);
            if (!correo.Equals(user.CorreoElectronico, StringComparison.OrdinalIgnoreCase))
                throw new FirmaCuentaException("Firma de autorización no encontrada.");

            //if (!codigo.Equals(user.CodigoAutorizacion, StringComparison.OrdinalIgnoreCase))
            //    throw new FirmaCuentaException("Firma de autorización no encontrada.");

            return user;
        }

        throw new FirmaCuentaException("Firma de autorización incorrecta.");
    }

    public async Task ActivacionDeCuenta(string sing, string code, string password, string confirmacionPassword)
    {
        var usuario = await ValidacionFirma(sing);

        if (!code.Equals(usuario.CodigoActivacion, StringComparison.OrdinalIgnoreCase))
            throw new FirmaCuentaException("Código de autorización inválido.");

        if (!confirmacionPassword.Equals(password, StringComparison.OrdinalIgnoreCase))
            throw new DesbloqueoCuentaException("Password y confirmación de password son diferentes.");

        usuario.Password = PasswordHasher.Hashear(password);
        usuario.CodigoActivacion = null;
        usuario.Estatus = true;
        //usuario.Habilitado = true;

        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();

        var mensaje = new NotificacionEmail
        {
            ClaveAplicativo = "app-portal-proveedores",
            NombreTemplate = "cuenta_activada",
            EmailDestino = usuario.CorreoElectronico,
            Data = new
            {
                payload = new
                {
                    nombre = usuario.Nombre,
                    to = usuario.CorreoElectronico,
                    url = $"{_frontUrl}/login"
                }
            }
        };

        await _pubSubPublisher.EnviarNotificacionAsync(mensaje);

        // Genera evento de usuario
        await _helperTraceService.SaveTraceUsuarios(usuario.IdUsuario, EventoUsuario.ActivacionDeCuenta);
    }

    public async Task DesbloquearCuenta(string sign, string code, string password, string confirmacionPassword)
    {
        var usuario = await ValidacionFirma(sign);

        if (!usuario.Estatus)
        {
            await _helperTraceService.SaveTraceUsuarios(usuario.IdUsuario, EventoUsuario.IntentoFallido, "La cuenta no esta activa.");
            throw new LoginException("La cuenta no esta activa.");
        }

        if (!confirmacionPassword.Equals(password, StringComparison.OrdinalIgnoreCase))
            throw new DesbloqueoCuentaException("Password y confirmación de password son diferentes.");

        //if (!code.Equals(usuario.CodigoAutorizacion, StringComparison.OrdinalIgnoreCase))
        //    throw new DesbloqueoCuentaException("Código de autorización inválido.");

        usuario.Password = PasswordHasher.Hashear(password);
        //usuario.CodigoAutorizacion = null;
        //usuario.BloqueadoEn = null;

        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();

        //var mensaje = new MessageWrapper<MessageCuentaBase>
        //{
        //    Payload = new MessageCuentaBase
        //    {
        //        NombreCompleto = usuario.Nombre,
        //        Email = usuario.CorreoElectronico 
        //        //todo: se tiene que anexar la url para login
        //    },
        //    TypeNotification = "password_cambiado"
        //};

        var mensaje = new NotificacionEmail
        {
            ClaveAplicativo = "app-portal-proveedores",
            NombreTemplate = "password_modificado",
            EmailDestino = usuario.CorreoElectronico,
            Data = new
            {
                payload = new
                {
                    nombre = usuario.Nombre,
                    to = usuario.CorreoElectronico,
                    url = $"{_frontUrl}/login"
                }
            }
        };

        await _pubSubPublisher.EnviarNotificacionAsync(mensaje);

        // Genera evento de usuario
        await _helperTraceService.SaveTraceUsuarios(usuario.IdUsuario, EventoUsuario.DesbloqueoDeCuenta);
    }

        public async Task<Usuario> LoginAsync(string email, string password)
    {
        var user = await _context.Usuarios
            .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(u => u.CorreoElectronico == email);

        if (user == null)
        {
            throw new LoginException("Credenciales incorrectas.");
        }

        if (!user.Estatus) {
            await _helperTraceService.SaveTraceUsuarios(user.IdUsuario, EventoUsuario.IntentoFallido, "La cuenta no esta activa.");
            throw new LoginException("La cuenta no esta activa.");
        }

        //if (!user.Habilitado)
        //{
        //    await _helperTraceService.SaveTraceUsuarios(user.Id, EventoUsuario.IntentoFallido, "La cuenta no esta habilitada.");
        //    throw new LoginException("La cuenta no esta habilitada (verificar con logistica).");
        //}

        //if (user.BloqueadoEn != null) {
        //    await _helperTraceService.SaveTraceUsuarios(user.Id, EventoUsuario.IntentoFallido, "La cuenta esta bloqueada.");
        //    throw new LoginException("La cuenta esta bloqueada.");
        //}

        if (user == null || string.IsNullOrEmpty(user.Password)) { 
            await _helperTraceService.SaveTraceUsuarios(user.IdUsuario, EventoUsuario.IntentoFallido, "Credenciales incorrectas.");
            throw new LoginException("Credenciales incorrectas.");
        }

        if (BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            await _helperTraceService.SaveTraceUsuarios(user.IdUsuario, EventoUsuario.InicioSesion, "Inicio de sesión EXITOSO.");
            //user.IntentosFallidos = 0;
            await _context.SaveChangesAsync();
            return user;
        }
        else
        {
            //user.IntentosFallidos++;
            await _helperTraceService.SaveTraceUsuarios(user.IdUsuario, EventoUsuario.IntentoFallido, $"Intento fallido #{0}.");
            //if (user.IntentosFallidos >= 5)
            //{
            //    await _helperTraceService.SaveTraceUsuarios(user.Id, EventoUsuario.CuentaBloqueada);
            //    user.BloqueadoEn = TimeHelper.NowMexicoUnspecified();
            //    var mensaje = new MessageWrapper<MessageCuentaBase>
            //    {
            //        Payload = new MessageCuentaBase
            //        {
            //            Email = user.Email,
            //            NombreCompleto = user.NombreCompleto
            //        },
            //        TypeNotification = "cuenta_bloqueada"
            //    };

            //    await _pubSubPublisher.EnviarNotificacionAsync(mensaje);
            //}

            await _context.SaveChangesAsync();
            throw new LoginException("Credenciales incorrectas.");
        }
    }

    public enum EventoUsuario
    {
        AltaCuenta,
        RecuperacionDeCuenta,
        InicioSesion,
        CierreSesion,
        CambioContrasena,
        IntentoFallido,
        CuentaBloqueada,
        CuentaHabilitadaPorLogistica,
        CuentaInhabilitadaPorLogistica,
        DesbloqueoDeCuenta,
        ActivacionDeCuenta,
        Desactivado,
        Creado,
        Actualizado,
        Eliminado
    }

    public async Task<bool> LogoutAsync(string token)
    {
        var tokenRefresh = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        if(tokenRefresh == null)
            return false;

        tokenRefresh.RevocadoEn = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

}
