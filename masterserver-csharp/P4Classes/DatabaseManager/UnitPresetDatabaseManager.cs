using System;
using System.Threading.Tasks;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using project.P4Classes.Entities;

namespace project.P4Classes
{
	public class UnitPresetDatabaseManager : ManagerBase
	{
		private DatabaseManager databaseManager;

		public override void OnCreate()
		{
			base.OnCreate();

			databaseManager = World.GetOrCreateManager<DatabaseManager>();
		}

		public async Task<(bool success, P4UnitData result)> CreateUnitPresetFor(UnitEntityDescription entity)
		{
			throw new NotImplementedException("CreateUnitPresetFor");
		}

		private static string GetIncrementalPath()
		{
			return string.Format(GetRootPath(), "incremental_count");
		}

		public static string GetComponentListPath(ulong unitId)
		{
			return $"{GetPath(unitId)}_components";
		}

		public static string GetComponentPath(ulong unitId, string component)
		{
			return $"{GetPath(unitId)}_{component}_data";
		}

		public static string GetPath(ulong unitId)
		{
			return string.Format(GetRootPath(), $"id({unitId})");
		}

		public static string GetRootPath()
		{
			return "p4/unit_preset:{0}";
		}
	}
}