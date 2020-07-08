using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using P4TLB.MasterServer;

namespace P4TLBMasterServer
{
	public class UserDatabaseManager : ManagerBase
	{
		private DatabaseManager m_DatabaseManager;

		public override void OnCreate()
		{
			m_DatabaseManager = World.GetOrCreateManager<DatabaseManager>();
		}

		/// <summary>
		/// Find an user by his ID
		/// </summary>
		/// <param name="id">The ID</param>
		/// <returns>The user (or null user if no user were found)</returns>
		public async Task<DataUserAccount> FindById(ulong id)
		{
			return await m_DatabaseManager.GetAsync<DataUserAccount>(GetPathId(id));
		}

		/// <summary>
		/// Get the ID of an user with his login
		/// </summary>
		/// <param name="login">The login</param>
		/// <returns>Return an ID, if it's 0 or less, then it couldn't find the user login</returns>
		public async Task<ulong> GetIdFromLogin(string login)
		{
			var id = await m_DatabaseManager.db.StringGetAsync(GetPathLoginToId(login));
			if (!id.HasValue)
				return 0;
			return (ulong) id;
		}

		/// <summary>
		/// Create an account
		/// </summary>
		/// <param name="login"></param>
		/// <param name="success"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public DataUserAccount CreateAccount(string login, out bool success)
		{
			var getIdTask = GetIdFromLogin(login);
			getIdTask.Wait();
			
			if (getIdTask.Result > 0)
			{
				throw new Exception($"An account with login '{login}' already exist.");
			}

			var userAccount = new DataUserAccount
			{
				Id    = (ulong) m_DatabaseManager.db.StringIncrement(GetPathIncrementalId(), 1L),
				Login = login
			};

			success =  m_DatabaseManager.Set(GetPathId(userAccount.Id), userAccount);
			success &= m_DatabaseManager.db.StringSet(GetPathLoginToId(userAccount.Login), userAccount.Id);
			return userAccount;
		}

		// todo: will be removed (this is only to test the discord bot)
		public IEnumerable<DataUserAccount> GetAllUsers(int min, int length)
		{
			var users  = new List<DataUserAccount>();
			length = Math.Min(length, 10);
			if (length <= 0)
				length = 10;

			for (var i = min; i < min + length; i++)
			{
				var user = m_DatabaseManager.Get<DataUserAccount>(GetPathId((ulong) i));
				if (user != null)
					users.Add(user);
			}

			return users;
		}

		/// <summary>
		/// Get user count
		/// </summary>
		/// <returns></returns>
		public ulong GetUserCount()
		{
			return (ulong) m_DatabaseManager.db.StringGet(GetPathIncrementalId());
		}

		/// <summary>
		/// Update an account data
		/// </summary>
		/// <param name="userAccount"></param>
		/// <returns></returns>
		public bool UpdateAccount(DataUserAccount userAccount)
		{
			return m_DatabaseManager.Set(GetPathId(userAccount.Id), userAccount);
		}

		private string GetPathIncrementalId()
		{
			return "user:incremental_id";
		}

		private string GetPathId(ulong id)
		{
			return $"user:id({id})";
		}

		private string GetPathLoginToId(string login)
		{
			return $"user:login_to_id({login})";
		}
	}
}