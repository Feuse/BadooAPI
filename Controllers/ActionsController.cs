using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Newtonsoft.Json;
using RabbitMQScheduler.Interfaces;
using ServicesInterfaces;
using ServicesModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Services.Server.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/")]
    public class ActionsController : ControllerBase
    {
        const string URI = "mongodb+srv://roman:1212@cluster0.voo5h.mongodb.net/myFirstDatabase?retryWrites=true&w=majority";
        const string NAME = "AUTOLOVER";
        private readonly IServicesFactory _factory;
        private readonly IScheduler _scheduler;
        private readonly IDataAccess _dataAccess;
        private IMongoDatabase _database;
        public ActionsController(IServicesFactory _factory, IScheduler _scheduler, IDataAccess _dataAccess)
        {

            this._factory = _factory;
            this._scheduler = _scheduler;
            this._dataAccess = _dataAccess;
            var client = new MongoClient(URI);
            _database = client.GetDatabase(NAME);
        }
        [AllowAnonymous]
        [Route("test")]
        [HttpPost]
        public async Task test(Data data)
        {
            //TEMP 

            //add to list
            var collection = _database.GetCollection<UserCredentials>("UserCredentials");
            UserCredentials user = new UserCredentials()
            {
                Username = "Roman135@gmail.com",
                Password = "1212",
                Services = null
            };
            await collection.InsertOneAsync(user);

        }

        [Route("login")]
        [HttpPost]
        public async Task<Data> Login(Data data)
        {

            data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //try to retrieve user service, if exists check for a valid session and return,
            // else if user has no service start it up and register
            var userService = await _dataAccess.GetUserServiceByServiceName(data);
            if (userService is not null)
            {

                data.UserServiceId = userService.UserServiceId;
                data.UserName = userService.Username;
                data.Password = userService.Password;

                /////////// TEMPORARY, XPing should be generated in login service method.
                data.XPing = "b3da80cc837c7a1cd30029ae9a129a82";
                /////////////////////

                var result = await _dataAccess.CheckForServiceSession(data);
                data.SessionId = result.SessionId;
                data.HiddenUrl = result.HiddenUrl;
                return data;
            }
            else
            {
                IService service = _factory.GetService(data.Service);
                var result = await service.AppStartUp(data);

                if (result.Result == Result.Success)
                {
                    userService = await _dataAccess.GetUserServiceByServiceName(data);
                    if (userService is null)
                    {
                        await _dataAccess.RegisterService(data);
                        // await _dataAccess.UpdateServiceSession(data);

                        return result;
                    }

                    await _dataAccess.UpdateServiceSession(data);

                    return result;
                }
                else
                {
                    //log error
                    return result;
                }

            }
        }

        [Route("getImages")]
        [HttpPost]
        public async Task<IDictionary<string, string>> GetImages(Data data)
        {
            IService service = _factory.GetService(data.Service);
            await Login(data);
            var result = await service.GetImages(data);
            return result;
        }

        [Route("removeImage")]
        [HttpPost]
        public async Task<IDictionary<string, string>> RemoveImage(Data data)
        {
            IService service = _factory.GetService(data.Service);
            await Login(data);
            var result = await service.RemoveImage(data);
            return result;
        }

        [Route("uploadImage")]
        [HttpPost]
        public async Task<string> UploadImage([FromForm] string data)
        {
            var files = Request.Form.Files.FirstOrDefault();
            var deserializedData = JsonConvert.DeserializeObject<Data>(data);
            deserializedData.File = files;

            await Login(deserializedData);

            IService service = _factory.GetService(deserializedData.Service);
            var result = await service.UploadImage(deserializedData);
            return result;
        }


        [Route("schedule")]
        [HttpPost]
        public async Task ScheduleTask([FromBody] List<Data> data)
        {
            var id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            foreach (var service in data)
            {
                await _scheduler.Schedule(new RabbitMQScheduler.Models.Message
                {
                    Likes = service.Likes,
                    Service = service.Service,
                    UserId = id
                });
            }
        }

        [Route("authServices")]
        [HttpGet]
        public async Task<List<Service>> AuthenticateUserServices()
        {
            var id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var data = new Data() { Id = id };
            data.XPing = "ed891794b88a2507a50abbc384f51627";

            var servicesList = new List<Service>();
            //servicesList.Add(Service.OkCupid);
            var allUserServices = await _dataAccess.GetAllUserServicesById(data);
            foreach (var singleService in allUserServices)
            {
                IService service = _factory.GetService(singleService.Service);
                data.Service = singleService.Service;
                data.UserName = singleService.Username;
                data.Password = singleService.Password;
                // await _dataAccess.GetUserServiceByServiceName(data); // no need? getting services from above
                var result = await service.AppStartUp(data);
                if (result.Result == Result.Success)
                {
                    servicesList.Add(singleService.Service);
                }
                else
                {
                    //if unable to log into service
                    await _dataAccess.RemoveServiceFromUser(data);
                }
            }

            return servicesList;
        }
    }
}
