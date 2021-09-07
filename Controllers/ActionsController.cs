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
using RabbitMQScheduler.Models;
using Microsoft.Extensions.Caching.Distributed;
using Services.Server.Utills;

namespace Services.Server.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/")]
    public class ActionsController : ControllerBase
    {
        //from config file

        const string URI = "mongodb+srv://roman:1212@cluster0.voo5h.mongodb.net/myFirstDatabase?retryWrites=true&w=majority";
        const string NAME = "AUTOLOVER";
        private readonly IServicesFactory _factory;
        private readonly IScheduler _scheduler;
        private readonly IDataAccess _dataAccess;
        private readonly IDistributedCache _distributedCache;
        private IMongoDatabase _database;

        public ActionsController(IServicesFactory _factory, IScheduler _scheduler, IDataAccess _dataAccess, IDistributedCache distributedCache)
        {

            this._factory = _factory;
            this._scheduler = _scheduler;
            this._dataAccess = _dataAccess;
            var client = new MongoClient(URI);
            _database = client.GetDatabase(NAME);
            _distributedCache = distributedCache;
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
            try
            {
                data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
            catch (Exception)
            { }

            ///  FOR THIS WHOLE PART: 
            /// 
            var userService = await _dataAccess.GetUserServiceByServiceNameAndId(data);
            if (userService is not null)
            {

                data.UserServiceId = userService.UserServiceId;
                data.UserName = userService.Username;
                data.Password = userService.Password;


                var result = await _dataAccess.CheckForServiceSession(data);
                if (result.SessionId is not null)
                {
                    data.SessionId = result.SessionId;
                    data.HiddenUrl = result.HiddenUrl;
                    return data;
                    /// 
                    /// I could return the same data type from GetUserServiceByServiceNameAndId method (Data) as the parameter it accepts and
                    /// then avoid the re-assignment of variables because the variable passed by reference.
                    /// that means I would need to do the assignment inside the GetUserServiceByServiceNameAndId method because internaly 
                    /// it works with a different data type.
                    /// 
                }
                else
                {
                    IService service = _factory.GetService(data.Service);
                    var startupResult = await service.AppStartUp(data);

                    if (startupResult.Result == Result.Success)
                    {
                        try
                        {
                            await _dataAccess.UpdateServiceSession(data);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                await _dataAccess.RemoveServiceFromUser(data);
                            }
                            catch (Exception)
                            { }
                            return startupResult;
                        }

                        return startupResult;
                    }
                    else
                    {
                        //log error
                        return startupResult;
                    }
                }
            }
            else
            {
                IService service = _factory.GetService(data.Service);
                var result = await service.AppStartUp(data);

                if (result.Result == Result.Success)
                {
                    userService = await _dataAccess.GetUserServiceByServiceNameAndId(data);
                    if (userService is null)
                    {
                        await _dataAccess.RegisterService(data);
                        // await _dataAccess.UpdateServiceSession(data);

                        return result;
                    }

                    try
                    {
                        await _dataAccess.UpdateServiceSession(data);
                    }
                    catch (Exception)
                    {
                        return result;
                    }

                    return result;
                }
                else
                {
                    //log error
                    return result;
                }
            }
        }
        //Response Cache
        [Route("getImages")]
        [HttpPost]
        public async Task<IDictionary<string, string>> GetImages(Data data)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                IService service = _factory.GetService(data.Service);
                var userService = await _dataAccess.GetUserServiceByServiceNameAndId(data);
                data.UserName = userService.Username;
                data.Password = userService.Password;
                await Login(data);
                result = await service.GetImages(data);
            }
            catch (Exception)
            {
                return result;
            }

            return result;
        }

        [Route("removeImage")]
        [HttpPost]
        public async Task<IDictionary<string, string>> RemoveImage(Data data)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                IService service = _factory.GetService(data.Service);
                await Login(data);
                result = await service.RemoveImage(data);
            }
            catch (Exception)
            {
                return result;
            }
            return result;
        }

        [Route("uploadImage")]
        [HttpPost]
        public async Task<IDictionary<string, string>> UploadImage()
        {
            Data data = new Data();
            IDictionary<string, string> dict = new Dictionary<string, string>();
            IFormFile files;
            dynamic parsedResponse = new object();

            try
            {
                files = Request.Form.Files.FirstOrDefault();
                data.File = files;

                await Login(data);

                IService service = _factory.GetService(data.Service);
                var result = await service.UploadImage(data);
                parsedResponse = JsonConvert.DeserializeObject<dynamic>(result);
            }
            catch (Exception)
            {
                return dict;
            }
            try
            {
                var id = (string)parsedResponse.photo_id;
                var trimmedId = id.Trim(new Char[] { '}', '{' });
                var url = (string)parsedResponse.photo_url;
                var trimmedUrl = url.Trim(new Char[] { '}', '{' });
                dict.Add(trimmedId, trimmedUrl);
                return dict;

            }
            catch (Exception)
            {
                return dict;
            }
        }


        [Route("schedule")]
        [HttpPost]
        public async Task ScheduleTask([FromBody] List<Data> data)
        {
            try
            {
                var id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

                foreach (var service in data)
                {
                    await _scheduler.Schedule(new Message
                    {
                        Likes = service.Likes,
                        Service = service.Service,
                        UserId = id
                    });
                }
            }
            catch (Exception)
            {
                //log
            }
        }

        [Route("authServices")]
        [HttpGet]
        public async Task<List<Service>> AuthenticateUserServices()
        {
            var servicesList = new List<Service>();
            
            try
            {
                var id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                var data = new Data() { Id = id };

                //check cache for services
                var UserServices = await _distributedCache.GetRecordAsync<List<UserServiceCredentials>>("userServices");

                if (UserServices is null)
                {
                    UserServices = await _dataAccess.GetAllUserServicesById(id);

                    if (UserServices is null)
                    {
                        return null;
                    }
                    //cache after retrieving from db 
                    await _distributedCache.SetRecordAsync("userServices", UserServices);
                }

                foreach (var singleService in UserServices)
                {
                    IService service = _factory.GetService(singleService.Service);
                    data.Service = singleService.Service;
                    data.UserName = singleService.Username;
                    data.Password = singleService.Password;
                    data.UserServiceId = singleService.UserServiceId;
                    // await _dataAccess.GetUserServiceByServiceName(data); // no need? getting services from above

                    var chachedSession = await _distributedCache.GetRecordAsync<ServiceSessions>("serviceSession");

                    if (chachedSession is null)
                    {
                        var session = await _dataAccess.CheckForServiceSession(data);

                        if (session is not null)
                        {
                            servicesList.Add(singleService.Service);
                            await _distributedCache.SetRecordAsync("serviceSession", session);
                        }
                        else
                        {
                            // No session, log in to user and update session.
                            var result = await service.AppStartUp(data);
                            if (result.Result == Result.Success)
                            {
                                servicesList.Add(singleService.Service);
                                await _dataAccess.UpdateServiceSession(data);
                            }
                            else
                            {
                                //if unable to log into service, remove service and session.
                                await _dataAccess.RemoveServiceFromUser(data);
                            }
                        }
                    }
                    else
                    {
                        servicesList.Add(singleService.Service);
                    }  
                }

            }
            catch (Exception)
            {

                return servicesList;

            }
            return servicesList;
        }
        [Route("updateAbout")]
        [HttpPost]
        public async Task<string> Update(Data data)
        {
            try
            {
                IService service = _factory.GetService(data.Service);
                await Login(data);
                await service.UpdateAboutMe(data);
                return data.About;
            }
            catch (Exception)
            {
                //log
                return "unable to update";
            }
        }
    }
}
