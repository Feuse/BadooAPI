using BadooAPI.Factories;
using DataAccess;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using RabbitMQScheduler;
using RabbitMQScheduler.Interfaces;
using RabbitMQScheduler.ServicesImpl;
using ServicesInterfaces;

namespace Services.Server
{
    public class Startup
    {
        private Quartz.IScheduler _scheduler { get; }
        readonly string allowSpecificOrigins = "AllowAllHeaders";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _scheduler = QuartzInstance.Instance;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.AddCors(options =>
            {
                options.AddPolicy(allowSpecificOrigins, builder =>
                {
                    builder.WithOrigins("https://localhost")
                    .AllowAnyHeader()
                    .AllowCredentials();
                });
            });
            services.AddSingleton<IDataAccess, DataAccess.DataAccess>();
            services.AddTransient<IServicesFactory, ServicesFactory>();
            services.AddTransient<IScheduler, Scheduler>();
            services.AddTransient<IQueue, QueueImpl>();
            services.AddTransient<SchedulerJob>();
            services.AddControllers();
            services.AddSingleton(provider => _scheduler);
            services.AddSession();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.Cookie.HttpOnly = false;
                options.LoginPath = "/login";
                options.Events = new CookieAuthenticationEvents()
                {
                    OnSigningIn = async context =>
                    {
                        var x = context;
                        await System.Threading.Tasks.Task.CompletedTask;
                    }
                    };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(allowSpecificOrigins);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }

            _scheduler.JobFactory = new AspnetCoreJobFactory(app.ApplicationServices);
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }
}
