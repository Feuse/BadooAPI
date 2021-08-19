using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RabbitMQScheduler.Interfaces;
using ServicesInterfaces;
using ServicesModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Services.Server.Controllers
{
    [ApiController]
    [Route("api/")]
    public class BadooController : ControllerBase
    {
        private readonly IServicesFactory _factory;
        private readonly IScheduler _scheduler;
        public BadooController(IServicesFactory _factory, IScheduler _scheduler)
        {
            this._factory = _factory;
            this._scheduler = _scheduler;
        }
        [Route("login")]
        [HttpPost]
        public async Task<string> Login(Data data)
        {
            IService service = _factory.GetService(data.Service);
            var result = await service.AppStartUp(data);
            //returning userid, sessionid, hiddenurl
            //save in db for 24 hrs?
            return result;
        }
        [Route("getImages")]
        [HttpPost]
        public async Task<IDictionary<string, string>> GetImages(Data data)
        {
            IService service = _factory.GetService(data.Service);
            var result = await service.GetImages(data);
            //returning userid, sessionid, hiddenurl
            //save in db for 24 hrs?
            return result;
        }
        [Route("schedule")]
        [HttpPost]
        public async Task<string> ScheduleTask([FromBody] Data data)
        {
            IService service = _factory.GetService(data.Service);

            //schedule likes 
            await _scheduler.Schedule(new RabbitMQScheduler.Models.Message { 
                Likes = data.Likes, Service = data.Service, Time = data.Time, UserId = data.UserId 
            });
            
            return "";
        }

        //[Route("like")]
        //[HttpPost]
        //public async Task<string> Like(Data data)
        //{
        //    IService service = _factory.GetService(data.Service);

        //    //schedule likes 

        //    var result = await service.GetEncounters(data);
        //    return result;
        //}
    }
}
