
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Web;
using WebServer.Models.Identity;
using WebServer.Controllers.Identity;
using System.Net;
using WebServer;

internal class Program
{
    private static void Main(string[] args)
    {
        //var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

        var logger = NLog.LogManager.Setup().LoadConfigurationFromFile(args).GetCurrentClassLogger();

        var host = CreateHostBuilder(args).Build();
        try
        {

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    Task t;
                    var configuration = GetConfiguration();
                    var defAdminPass = configuration.GetSection("DefaultAdminPass").Get<string>();
                    var userManager = services.GetRequiredService<UserManager<AppUser>>();
                    var rolesManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    t = RoleInitializer.InitializeAsync(userManager, rolesManager, defAdminPass);
                    t.Wait();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "An error occurred while seeding the database.");
                }
            }

            host.Run();

        }
        catch (Exception exception)
        {
            //NLog: catch setup errors
            logger.Error(exception, "Stopped program because of exception");
            throw;
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            NLog.LogManager.Shutdown();
        }

    }

    private static IConfiguration GetConfiguration()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (string.IsNullOrEmpty(environmentName))
            environmentName = "Production";

        Console.WriteLine($"Environment: {environmentName}");

        return new ConfigurationBuilder()
            .SetBasePath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: false)
            .Build();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                var configuration = GetConfiguration();
                webBuilder.UseHttpSys(options =>
                {
                    options.Authentication.Schemes = (Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes)AuthenticationSchemes.None;
                    options.Authentication.AllowAnonymous = true;
                    options.MaxConnections = null;
                    options.MaxRequestBodySize = 30000000;
                });
                webBuilder.UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, int.Parse(configuration["BindingPort"]), listenOptions =>
                    {
                        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                        if (string.IsNullOrEmpty(environmentName))
                            throw new Exception($"{nameof(CreateHostBuilder)} -> ASPNETCORE_ENVIRONMENT isnull or empty");

 //                      string pass = configuration?.GetSection("SSL")?.GetSection("CertPass")?.Get<string>();
 //                       pass = !string.IsNullOrEmpty(pass) ? pass : "root";

 //                       listenOptions.UseHttps($"ssl/certificate.{environmentName}.pfx", pass);
                    });
//                    options.ConfigureHttpsDefaults(co => co.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13);
           
                });
                webBuilder.UseStartup<Startup>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            })
            .UseNLog();
}