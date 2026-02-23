using ApiProveedores.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using ApiProveedores.Http.Filters;
using Microsoft.AspNetCore.Authorization;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Redirect("/swagger");
        }

        [HttpGet("api/health_check")]
        public async Task<IActionResult> HealthCheck()
        {
            return Ok();
        }


        [Authorize]
        [HttpGet("api/secured_health_check")]
        public async Task<IActionResult> SecuredHealthCheck()
        {
            return Ok();
        }
        
    }
}
