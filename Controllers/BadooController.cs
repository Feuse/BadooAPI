using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    [ApiController]
    [Route("api/")]
    public class BadooController : ControllerBase
    {
        const string URI = "mongodb+srv://roman:1212@cluster0.voo5h.mongodb.net/myFirstDatabase?retryWrites=true&w=majority";
        const string NAME = "AUTOLOVER";
        private readonly IServicesFactory _factory;
        private readonly IScheduler _scheduler;
        private readonly IDataAccess _dataAccess;
        private IMongoDatabase database;
        public BadooController(IServicesFactory _factory, IScheduler _scheduler, IDataAccess _dataAccess)
        {

            this._factory = _factory;
            this._scheduler = _scheduler;
            this._dataAccess = _dataAccess;
            var client = new MongoClient(URI);
            database = client.GetDatabase(NAME);
        }

        [Route("test")]
        [HttpPost]
        public async Task<IList<UserCredentials>> test(Data data)
        {
            //TEMP 
            data.Id = "611eabd43131a540a0a478f5";
            data.Service = Service.Badoo;
            await _dataAccess.RegisterService(data);
            data.Service = Service.OkCupid;
            await _dataAccess.RegisterService(data);
            data.Service = Service.Badoo;
            await _dataAccess.RemoveServiceFromUser(data);
            //add to list
            var collection = database.GetCollection<UserCredentials>("test");
            var filter = Builders<UserCredentials>.Filter.Eq("Username", "uniquemail@gmail.com");
            await collection.UpdateOneAsync(filter,
                 Builders<UserCredentials>.Update.AddToSet(u => u.Services, new UserServiceCredentials()
                 {
                     UserServiceId = "ZEJKSEUIQASD",
                     Username = "uniquemail@gmail.com",
                     Hash = "434343",
                     Password = "8888",
                     Service = Service.Grinder
                 }));

            //// ADD Expiry to serviceSessions based on UserServiceCredentials ID (Badoo ID) 
            var sessions = database.GetCollection<ServiceSessions>("test");
            var games = database.GetCollection<UserCredentials>("UserCredentials");
            var res2 = games.Find(filter).FirstOrDefault();
            ServiceSessions session = new ServiceSessions()
            {
                Id = res2.Id,
                SessionId = "s4:93jiw4o4ii24039430",
                expireAt = DateTime.UtcNow
            };

            sessions.InsertOne(session);
            UserCredentials x = new UserCredentials()
            {
                Username = "dsadas"
            };
            await collection.InsertOneAsync(x);

            //UserCredentials newGame = new UserCredentials()
            //{
            //    _id = auto generated ,
            //    Username = "feuse135@gmail.com",
            //    Hash = "123123",
            //    Password = "1212",
            //    Services = new List<UserServiceCredentials>() { new UserServiceCredentials()
            //    {
            //        UserId = "ABCDEFGHIJ",
            //        Service = Service.Badoo,
            //        Username = "feues3",
            //        Password = "121233",
            //    } }
            //};

            //games.InsertOne(newGame);

            //Create session id table and insert TTL 
            sessions = database.GetCollection<ServiceSessions>("test");

            filter = Builders<UserCredentials>.Filter.Eq("Username", "jaja");
            res2 = games.Find(filter).FirstOrDefault();
            ServiceSessions session2 = new ServiceSessions()
            {
                Id = res2.Id,
                SessionId = "1323222",
                Service = Service.OkCupid,
                expireAt = DateTime.UtcNow
            };

            sessions.InsertOne(session);

            // INSERT TTL Index 
            //var collection = database.GetCollection<ServiceSessions>("test");

            //var indexKeysDefinition = Builders<ServiceSessions>.IndexKeys.Ascending("expireAt");
            //var indexOptions = new CreateIndexOptions { ExpireAfter = new TimeSpan(0, 0, 0) };
            //var indexModel = new CreateIndexModel<ServiceSessions>(indexKeysDefinition, indexOptions);
            //collection.Indexes.CreateOne(indexModel);


            return games.Find(game => true).ToList();
        }

        [Route("login")]
        [HttpPost]
        public async Task<Data> Login(Data data)
        {
            try
            {
                data.Id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
               
                //try to retrieve user service, if exists check for a valid session and return,
                // else if user has no service start it up and register
                var userService = await _dataAccess.GetUserServiceByServiceName(data);
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
            catch (Exception)
            {
                IService service = _factory.GetService(data.Service);
                var result = await service.AppStartUp(data);

                if (result.Result == Result.Success)
                {
                    var userService = await _dataAccess.GetUserServiceByServiceName(data);
                    if (userService is null)
                    {
                        await _dataAccess.RegisterService(data);
                        await _dataAccess.UpdateServiceSession(data);

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
        public async Task<string> UploadImage([FromForm]string data)
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
        public async Task ScheduleTask([FromBody] Data data)
        {
            await _scheduler.Schedule(new RabbitMQScheduler.Models.Message
            {
                Likes = data.Likes,
                Service = data.Service,
                Time = data.Time,
                UserId = data.Id
            });
        }

        public async Task<IList<Service>> AuthenticateUserServices(Data data)
        {
            var servicesList = new List<Service>();
            var allUserServices = await _dataAccess.GetAllUserServicesById(data);
            foreach (var singleService in allUserServices)
            {
                IService service = _factory.GetService(singleService.Service);
                data.Service = singleService.Service;
                await _dataAccess.GetUserServiceByServiceName(data);
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
