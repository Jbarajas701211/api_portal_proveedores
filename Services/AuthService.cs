
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiProveedores.Models;
using System;
using ApiProveedores.Services.PubSub;
using ApiProveedores.Helper;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Services.Helper;
using ApiProveedores.Dto.Http;
using ApiProveedores.Dto.PubSub;

public class AuthService
{
    private readonly HelperTraceService _helperTraceService;
    private readonly GoogleKmsHelper _googleKmsHelper;
    private readonly PublisherPnjService _pubSubPublisher;
    private readonly PortalDbContext _context;
    private string _frontUrl;
    
    public AuthService(PortalDbContext context, PublisherPnjService pubSubPublisher, 
        GoogleKmsHelper googleKmsHelper, HelperTraceService helperTraceService)
    {
        _context = context;
        _pubSubPublisher = pubSubPublisher;
        _googleKmsHelper = googleKmsHelper;
        _frontUrl = Environment.GetEnvironmentVariable("PORTAL_CITAS_URL");
        _helperTraceService = helperTraceService;
    }
    public async Task AltaDeCuenta(AltaCuentaRequest request) {

        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.CorreoElectronico == request.Email);
        if (user != null)
            throw new AltaCuentaException();

        var usuarioNuevo = new Usuario();
        //usuarioNuevo.Rol = request.Rol.ToString();
        usuarioNuevo.Nombre = request.NombreCompleto.ToUpperInvariant();
        usuarioNuevo.CorreoElectronico = request.Email.ToLowerInvariant();

        if (request.Rol == RolUsuario.PROVEEDOR)
        {
            if (request.ProveedorId == 0)
            {
                throw new AltaCuentaException("Es necesario indicar el Proveedor para el registro del usuario.");
            }
            //usuarioNuevo.ProveedorId = request.ProveedorId;
        }
        usuarioNuevo.Estatus = false;


        // Bloqueo de cuenta
        var codigoActivacion = CodigoHelper.GenerarCodigoAlfanumerico();
        //usuarioNuevo.CodigoAutorizacion = codigoActivacion;
        


        DateTime fechaExpiracion = DateTime.UtcNow.AddHours(2);
        string estampaTiempo = fechaExpiracion.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string resultado = $"{request.Email}|{codigoActivacion}|{estampaTiempo}";
        string sign = await _googleKmsHelper.EncryptAsync(resultado);
        string url = $"{_frontUrl}/ActivateAccount?sign={Uri.EscapeDataString(sign)}";
        
        var mensaje = new MessageWrapper<MessageCuentaNueva>
        {
            Payload = new MessageCuentaNueva
            {
                NombreCompleto = usuarioNuevo.Nombre,
                Email = usuarioNuevo.CorreoElectronico,
                Code = codigoActivacion,
                Url = url

            },
            TypeNotification = "cuenta_nueva"
        };


        //usuarioNuevo.CodigoAutorizacion = codigoActivacion;
        var userSaved = _context.Usuarios.Add(usuarioNuevo);
        await _context.SaveChangesAsync();


        // Genera evento de usuario
        await _helperTraceService.SaveTraceUsuarios(userSaved.Entity.IdUsuario, EventoUsuario.AltaCuenta);

        await _pubSubPublisher.EnviarNotificacionAsync(mensaje);
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
        //user.CodigoAutorizacion = code;
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

        //if (!code.Equals(usuario.CodigoAutorizacion, StringComparison.OrdinalIgnoreCase))
        //    throw new FirmaCuentaException("Código de autorización inválido.");

        if (!confirmacionPassword.Equals(password, StringComparison.OrdinalIgnoreCase))
            throw new DesbloqueoCuentaException("Password y confirmación de password son diferentes.");

        usuario.Password = PasswordHasher.Hashear(password);
        //usuario.CodigoAutorizacion = null;
        usuario.Estatus = true;
        //usuario.Habilitado = true;

        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();

        var mensaje = new MessageWrapper<MessageCuentaBase>
        {
            Payload = new MessageCuentaBase
            {
                NombreCompleto = usuario.Nombre,
                Email = usuario.CorreoElectronico
            },
            TypeNotification = "cuenta_confirmada"
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

        var mensaje = new MessageWrapper<MessageCuentaBase>
        {
            Payload = new MessageCuentaBase
            {
                NombreCompleto = usuario.Nombre,
                Email = usuario.CorreoElectronico
            },
            TypeNotification = "password_cambiado"
        };

        await _pubSubPublisher.EnviarNotificacionAsync(mensaje);

        // Genera evento de usuario
        await _helperTraceService.SaveTraceUsuarios(usuario.IdUsuario, EventoUsuario.DesbloqueoDeCuenta);
    }

    public async Task<Usuario> LoginAsync(string email, string password)
    {
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.CorreoElectronico == email);

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

}
