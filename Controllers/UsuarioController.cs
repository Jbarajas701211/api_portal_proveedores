using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ApiProveedores.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using ApiProveedores.Services;
using ApiProveedores.Dto.Http;
using ApiProveedores.Dto.Entrada;
using static System.Net.Mime.MediaTypeNames;
using System;
using System.Text;
using System.Security.Cryptography;

[ApiController]
[Route("api/usuarios")]
public class UsuarioController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UsuariosService _usuariosService;

    public UsuarioController(AuthService authService, UsuariosService usuariosService)
    {
        _authService = authService;
        _usuariosService = usuariosService;
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost("alta_cuenta")]
    public async Task<IActionResult> AltaDeCuenta(AltaCuentaRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var resultado = await _authService.AltaDeCuenta(request);
        return Ok(resultado);
    }

    [Authorize]
    [HttpGet("info")]
    public async Task<IActionResult> Info()
    {
        var user = User;

        var userId = user.FindFirst("sub")?.Value
          ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = user.FindFirst("name")?.Value;
        var givenName = user.FindFirst("given_name")?.Value
                     ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value;

        var cveProveedor = user.FindFirst("cveprov")?.Value;

        var roles = user.FindAll(System.Security.Claims.ClaimTypes.Role)
                        .Select(r => r.Value).ToList();

        byte[] data = Encoding.UTF8.GetBytes($"{userId}");
        byte[] hash = SHA256.HashData(data);
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return Ok(new
        {
            UserId = userId,
            UserName = userName,
            GivenName = givenName,
            Roles = roles, 
            Etag = hex,
            ClaveProveedor = cveProveedor
        });
    }


    [Authorize(Roles = "ADMIN")]
    [HttpGet("buscar")]
    public async Task<IActionResult> BuscarUsuarios(
        [FromQuery] string? usuario,
        [FromQuery] string? proveedor,
        [FromQuery] AgrupadorRol? agrupador,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        var resultado = await _usuariosService.BuscarUsuariosAsync(usuario, proveedor, agrupador, pagina, tamanoPagina);
        return Ok(resultado);
    }


    [Authorize(Roles = "ADMIN")]
    [HttpPatch("habilitar")]
    public async Task<IActionResult> DesactivarUsuario(HabilitarUsuarioDto dto)
    {
        await _usuariosService.DesactivarUsuarioAsync(dto);
        return Ok();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost("asociar_empresas")]
    public async Task<IActionResult> AsociarEmpresaAsync(AsociarEmpresasRequestDto asociarEmpresas)
    {
        try
        {
            var respuesta = await _usuariosService.AsociarEmpresasAsync(asociarEmpresas);
            return Ok(respuesta);
        }
        catch (ApiProveedoresException appEx)
        {
            return BadRequest(appEx.Message);
        }
    }

}
