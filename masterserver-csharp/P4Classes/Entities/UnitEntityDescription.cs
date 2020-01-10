using P4TLBMasterServer;

namespace project.P4Classes.Entities
{
	public struct UnitEntityDescription : IEntityDescription
	{
		public ulong Id;

		public string GetEntityComponentPath(string componentName)
		{
			if (Id <= 0)
				Logger.Error("Unit with ID 0 found...", true);

			return UnitDatabaseManager.GetUnitComponentPath(Id, componentName);
		}

		public string GetEntityComponentListPath()
		{
			if (Id <= 0)
				Logger.Error("Unit with ID 0 found...", true);

			return UnitDatabaseManager.GetUnitComponentListPath(Id);
		}
	}
}