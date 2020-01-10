using System;
using P4TLB.MasterServer;
using P4TLBMasterServer;

namespace project.P4Classes
{
	public class P4CreateUnitInFormationOnce : ManagerBase
	{
		public override async void OnNotification<T>(object caller, string eventName, T data)
		{
			if (data is OnPlayerFormationRelay relay)
			{
				var formationDbMgr = World.GetOrCreateManager<FormationDatabaseManager>();
				var formation      = formationDbMgr.FindFormation(relay.FormationId);
				if (formation == null)
					Logger.Error("Formation doesn't exist.", true);

				P4Army targetArmy;
				if (formation.Armies.Count == 0)
					formation.Armies.Add((targetArmy = new P4Army()));
				else
					targetArmy = formation.Armies[0];

				if (targetArmy.Units.Count != 0)
					return;

				var unitDbMgr = World.GetOrCreateManager<UnitDatabaseManager>();
				var (success, result) = await unitDbMgr.CreateUnit(relay.UserId, relay.UserId);
				if (!success)
					Logger.Error("Couldn't create unit...", true);

				targetArmy.Units.Add(result.Id);
				formationDbMgr.UpdateFormation(formation);
				Logger.Log("Unit created!");
			}
		}
	}
}