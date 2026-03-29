using System;
using WebServer.Controllers.Device;
using static System.Formats.Asn1.AsnWriter;

namespace WebServer.Models.Action
{
	public class ActionProcess
	{
        IServiceScope scope;

        public ActionProcess(IServiceScopeFactory scopeFactory)
		{
            scope = scopeFactory.CreateScope();
        }

        public void ActionSave(int actionCode, string userID, ulong actionServerId, ulong actionStationId, ulong actionPowerBankId,uint actionPowerBankSlot, string actionText)
        {
            using (var dbActions = scope.ServiceProvider.GetRequiredService<ActionContext>())
            {
                try
                {
                    var action = new WebServer.Models.Action.Action(DateTime.Now, actionCode, userID, actionServerId, actionStationId, actionPowerBankId, actionPowerBankSlot, "");

                    dbActions.Actions.Add(action);
                    dbActions.SaveChanges();
                }
                catch (Exception ex)
                {
                    var z = ex.Message;
                }
            }
        }

        public void ActionSavePayment(int actionCode, string userID, ulong? actionStationId, ulong? actionPowerBankId, float paymentAmount, string paymentInfo)
        {
            using (var dbActions = scope.ServiceProvider.GetRequiredService<ActionContext>())
            {
                try
                {
                    var action = new WebServer.Models.Action.Action(
                        DateTime.Now,
                        actionCode,
                        userID,
                        0,  // serverId
                        actionStationId ?? 0,
                        actionPowerBankId ?? 0,
                        0,  // slot
                        "",
                        paymentAmount,
                        paymentInfo
                    );

                    dbActions.Actions.Add(action);
                    dbActions.SaveChanges();
                }
                catch (Exception ex)
                {
                    var z = ex.Message;
                }
            }
        }
    }
}

