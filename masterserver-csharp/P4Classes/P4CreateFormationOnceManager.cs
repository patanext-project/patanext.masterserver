using System;
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
		public override void OnNotification<T>(object caller, string eventName, T data)
		{
			if (eventName == "OnUserConnection" && data is OnUserConnection onUserConnection)
			{
				var formationDbMgr = World.GetOrCreateManager<FormationDatabaseManager>();

				OnPlayerFormationRelay relay;
				ulong                    formationId;
				Console.WriteLine("yes: " + formationDbMgr.FindFormationIdByUserId(onUserConnection.User.Id));
				if ((formationId = formationDbMgr.FindFormationIdByUserId(onUserConnection.User.Id)) > 0)
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