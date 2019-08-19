using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using P4TLB.MasterServer;
using StackExchange.Redis;

namespace P4TLBMasterServer
{
	public class DatabaseManager : ManagerBase
	{
		private ConnectionMultiplexer m_RedisConnection;

		/// <summary>
		/// Current alive redis connection
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		public ConnectionMultiplexer redisConnection
		{
			get
			{
				if (m_RedisConnection == null)
					throw new InvalidOperationException("Not init.");
				return m_RedisConnection;
			}
		}

		/// <summary>
		/// First database of the redis connection
		/// </summary>
		public IDatabase db => redisConnection.GetDatabase();

		/// <summary>
		/// Set an already alive connection to this database
		/// </summary>
		/// <param name="connectionMultiplexer">The connection</param>
		public void SetConnection(ConnectionMultiplexer connectionMultiplexer)
		{
			m_RedisConnection = connectionMultiplexer;
		}

		/// <summary>
		/// Set a value as a bitified string with a key.
		/// </summary>
		/// <param name="key">The key</param>
		/// <param name="obj">The value</param>
		/// <param name="database">The database</param>
		/// <typeparam name="T">The type of the value</typeparam>
		/// <returns>If it was success, it return true</returns>
		public bool Set<T>(RedisKey key, T obj, IDatabase database = null)
			where T : class, IMessage<T>
		{
			database = database ?? db;
			return database.StringSet(key, obj.ToByteArray());
		}

		/// <summary>
		/// Get a bitified string value from a key
		/// </summary>
		/// <param name="key">The key</param>
		/// <param name="database">The database</param>
		/// <typeparam name="T">The type of the value</typeparam>
		/// <returns>The value</returns>
		public T Get<T>(RedisKey key, IDatabase database = null)
			where T : class, IMessage<T>, new()
		{
			database = database ?? db;

			var data = database.StringGet(key);
			if (data.IsNullOrEmpty)
				return null;
			var obj = new T();
			obj.MergeFrom((byte[]) database.StringGet(key));
			return obj;
		}
	}

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
		public DataUserAccount FindById(ulong id)
		{
			return m_DatabaseManager.Get<DataUserAccount>(GetPathId(id));
		}

		/// <summary>
		/// Get the ID of an user with his login
		/// </summary>
		/// <param name="login">The login</param>
		/// <returns>Return an ID, if it's 0 or less, then it couldn't find the user login</returns>
		public ulong GetIdFromLogin(string login)
		{
			var id = m_DatabaseManager.db.StringGet(GetPathLoginToId(login));
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
			if (GetIdFromLogin(login) > 0)
			{
				throw new Exception($"An account with login '{login}' already exist.");
			}

			var userAccount = new DataUserAccount
			{
				Id = (ulong) m_DatabaseManager.db.StringIncrement(GetPathIncrementalId(), 1L),
				Login = login
			};

			success = m_DatabaseManager.Set(GetPathId(userAccount.Id), userAccount);
			success &= m_DatabaseManager.db.StringSet(GetPathLoginToId(userAccount.Login), userAccount.Id);
			return userAccount;
		}

		// todo: will be removed (this is only to test the discord bot)
		public IEnumerable<DataUserAccount> GetAllUsers(int min, int length)
		{
			var ep = m_DatabaseManager.redisConnection.GetEndPoints().FirstOrDefault();
			if (ep == null)
				return null;

			var users = new List<DataUserAccount>();
			var server = m_DatabaseManager.redisConnection.GetServer(ep);
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