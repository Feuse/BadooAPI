using BadooAPI.Utills;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        private readonly IDataAccess _dataAccess;
        public HomeController(IDataAccess _dataAccess)
        {

            this._dataAccess = _dataAccess;

        }
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Data data, string returnUrl = "/")
        {

            var result = await _dataAccess.CheckIfUsernameExists(data);
            if (result == null)
            {
                await _dataAccess.RegisterUser(data);
                await Login(new Data() { UserName = data.UserName, Password = data.Password, Id = data.Id });
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
            var result = await _dataAccess.AuthenticateUser(data);
            if (result != null)
            {
                var claims = new List<Claim>();
                claims.Add(new Claim("username", result.Username));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, result.Id));
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(claimsPrincipal);
                CookieOptions options = new();
                options.HttpOnly = false;
                HttpContext.Response.Cookies.Append("username", result.Username, options);
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
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }

        [Authorize]
        [HttpPost("checkOut")]
        public async Task<IActionResult> CheckOut(string ammount)
        {
            try
            {

            }
            catch (Exception e)
            {
                e.LogException();
                throw;
            }
            return Ok();
        }
    }
}
