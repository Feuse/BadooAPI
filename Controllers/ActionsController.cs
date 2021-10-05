using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Newtonsoft.Json;
using ServicesInterfaces;
using ServicesModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Services.Server.Utills;
using DataAccess;
using ServicesInterfaces.Scheduler;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ServicesInterfaces.Global;
using ServicesInterfaces.Facades;
using ServicesFacade;

namespace Services.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("actions/")]
    public class ActionsController : ControllerBase
    {
        private readonly IActionsFacade _actionsFacade;
        public ActionsController(IActionsFacade facade)
        {
            _actionsFacade = facade;
        }
        [Route("getImages")]
        [HttpPost]
        public async Task<IDictionary<string, string>> GetImages(Data data)
        {
            IDictionary<string, string> images = new Dictionary<string, string>();
            try
            {
                data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                images = await _actionsFacade.GetImages(data);

                return images;
            }
            catch (Exception)
            {
                return images;
            }
        }
        [Route("removeImage")]
        [HttpPost]
        public async Task<IActionResult> RemoveImage(Data data)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                result = await _actionsFacade.RemoveImage(data);

                return Ok(result);
            }
            catch (Exception)
            {
                return BadRequest(result);
            }
        }
        [Route("uploadImage")]
        [HttpPost]
        public async Task<IActionResult> UploadImage(Service service)
        {
            IDictionary<string, string> images = new Dictionary<string, string>();
            Data data = new Data();
            try
            {
                data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                data.File = Request.Form.Files.FirstOrDefault();
                data.Service = service;

                images = await _actionsFacade.UploadImage(data);

                return Ok(images);
            }
            catch (Exception)
            {
                return BadRequest(images);
            }
        }
        [Route("schedule")]
        [HttpPost]
        public async Task<IActionResult> ScheduleTask([FromBody] List<Data> data)
        {
            try
            {
                var plan = await _actionsFacade.ScheduleTask(data, HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (plan.Count > 0)
                {
                    var result = String.Join(", ", plan.ToArray());
                    return Ok(result);
                }
                return BadRequest("no valid services");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [Route("updateAbout")]
        [HttpPost]
        public async Task<IActionResult> UpdateAboutMe(Data data)
        {
            data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            try
            {
                return Ok(await _actionsFacade.UpdateAboutMe(data));
            }
            catch (Exception e)
            {
                return BadRequest();
            }
        }

    }

}
