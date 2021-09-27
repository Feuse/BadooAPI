using BadooAPI.Utills;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ServicesInterfaces;
using ServicesModels;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;


namespace Services.Server.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDataAccessManager _dataManager;
        public HomeController(IDataAccessManager dataManager)
        {
            _dataManager = dataManager;
        }
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Data data, string returnUrl = "/")
        {
            var result = await _dataManager.CheckIfUsernameExists(data);
            if (result is null)
            {
                await _dataManager.RegisterUser(data);
                await Login(new Data() { Username = data.Username, Password = data.Password, Id = data.Id });
                ////TEMP
                return Ok();
                //return Redirect(returnUrl);
            }
            else
            {
                return BadRequest();
            }
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Data data, string returnUrl = "/")
        {
 
            // var id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var result = await _dataManager.AuthenticateUser(data);
            if (result is not null)
            {
                CookieOptions options = new();
                options.Secure = true;
                options.SameSite = SameSiteMode.None;
                options.HttpOnly = false;
                var claims = new List<Claim>();
                claims.Add(new Claim("username", result.Username));
    
                claims.Add(new Claim(ClaimTypes.NameIdentifier, result.Id));
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
              
                await HttpContext.SignInAsync(claimsPrincipal);
                HttpContext.Response.Cookies.Append("username", result.Username, options);
                HttpContext.Response.Cookies.Append("tutorial", result.SeenTutorial.ToString(), options);
                // var username = HttpContext.User.FindFirst("username").Value;
                //TEMP
                return Ok(result.Username);
                //return Redirect(returnUrl);
            }
            else
            {
                return BadRequest();
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Ok();
        }

    }
}
