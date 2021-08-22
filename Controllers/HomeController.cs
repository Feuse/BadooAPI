using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicesInterfaces;
using ServicesModels;
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
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Data data, string returnUrl = "/")
        {
            var result = await _dataAccess.AuthenticateUser(data);
            if (result != null)
            {
                var claims = new List<Claim>();
                claims.Add(new Claim("username", result.Username));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, result.Id));
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(claimsPrincipal);

                //TEMP
                return Redirect("/success");
                //return Redirect(returnUrl);
            }
            else
            {
                return Redirect("/error");
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
    }
}
