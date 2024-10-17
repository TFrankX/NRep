using System;
using Microsoft.AspNetCore.Identity;
using WebServer.Models.Identity;

namespace WebServer.Models.Action
{
	public class ActionMessages
	{
		public List<ActionMessage> Content { get; set; }
        UserManager<AppUser> userManag;

        public ActionMessages(UserManager<AppUser> userManager)
		{
			userManag = userManager;

        }

		public void AddMessage(Action action)
		{
			ActionMessage ContentAdd = new ActionMessage();
			ContentAdd.ActionTime = action.ActionTime;


            //var user = userManag.Users.Where(x => x.Id == action.UserId).First();
            //var user = await userManag.FindByNameAsync(model.PhoneNumber);


		}

	}
}

