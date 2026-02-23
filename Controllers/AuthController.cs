
using System;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ApiProveedores.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Services;
using ApiProveedores.Dto.Http;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly TokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, JwtSettings jwtSettings, TokenService tokenService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtSettings = jwtSettings;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("activar_cuenta")]
    public async Task<IActionResult> ActivacionDeCuenta([FromQuery] string code, [FromQuery] string sign, [FromBody] RegistroPasswordRequest request)
    {

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(sign))
        {
            return BadRequest(new { message = "Parámetros 'code' y 'sign' son obligatorios." });
        }

        try
        {
            await _authService.ActivacionDeCuenta(sign, code, request.Password, request.Confirmacion);
            return Ok(new { message = "La cuenta ha sido activada correctamente." });
        }
        catch (AltaCuentaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("desbloquear_cuenta")]
    public async Task<IActionResult> DesbloquearCuenta([FromQuery] string code, [FromQuery] string sign, [FromBody] RegistroPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(sign))
        {
            return BadRequest(new { message = "Parámetros 'code' y 'sign' son obligatorios." });
        }

        await _authService.DesbloquearCuenta(sign, code, request.Password, request.Confirmacion);
        return Ok(new { message = "La cuenta ha sido activada correctamente." });
    }


    [HttpPost("recuperar_cuenta")]
    public async Task<IActionResult> SolicitudRecuperacionDeCuenta(RecuperacionRequest request)
    {
        await _authService.SolicitudDeRecuperacionDeCuenta(request);
        return Ok(new { message = "Solicitud de recuperación de cuenta exitosa." });
    }

    [HttpGet("validar_firma")]
    public async Task<IActionResult> ValidaFirma([FromQuery] string sign)
    {

        if (string.IsNullOrWhiteSpace(sign))
        {
            return BadRequest(new { message = "Parámetro 'sign' son obligatorio." });
        }

        await _authService.ValidacionFirma(sign);
        return Ok(new { message = "La firma es válida." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _authService.LoginAsync(request.Email, request.Password);
        var token = _tokenService.GenerarJwt(user);
        var refreshToken = await _tokenService.GenerarRefreshTokenAsync(user);
        var expiresAt = DateTime.UtcNow.AddDays(TokenService.REFRESH_TOKEN_DIAS_VALIDOS);
        return Ok(new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            RefreshExpiresAt = expiresAt,
            Nombre = user.Nombre ?? "NO NAME",
            Rol = "ANONIMO"
        });
    }



    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var (jwt, nuevoRefresh) = await _tokenService.RenovarAsync(request.RefreshToken);
            return Ok(new
            {
                token = jwt,
                refreshToken = nuevoRefresh
            });
        }
        catch
        {
            return Unauthorized(new { message = "Refresh token inválido o expirado" });
        }
    }


}
