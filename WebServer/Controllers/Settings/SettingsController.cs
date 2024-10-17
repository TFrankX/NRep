using System.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Settings;


namespace WebServer.Controllers.Settings
{
    public class SettingsController : Controller
    {
        private WebServer.Models.Settings.SettingsContext db;
        public SettingsController(WebServer.Models.Settings.SettingsContext context)
        {
            db = context;
        }

        [Authorize(Roles = "admin")]
        public IActionResult Settings()
        {
            return View();
        }
    }
}
