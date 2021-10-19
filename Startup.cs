using BadooAPI.Factories;
using BadooAPI.Utills;
using DataAccess;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using ServicesInterfaces;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Scheduler;
using ServicesInterfaces.Scheduler;
using MessagesQueue;
using Quartz;
using Services.Server.Utills;
using ServicesInterfaces.Global;
using Autofac;
using BadooAPI;
using Divergic.Configuration.Autofac;
using System;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;
using ServicesFacade;
using ServicesInterfaces.Facades;
using ServicesInterfaces.DataAccess;
using ServicesInterfaces.DataAccess.Cache;
using DataAccess.Cache;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using Autofac.Extensions.DependencyInjection;
using System.Collections.Specialized;

namespace Services.Server
{
    public class Startup
    {
        public ILifetimeScope AutofacContainer { get; private set; }

        private Microsoft.Extensions.Logging.ILogger Logger { get; }
        private Quartz.IScheduler _scheduler { get; set; }
        public IConfiguration Configuration { get; }
        readonly string allowSpecificOrigins = "AllowAllHeaders";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            var nlogLoggerProvider = new NLogLoggerProvider();
            Logger = nlogLoggerProvider.CreateLogger(typeof(Startup).FullName);
            _scheduler = QuartzInstance.Instance;
        }
       
      
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
         

            #region Configuration file
            var redisConfiguration = Configuration.GetSection("AppSettings:Redis").Get<RedisConfiguration>();
            
            services.AddStackExchangeRedisCache(options=>
                options.ConfigurationOptions = redisConfiguration.ConfigurationOptions);

            services.Configure<AppSettings>(
                Configuration.GetSection(nameof(AppSettings)));

            services.AddSingleton<IAppSettings>(sp =>
               sp.GetRequiredService<IOptions<AppSettings>>().Value);
            #endregion

            #region Facades 
            services.AddSingleton<IActionsFacade, ActionsFacade>();
            services.AddSingleton<ILoginFacade, LoginFacade>();
            services.AddSingleton<IUserFacade, UserFacade>();
            services.AddSingleton<IUserServicesFacade, UserServicesFacade>();
            services.AddSingleton<IServicesFactory, ServicesFactory>();
            #endregion

            #region DataAccess
            services.AddSingleton<IServiceDataAccess, ServicesDataAccess>();
            services.AddSingleton<IUserDataAccess, UserDataAccess>();
            services.AddSingleton<IUserCacheAccess, UserCacheAccess>();
            services.AddSingleton<IServiceCacheAccess, ServicesCacheAccess>();
            services.AddSingleton<IDataAccessManager, DataAccessManager>();

            var options = new DistributedCacheEntryOptions()
                 .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));
            services.AddSingleton<DistributedCacheEntryOptions>(options);
            #endregion

            #region Scheduler
            services.AddTransient<ServicesInterfaces.Scheduler.IScheduler, Scheduler.Scheduler>();
            services.AddTransient<SchedulerJob>();
            services.AddSingleton(provider => _scheduler);
            #endregion

            #region Queue
            services.AddTransient<IQueue, Queue>();
            #endregion

            #region Utills
            services.AddAutoMapper(typeof(DataMapper));
            #endregion

            services.AddCors(o => o.AddPolicy("AllowOrigins", builder =>
            {
                //builder.WithOrigins("https://localhost", "https://www.autolovers.com")
                      // .AllowAnyMethod()
                      // .AllowCredentials()
                       //.AllowAnyHeader();
            }));
            
            services.AddControllers();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.Cookie.HttpOnly = true;
               // options.Cookie.Domain = ".autolovers.com";
                // options.LoginPath = "/login";
            });
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            try
            {
                _scheduler.JobFactory = new AspnetCoreJobFactory(app.ApplicationServices);
            }
            catch (Exception e)
            {
                Logger.LogCritical(e.Message);
                Logger.LogTrace(e.StackTrace);
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowOrigins");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }
}
