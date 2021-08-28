using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
                return Redirect("/success");
                //return Redirect(returnUrl);
            }
            else
            {
                return Redirect("/error");
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

        /// TEMP
        [HttpGet("error")]
        public async Task<string> Error()
        {
            return "error";
        }

        [HttpGet("success")]
        public async Task<string> Success()
        {
            return "success";
        }
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }
        [Authorize]
        [HttpPost("isLoggedIn")]
        public async Task<string> IsLoggedIn(string ammount)
        {
            return ammount;
        }
        [Authorize]
        [HttpPost("authUser")]
        public async Task<string> AuthUser()
        {
            var username = HttpContext.User.FindFirst("username").Value;
            var name = HttpContext.Session.GetString("username");
            if (name is null)
            {
                HttpContext.Session.SetString("username", username);
                return username;
            }
            return username;
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
