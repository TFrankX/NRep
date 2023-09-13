
using NLog;

internal class Program
{
    private static void Main(string[] args)
    {
        //var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

        var logger = NLog.LogManager.Setup().LoadConfigurationFromFile(args).GetCurrentClassLogger();
        var builder = WebApplication.CreateBuilder(args);
        logger.Debug("Starting service...");
        // Add services to the container.
        builder.Services.AddRazorPages();
        var app = builder.Build();
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();
        app.MapRazorPages();

        app.Run();
    }
}