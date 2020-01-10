using System;
using System.Threading.Tasks;
using P4TLB.MasterServer;
using P4TLBMasterServer;

namespace project.P4Classes
{
	/// <summary>
	/// Manage (mostly) the current armies formation of the player.
	/// </summary>
	public class FormationDatabaseManager : ManagerBase
	{
		public static readonly string NotificationOnFormationUpdate = "Db.OnFormationUpdate";

		public struct OnFormationUpdate
		{
			public P4ArmyFormationRoot Formation;
		}

		private DatabaseManager m_DatabaseManager;

		public override void OnCreate()
		{
			m_DatabaseManager = World.GetOrCreateManager<DatabaseManager>();
		}

		/// <summary>
		/// Create an account
		/// </summary>
		/// <param name="login"></param>
		/// <param name="success"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public P4ArmyFormationRoot CreateFormation(ulong userId, out bool success)
		{
			var formation = new P4ArmyFormationRoot
			{
				Id     = (ulong) m_DatabaseManager.db.StringIncrement(GetPathIncrementalId(), 1L),
				Name   = "Current",
				UserId = userId
			};

			success =  m_DatabaseManager.Set(GetFormationPath(formation.Id), formation);
			success &= m_DatabaseManager.db.StringSet(GetUserToFormationPath(userId), formation.Id);
			return formation;
		}

		public void UpdateFormation(P4ArmyFormationRoot value)
		{
			if (value.Id == 0)
				throw new InvalidOperationException();
			m_DatabaseManager.Set(GetFormationPath(value.Id), value);

			World.Notify(this, NotificationOnFormationUpdate, new OnFormationUpdate {Formation = value});
		}

		public async Task<P4ArmyFormationRoot> FindFormation(ulong formationId)
		{
			if (formationId == 0)
				return null;
			return await m_DatabaseManager.GetAsync<P4ArmyFormationRoot>(GetFormationPath(formationId));
		}

		public async Task<uint> FindFormationIdByUserId(ulong userId)
		{
			var rv = await m_DatabaseManager.db.StringGetAsync(GetUserToFormationPath(userId));
			if (!rv.HasValue)
				return 0;
			return (uint) rv;
		}

		/// <summary>
		/// Get formation count
		/// </summary>
		/// <returns></returns>
		public ulong GetFormationCount()
		{
			return (ulong) m_DatabaseManager.db.StringGet(GetPathIncrementalId());
		}

		private string GetPathIncrementalId()
		{
			return string.Format(GetRootPath(), "incremental_id");
		}

		private static string GetFormationPath(ulong formationId)
		{
			return string.Format(GetRootPath(), $"id({formationId})");
		}

		// somewhat temporary rn, once there will be a multiple save system, this function will be removed
		private static string GetUserToFormationPath(ulong userId)
		{
			return string.Format(GetRootPath(), $"user_to_formation_id({userId})");
		}

		private static string GetRootPath()
		{
			return "p4/formation:{0}";
		}
	}
}