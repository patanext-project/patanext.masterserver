using System;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using P4TLBMasterServer.Events;

namespace project.P4Classes
{
	// todo: TEMPORARY
	public struct OnPlayerFormationRelay
	{
		public ulong UserId;
		public ulong FormationId;
	}

	public class P4CreateFormationOnceManager : ManagerBase
	{
		public override async void OnNotification<T>(object caller, string eventName, T data)
		{
			if (eventName == "OnUserConnection" && data is OnUserConnection onUserConnection)
			{
				if (onUserConnection.User.Type == AccountType.Server)
					return;
				
				var formationDbMgr = World.GetOrCreateManager<FormationDatabaseManager>();

				OnPlayerFormationRelay relay;
				ulong                    formationId;
				if ((formationId = await formationDbMgr.FindFormationIdByUserId(onUserConnection.User.Id)) > 0)
				{
					relay = new OnPlayerFormationRelay {UserId = onUserConnection.User.Id, FormationId = formationId};
					World.Notify(this, "OnPlayerFormationRelay", relay);
					return;
				}

				formationId = formationDbMgr.CreateFormation(onUserConnection.User.Id, out var success).Id;
				Logger.Log("Formation created!");

				relay = new OnPlayerFormationRelay {UserId = onUserConnection.User.Id, FormationId = formationId};
				World.Notify(this, "OnPlayerFormationRelay", relay);
			}
		}
	}
}