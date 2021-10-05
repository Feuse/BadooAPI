using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServicesInterfaces;
using ServicesInterfaces.Facades;
using ServicesModels;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Services.Server.Controllers
{
    [Route("user/")]
    public class UserController : ControllerBase
    {
        private readonly IUserFacade _userFacade;

        public UserController(IUserFacade facade)
        {
            _userFacade = facade;
        }
        [Route("tutorial")]
        [HttpPost]
        public async Task SeenTutorial([FromBody]Data data)
        {
            try
            {
                var id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                data.Id = id;
                await _userFacade.SeenTutorial(data);

            }
            catch (Exception){}
        }
        [Route("info")]
        [HttpGet]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                var id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

                var user = await _userFacade.GetUserInfo(id);
                if (string.IsNullOrEmpty(user.Name))
                {
                    return BadRequest();
                }
                return Ok(new { user.Name, user.Age , user.About});
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}
