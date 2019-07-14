using System;
using Google.Protobuf;
using StackExchange.Redis;

namespace P4TLBMasterServer
{
	public class DatabaseManager : ManagerBase
	{
		private ConnectionMultiplexer m_RedisConnection;

		public ConnectionMultiplexer redisConnection
		{
			get
			{
				if (m_RedisConnection == null)
					throw new InvalidOperationException("Not init.");
				return m_RedisConnection;
			}
		}

		public IDatabase db => redisConnection.GetDatabase();

		public void SetConnection(ConnectionMultiplexer connectionMultiplexer)
		{
			m_RedisConnection = connectionMultiplexer;
		}

		public bool Set<T>(RedisKey key, T obj, IDatabase database = null)
			where T : class, IMessage<T>
		{
			database = database ?? db;
			return database.StringSet(key, obj.ToByteArray());
		}

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

		public DataUserAccount FindById(ulong id)
		{
			return m_DatabaseManager.Get<DataUserAccount>(GetPathId(id));
		}

		public ulong GetIdFromLogin(string login)
		{
			var id = m_DatabaseManager.db.StringGet(GetPathLoginToId(login));
			if (!id.HasValue)
				return 0;
			return (ulong) id;
		}

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