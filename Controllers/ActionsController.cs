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
using DataAccess;

namespace Services.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/")]
    public class ActionsController : ControllerBase
    {
        private readonly IServicesFactory _factory;
        private readonly IScheduler _scheduler;
        private readonly IDataAccess _dataAccess;
        private readonly IDistributedCache _distributedCache;

        public ActionsController(IServicesFactory _factory, IScheduler _scheduler, IDataAccess _dataAccess, IDistributedCache distributedCache)
        {

            this._factory = _factory;
            this._scheduler = _scheduler;
            this._dataAccess = _dataAccess;
            _distributedCache = distributedCache;
        }

        [Route("login")]
        [HttpPost]
        public async Task<Data> Login(Data data)
        {
            try
            {
                data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

                var UserServices = await _distributedCache.GetRecordAsync<List<UserServiceCredentials>>("userServices");

                if (UserServices is null)
                {
                    UserServices = await _dataAccess.GetAllUserServicesById(data.Id);

                    if (UserServices is null)
                    {
                        return null;
                    }
                    //cache after retrieving from db 
                    await _distributedCache.SetRecordAsync("userServices", UserServices);
                }

                var singleService = UserServices.Where(a => a.Service == data.Service).FirstOrDefault();

                IService service = _factory.GetService(data.Service);

                data.Service = singleService.Service;
                data.UserName = singleService.Username;
                data.Password = singleService.Password;
                data.UserServiceId = singleService.UserServiceId;

                await GetSingleServiceSession(data, singleService, new ServiceSessions(), service);

            }
            catch (Exception)
            {
                return data;

            }
            return data;
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

                var cachedImages = await _distributedCache.GetRecordAsync<IDictionary<string, string>>($"{data.Id}|images");
                if (cachedImages is not null)
                {
                    return cachedImages;
                }

                var userService = await _dataAccess.GetUserServiceByServiceNameAndId(data);
                data.UserName = userService.Username;
                data.Password = userService.Password;
                await Login(data);

                result = await service.GetImages(data);
                if (result.Count > 0)
                {
                    await _distributedCache.SetRecordAsync($"{data.Id}|images", result);
                    return result;
                }
                return result;
            }
            catch (Exception)
            {
                return result;
            }
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
            Data data = new();
            IDictionary<string, string> images = new Dictionary<string, string>();

            try
            {
                var files = Request.Form.Files.FirstOrDefault();
                data.File = files;

                await Login(data);

                IService service = _factory.GetService(data.Service);
                var result = await service.UploadImage(data);

                var resultObject = JsonConvert.DeserializeObject<PhotosResultModel>(result);

                var id = resultObject.PhotoId;
                var url = resultObject.PhotoUrl;

                images.Add(id, url);

                return images;
            }
            catch (Exception)
            {
                return images;
            }
        }

        [Route("schedule")]
        [HttpPost]
        public async Task ScheduleTask([FromBody] List<Data> data)
        {
            var planFactory = new PlanFactory();
            try
            {
                var id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

                foreach (var service in data)
                {
                    var plan = planFactory.GetPlan(service.Likes);
                    await _scheduler.Schedule(new Message
                    {
                        Likes = plan.Likes,
                        Price = plan.Price,
                        Service = service.Service,
                        UserId = id,
                        MessageId = Guid.NewGuid()
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

                    await GetServicesSessions(servicesList, data, singleService, service);
                }

            }
            catch (Exception)
            {
                servicesList.Clear();
                return servicesList;

            }
            return servicesList;
        }

        private async Task GetServicesSessions(List<Service> servicesList, Data data, UserServiceCredentials singleService, IService service)
        {
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
                    data = await service.AppStartUp(data);
                    await TryGetAndUpdateSession(data);
                    servicesList.Add(singleService.Service);
                }
            }
            else
            {
                servicesList.Add(singleService.Service);
            }
        }
        private async Task GetSingleServiceSession(Data data, UserServiceCredentials singleService, ServiceSessions singleSession, IService service)
        {
            var chachedSession = await _distributedCache.GetRecordAsync<ServiceSessions>("serviceSession");

            if (chachedSession is null)
            {
                var session = await _dataAccess.CheckForServiceSession(data);

                if (session is not null)
                {
                    data.SessionId = session.SessionId;
                    data.HiddenUrl = session.HiddenUrl;

                    await _distributedCache.SetRecordAsync("serviceSession", session);
                }
                else
                {
                    // No session, log in to user and update session.
                    var result = await service.AppStartUp(data);
                    await TryGetAndUpdateSession(result);
                }
            }
            else
            {
                data.SessionId = chachedSession.SessionId;
                data.HiddenUrl = chachedSession.HiddenUrl;
            }
        }
        private async Task TryGetAndUpdateSession(Data data)
        {
            if (data.Result == Result.Success)
            {
                await _dataAccess.UpdateServiceSession(data);
            }
            else
            {
                //if unable to log into service, remove service and session.
                await _dataAccess.RemoveServiceFromUser(data);
            }
        }

        [Route("updateAbout")]
        [HttpPost]
        public async Task<string> UpdateAboutMe(Data data)
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
