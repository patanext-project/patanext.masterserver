using System.Collections.Generic;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using project.P4Classes.Components;
using project.P4Classes.Entities;

namespace project.P4Classes
{
	public class ClientUnitUpdateEvent
	{
		public HashSet<uint> HashSet = new HashSet<uint>();
	}
	
	public class RelayStandardEventToClient : ManagerBase
	{
		public override async void OnNotification<T>(object caller, string eventName, T data)
		{
			if (eventName == FormationDatabaseManager.NotificationOnFormationUpdate && data is FormationDatabaseManager.OnFormationUpdate ev)
			{
				TryAddEventList(ev.Formation.UserId, nameof(P4PlayerEvents.OnFormationUpdate));
			}
			else if (eventName == EntityManager.NotificationOnEntityUpdate && data is IEntityUpdateKey<UnitEntityDescription> entityUpdate)
			{
				var unit = await World.GetOrCreateManager<UnitDatabaseManager>()
				                      .FindUnit(entityUpdate.Key.Id);
				TryAddEventList(unit.UserId, nameof(P4PlayerEvents.OnUnitUpdate));
			}
		}

		private bool TryAddEventList(ulong userId, string data)
		{
			var clientMgr = World.GetOrCreateManager<ClientManager>();
			if (!clientMgr.TryGetClientFromUserId(userId, out var client))
			{
				Logger.Warning($"[RelayStandardEventToClient] No client for user '{userId}' found...");
				return false;
			}

			clientMgr.GetOrCreateData<ClientEventList>(client)
			         .Add(data);
			return true;
		}
	}
}