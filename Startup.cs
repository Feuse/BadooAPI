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

namespace Services.Server
{
    public class Startup
    {
        private Quartz.IScheduler _scheduler { get; set; }
        public IConfiguration Configuration { get; }
        readonly string allowSpecificOrigins = "AllowAllHeaders";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // Autofac container builder
        public void ConfigureContainer(ContainerBuilder builder)
        {

            var someSettings = Configuration.GetSection(typeof(AppSettings).Name).Get<AppSettings>();

            builder.Register(c => someSettings).As<IAppSettings>();

            builder.RegisterType<QuartzInstance>()
        .WithProperty(nameof(AppSettings), someSettings);
            _scheduler = QuartzInstance.Instance;

        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddStackExchangeRedisCache(options => options.Configuration = Configuration.GetConnectionString("Redis"));

            services.Configure<AppSettings>(
       Configuration.GetSection(nameof(AppSettings)));

            services.AddSingleton<IAppSettings>(sp =>
               sp.GetRequiredService<IOptions<AppSettings>>().Value);

            services.AddCors(o => o.AddPolicy("AllowOrigins", builder =>
            {
                builder.WithOrigins("https://localhost", "https://autolovers.000webhostapp.com")
                       .AllowAnyMethod()
                       .AllowCredentials()
                       .AllowAnyHeader();
            }));

            services.AddSingleton<IDataAccess, ServicesDataAccess>();
         
            services.AddSingleton<ICacheDataAccess, ServicesDataAccessCache>();
            services.AddSingleton<IDataAccessManager, DataAccessManager>();

            services.AddTransient<ServicesInterfaces.Scheduler.IScheduler, Scheduler.Scheduler>();
            services.AddTransient<IQueue, Queue>();
            services.AddTransient<SchedulerJob>();
            services.AddControllers();
            services.AddSingleton(provider => _scheduler);
            services.AddAutoMapper(typeof(DataMapper));
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.Cookie.HttpOnly = true;
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

            _scheduler.JobFactory = new AspnetCoreJobFactory(app.ApplicationServices);
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
