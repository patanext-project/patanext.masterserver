using System.Threading.Tasks;
using P4TLB.MasterServer;
using P4TLBMasterServer;

namespace project.P4Classes
{
	public interface IOnRelatedUnitEvent
	{
		P4UnitData UnitData { get; set; }
	}

	public struct OnUnitCreated : IOnRelatedUnitEvent
	{
		public P4UnitData UnitData { get; set; }
	}

	public struct OnUnitUpdated : IOnRelatedUnitEvent
	{
		public P4UnitData UnitData { get; set; }
	}

	/// <summary>
	/// An unit should NOT possess a lot of data by itself, like an ECS entity.
	/// An unit only possess:
	/// - His own id
	/// - An owner
	/// - The save id
	/// </summary>
	public class UnitDatabaseManager : ManagerBase
	{
		private DatabaseManager databaseManager;

		public override void OnCreate()
		{
			base.OnCreate();

			databaseManager = World.GetOrCreateManager<DatabaseManager>();
		}

		public async Task<(bool success, P4UnitData result)> CreateUnit(ulong userId, ulong saveId)
		{
			var unit = new P4UnitData
			{
				Id     = (ulong) databaseManager.db.StringIncrement(GetIncrementalPath(), 1L),
				UserId = userId,
				SaveId = saveId
			};

			(bool success, P4UnitData result) result;
			result.result  = unit;
			result.success = await databaseManager.SetAsync(GetUnitPath(unit.Id), unit);
			// todo: use saveId and not userId for setting the value GetSaveUnitLinkPath...
			result.success &= await databaseManager.db.StringSetAsync(GetSaveUnitLinkPath(userId, unit.Id), unit.Id);

			World.Notify(this, string.Empty, new OnUnitCreated {UnitData = unit});

			return result;
		}

		public async Task<P4UnitData> FindUnit(ulong unitId)
		{
			return await databaseManager.GetAsync<P4UnitData>(GetUnitPath(unitId));
		}

		private static string GetIncrementalPath()
		{
			return string.Format(GetRootPath(), "incremental_count");
		}

		public static string GetUnitComponentListPath(ulong unitId)
		{
			return $"{GetUnitPath(unitId)}_components";
		}

		public static string GetUnitComponentPath(ulong unitId, string component)
		{
			return $"{GetUnitPath(unitId)}_{component}_data";
		}

		public static string GetUnitPath(ulong unitId)
		{
			return string.Format(GetRootPath(), $"id({unitId})");
		}

		public static string GetSaveUnitLinkPath(ulong saveId, ulong unitId)
		{
			return string.Format(GetRootPath(), $"save_unit_link({saveId})_({unitId})");
		}

		public static string GetRootPath()
		{
			return "p4/unit:{0}";
		}
	}
}