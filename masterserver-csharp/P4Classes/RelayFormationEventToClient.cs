using P4TLB.MasterServer;
using P4TLBMasterServer;

namespace project.P4Classes
{
	public class RelayFormationEventToClient : ManagerBase
	{
		public override void OnNotification<T>(object caller, string eventName, T data)
		{
			if (eventName == FormationDatabaseManager.NotificationOnFormationUpdate && data is FormationDatabaseManager.OnFormationUpdate ev)
			{
				var clientMgr = World.GetOrCreateManager<ClientManager>();
				if (!clientMgr.TryGetClientFromUserId(ev.Formation.UserId, out var client))
				{
					Logger.Warning($"[RelayFormationToEventClient] No client for user '{ev.Formation.UserId}' found...");
					return;
				}

				var eventList = clientMgr.GetOrCreateData<ClientEventList>(client);
				eventList.Add(nameof(P4PlayerEvents.OnFormationUpdate));
			}
		}
	}
}