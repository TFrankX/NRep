using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.Identity;
using WebServer.Models.Device;
using WebServer.Models.Action;
using WebServer.Workers;
using WebServer.Models.Settings;
using Microsoft.Extensions.Hosting;
using WebServer.Data;
using ProtoBuf.Meta;
using NLog;
using NLog.Web;

namespace WebServer
{
    public class Startup
    {
        public List<Server> Servers;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        private IServiceCollection Services;
        private IServiceScopeFactory ScopeFactory;

        public IConfiguration Configuration { get; }



        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            //IServiceScopeFactory scopeFactory;

            //            services.AddDbContextPool<DeviceContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteDevice")));
            //            services.AddDbContext<AppIdentityContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteAppAccounts")));
            //            services.AddDbContextPool<SettingsContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteSettings")));
            //            //services.AddSingleton<IDevActionTable, DevActionTable>();
            //            services.AddDbContextPool<ActionContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteActions")));

            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrEmpty(environmentName))
                environmentName = "Development";


            //environmentName = "Production";
            if (environmentName == "Development")
            {
                            services.AddDbContextPool<DeviceContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteDevice")));
                            services.AddDbContext<AppIdentityContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteAppAccounts")));
                //            services.AddDbContextPool<SettingsContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteSettings")));
                //            //services.AddSingleton<IDevActionTable, DevActionTable>();
                            services.AddDbContextPool<ActionContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteActions")));
            }

            if (environmentName == "Production")
            {
                services.AddDbContextPool<DeviceContext>(options =>
                {
                    options.UseNpgsql(Configuration.GetConnectionString("pgDevice"));
                });
                services.AddDbContext<AppIdentityContext>(options => options.UseNpgsql(Configuration.GetConnectionString("pgAppAccounts")));
                services.AddDbContextPool<ActionContext>(options => options.UseNpgsql(Configuration.GetConnectionString("pgActions")));

            }




            //services.AddDbContextPool<SettingsContext>(options => options.UseNpgsql(Configuration.GetConnectionString("pgSettings")));
            //services.AddSingleton<IDevActionTable, DevActionTable>();


            services.AddSingleton<IDevicesData, DevicesData>();
            services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
            services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AppIdentityContext>();
            //services.AddHostedService<ScanDevices>(serviceProvider =>
            //        new ScanDevices(Servers, serviceProvider.GetService<IServiceScopeFactory>()));
            services.AddIdentityCore<AppUser>(options => options.SignIn.RequireConfirmedAccount = false)
                 .AddEntityFrameworkStores<AppIdentityContext>()
                 .AddTokenProvider<DataProtectorTokenProvider<AppUser>>(TokenOptions.DefaultProvider);
            services.AddSingleton<ScanDevices>();
            services.AddHostedService<ScanDevices>(p => p.GetRequiredService<ScanDevices>());

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddDistributedMemoryCache();

            

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.Configure<IdentityOptions>(options =>
            {
                // Default SignIn settings.
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
                options.Password.RequiredLength = 3;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;

            });

            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

                options.LoginPath = "/AppAccount/AppAccountLogin";
                options.AccessDeniedPath = "/AppAccount/AccessDenied";
                options.SlidingExpiration = true;

            });

            services.AddSignalR(o =>
            {
                o.EnableDetailedErrors = true;
                o.MaximumReceiveMessageSize = null; // bytes
            });
            services.AddRazorPages();
            Services = services;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime hostAapplicationLifetime,IServiceScopeFactory scopeFactory)
        {
            ScopeFactory = scopeFactory;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePagesWithReExecute("/Errors/{0}");
                app.UseHsts();
            }

            hostAapplicationLifetime.ApplicationStopping.Register(OnShutDown);
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllerRoute(
                //    name: "default",
                //    pattern: "{controller=Servers}/{action=Servers}");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=AppAccount}/{action=AppAccountLogin}");



                //                endpoints.MapControllerRoute(
                //                    name: "default",
                //                    pattern: "{controller=Administration}/{action=Administration}");
                ////endpoints.MapControllerRoute(
                ////    name: "default",
                ////    pattern: "{controller=AppAccount}/{action=AppAccountLogin}");
                //endpoints.MapControllerRoute(
                //    name: "default",
                //    pattern: "{controller=WebDocSection}/{action=GetChartData}");
                //endpoints.MapHub<DockMonHub>("/dockmonhub");
                endpoints.MapRazorPages();

                //endpoints.MapGet("/", async context =>
                //{
                //    await context.Response.WriteAsync("Hello World!");
                // });
            });
        }

        private void OnShutDown()
        {
            //var actionProcess = new ActionProcess(scopeFactory);
            //var sp = Services.BuildServiceProvider();
            //var service =  sp.GetService<ScanDevices>();

            // ActSave();
            var actionProcess = new ActionProcess(ScopeFactory);
            actionProcess.ActionSave((int)ActionsDescription.ServiceShutdown, "System", 0, 0, 0,0, "");
        }


    }
}
