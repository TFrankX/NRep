using Microsoft.AspNetCore.Identity;

namespace WebServer.Models

{
    public class LayoutViewModel
    {
        public bool ShowReports { get; set; }
        public bool ShowDashboard { get; set; }
        public bool ShowAdmin { get; set; }

        public void SetViewModel()
        {
            //if (User.Identity.IsAuthenticated)
            //{
            //    if (HttpContext.Current.User.IsInRole("Reporters"))
            //    {
            //        ShowReports = true;
            //    }
            //    if (HttpContext.Current.User.IsInRole("Administrators"))
            //    {
            //        ShowDashboard = true;
            //        ShowAdmin = true;
            //    }
            //}
        }
    }
}