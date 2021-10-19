using AutoMapper;
using BadooAPI.Factories;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    [ApiController]
    [Route("services/")]
    public class UserServicesController : ControllerBase
    {
        private readonly IUserServicesFacade _userServicesFacade;
        public UserServicesController(IUserServicesFacade facade)
        {
            _userServicesFacade = facade;
        }

        [Route("loginToService")]
        [HttpPost]
        public async Task<IActionResult> LoginToService(Data data)
        {
            try
            {
                data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                data = await _userServicesFacade.LoginToService(data);
                if (data.Result == Result.Success)
                    return Ok(data);
                return BadRequest();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [Route("authServices")]
        [HttpGet]
        public async Task<IActionResult> AuthenticateUserServices()
        {

            var servicesList = new List<UserServiceCredentials>();
            try
            {
                var id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                servicesList = await _userServicesFacade.AuthenticateUserServices(id);
                if (servicesList.Count > 0)
                {
                    return Ok(servicesList);
                }
                return BadRequest(servicesList);
            }
            catch (Exception)
            {
                return BadRequest(servicesList);
            }
        }
    }
}
