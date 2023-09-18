using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.Identity;
using WebServer.Models.Device;
using WebServer.Workers;
using Microsoft.Extensions.Hosting;

namespace WebServer
{
    public class Startup
    {
        public List<Server> Servers;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }



        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //IServiceScopeFactory scopeFactory;

            services.AddDbContextPool<DeviceContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteDevice")));
            services.AddDbContext<AppIdentityContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteAppAccounts")));
           // services.AddSingleton<IAlertService, AlertService>();
            services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
            services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AppIdentityContext>();
            //services.AddHostedService<ScanDevices>(serviceProvider =>
            //        new ScanDevices(Servers, serviceProvider.GetService<IServiceScopeFactory>()));
            services.AddHostedService<ScanDevices>();
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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePagesWithReExecute("/Errors/{0}");
                app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //                endpoints.MapControllerRoute(
                //                    name: "default",
                //                    pattern: "{controller=Administration}/{action=Administration}");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=AppAccount}/{action=AppAccountLogin}");
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

    }
}
