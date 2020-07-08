using System;
using System.Collections.Generic;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using project.P4Classes.Components;
using project.P4Classes.Entities;

namespace project.P4Classes
{
	public class ClientUnitUpdateEvent
	{
		public HashSet<ulong> HashSet = new HashSet<ulong>();
	}

	public class RelayStandardEventToClient : ManagerBase
	{
		public override async void OnNotification<T>(object caller, string eventName, T data)
		{
			var clientMgr = World.GetOrCreateManager<ClientManager>();
			if (eventName == FormationDatabaseManager.NotificationOnFormationUpdate && data is FormationDatabaseManager.OnFormationUpdate ev)
			{
				TryAddEventList(ev.Formation.UserId, nameof(P4PlayerEvents.OnFormationUpdate), in clientMgr, out _);
			}
			else if (eventName == EntityManager.NotificationOnEntityUpdate && data is IEntityUpdateKey<UnitEntityDescription> entityUpdate)
			{
				var unit = await World.GetOrCreateManager<UnitDatabaseManager>()
				                      .FindUnit(entityUpdate.Key.Id);

				if (TryAddEventList(unit.UserId, nameof(P4PlayerEvents.OnUnitUpdate), in clientMgr, out var client))
				{
					var onUnitUpdateEvent = clientMgr.GetOrCreateData<ClientUnitUpdateEvent>(client);
					onUnitUpdateEvent.HashSet.Add(entityUpdate.Key.Id);
				}

				var server = clientMgr.GetOrCreateData<UserServerLink>(client);
				if (server.ServerId > 0 && TryAddEventList(server.ServerId, nameof(P4PlayerEvents.OnUnitUpdate), in clientMgr, out var serverClient))
				{
					var onUnitUpdateEvent = clientMgr.GetOrCreateData<ClientUnitUpdateEvent>(serverClient);
					onUnitUpdateEvent.HashSet.Add(entityUpdate.Key.Id);
				}
			}
		}

		private bool TryAddEventList(ulong userId, string data, in ClientManager clientMgr, out Client client)
		{
			if (!clientMgr.TryGetClientFromUserId(userId, out client))
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