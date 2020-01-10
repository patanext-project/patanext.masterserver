using P4TLBMasterServer;

namespace project.P4Classes
{
	/// <summary>
	/// Manage the preset of player's armies (NOT FORMATIONS!!!)
	/// </summary>
	public class ArmyPresetDatabaseManager : ManagerBase
	{
		private DatabaseManager m_DatabaseManager;

		public override void OnCreate()
		{
			m_DatabaseManager = World.GetOrCreateManager<DatabaseManager>();
		}

		private string GetRootPath()
		{
			return "p4/army_preset:{0}";
		}
	}
}