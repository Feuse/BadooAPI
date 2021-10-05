using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServicesInterfaces;
using ServicesInterfaces.Facades;
using ServicesInterfaces.Global;
using ServicesModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Services.Server.Controllers
{
    [ApiController]
    [Route("authorize/")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginFacade _loginFacade;
        private readonly ILogger<LoginController> _logger;
        public LoginController(ILogger<LoginController> logger, ILoginFacade facade)
        {
            _logger = logger;
            _loginFacade = facade;
        }
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Data data)
        {
            try
            {
                //data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                await _loginFacade.Register(data);
                await Login(new Data() { Username = data.Username, Password = data.Password, Id = data.Id });
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                _logger.LogTrace(e.StackTrace);

                return Conflict(e.Message);
            }
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Data data)
        {
            try
            {
                var result = await _loginFacade.Login(data);
                var principal = result.Item1;

                if (principal is not null)
                {
                    var user = result.Item2;

                    await HttpContext.SignInAsync(principal);

                    SetCookies(user);

                    return Ok(user.Username);
                }
                return BadRequest();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        private void SetCookies(UserCredentials user)
        {
            CookieOptions options = new();
            options.Secure = true;
            options.SameSite = SameSiteMode.None;
            options.HttpOnly = false;
            //options.Domain =".autolovers.com";

            HttpContext.Response.Cookies.Append("username", user.Username, options);
            HttpContext.Response.Cookies.Append("tutorial", user.SeenTutorial.ToString(), options);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync();
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                _logger.LogTrace(e.StackTrace);
                return BadRequest();
            }
        }
    }
}
