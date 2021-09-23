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

namespace Services.Server
{
    public class Startup
    {
        private Quartz.IScheduler _scheduler { get; }
        public IConfiguration Configuration { get; }
        readonly string allowSpecificOrigins = "AllowAllHeaders";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _scheduler = QuartzInstance.Instance;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddStackExchangeRedisCache(options => options.Configuration = Configuration.GetConnectionString("Redis"));
            services.Configure<AutoLoverDatabaseSettings>(
       Configuration.GetSection(nameof(AutoLoverDatabaseSettings)));

            services.AddSingleton<IAutoLoverDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<AutoLoverDatabaseSettings>>().Value);
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

            services.AddTransient<IServicesFactory, ServicesFactory>();
            services.AddTransient<IScheduler, Scheduler.Scheduler>();
            services.AddTransient<IQueue, Queue>();
            services.AddTransient<SchedulerJob>();
            services.AddControllers();
            services.AddSingleton(provider => _scheduler);
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
