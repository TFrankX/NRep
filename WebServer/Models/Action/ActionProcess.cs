using System;
using WebServer.Controllers.Device;
using static System.Formats.Asn1.AsnWriter;

namespace WebServer.Models.Action
{
	public class ActionProcess
	{
        private readonly IServiceScopeFactory _scopeFactory;

        public ActionProcess(IServiceScopeFactory scopeFactory)
		{
            _scopeFactory = scopeFactory;
        }

        public void ActionSave(int actionCode, string userID, ulong actionServerId, ulong actionStationId, ulong actionPowerBankId,uint actionPowerBankSlot, string actionText)
        {
            using var scope = _scopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetService<ILogger<ActionProcess>>();
            using (var dbActions = scope.ServiceProvider.GetRequiredService<ActionContext>())
            {
                try
                {
                    var action = new WebServer.Models.Action.Action(DateTime.Now, actionCode, userID, actionServerId, actionStationId, actionPowerBankId, actionPowerBankSlot, "");

                    dbActions.Actions.Add(action);
                    dbActions.SaveChanges();
                    logger?.LogDebug("ActionSave: code={ActionCode}, station={StationId}", actionCode, actionStationId);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "ActionSave failed: code={ActionCode}, station={StationId}", actionCode, actionStationId);
                }
            }
        }

        public void ActionSavePayment(int actionCode, string userID, ulong? actionStationId, ulong? actionPowerBankId, float paymentAmount, string paymentInfo)
        {
            using var scope = _scopeFactory.CreateScope();
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

        /// <summary>
        /// Updates payment info for an existing action by SessionId (stored in UserId field)
        /// </summary>
        public bool UpdatePaymentInfo(string sessionId, string customerName, string paymentInfo)
        {
            using var scope = _scopeFactory.CreateScope();
            using (var dbActions = scope.ServiceProvider.GetRequiredService<ActionContext>())
            {
                try
                {
                    var action = dbActions.Actions
                        .Where(a => a.UserId == sessionId && a.ActionCode == (int)ActionsDescription.PaymentHold)
                        .OrderByDescending(a => a.ActionTime)
                        .FirstOrDefault();

                    if (action != null)
                    {
                        action.UserId = customerName;
                        action.PaymentInfo = paymentInfo;
                        dbActions.SaveChanges();
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    var z = ex.Message;
                    return false;
                }
            }
        }
    }
}

