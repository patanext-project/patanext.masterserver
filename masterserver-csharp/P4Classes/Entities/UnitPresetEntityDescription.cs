using P4TLBMasterServer;

namespace project.P4Classes.Entities
{
	// Unit Presets are useful to represent an actual kit data, or a preset that can be shared with others.
	// An unit contains multiple unique presets with an attached class. (so for example it would have a Shurika and Taterazay preset if we possess these classes)
	public struct UnitPresetEntityDescription : IEntityDescription
	{
		public ulong Id;

		public string GetEntityIdPath()
		{
			if (Id <= 0)
				Logger.Error("UnitPreset with ID 0 found...", true);

			return UnitPresetDatabaseManager.GetPath(Id);
		}

		public string GetEntityComponentPath(string componentName)
		{
			if (Id <= 0)
				Logger.Error("UnitPreset with ID 0 found...", true);

			return UnitPresetDatabaseManager.GetComponentPath(Id, componentName);
		}

		public string GetEntityComponentListPath()
		{
			if (Id <= 0)
				Logger.Error("UnitPreset with ID 0 found...", true);

			return UnitPresetDatabaseManager.GetComponentListPath(Id);
		}
	}
}