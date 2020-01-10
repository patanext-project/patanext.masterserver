using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
			database ??= db;
			return database.StringSet(key, obj.ToByteArray());
		}
		
		/// <summary>
		/// Set a value as a bitified string with a key (async version)
		/// </summary>
		/// <param name="key">The key</param>
		/// <param name="obj">The value</param>
		/// <param name="database">The database</param>
		/// <typeparam name="T">The type of the value</typeparam>
		/// <returns>If it was success, it return true</returns>
		public Task<bool> SetAsync<T>(RedisKey key, T obj, IDatabase database = null)
			where T : class, IMessage<T>
		{
			database ??= db;
			return database.StringSetAsync(key, obj.ToByteArray());
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
			database ??= db;

			var data = database.StringGet(key);
			if (data.IsNullOrEmpty)
				return null;
			var obj = new T();
			obj.MergeFrom((byte[]) database.StringGet(key));
			return obj;
		}

		public async Task<T> GetAsync<T>(RedisKey key, IDatabase database = null)
			where T : class, IMessage<T>, new()
		{
			database ??= db;

			var data = await database.StringGetAsync(key);
			if (data.IsNullOrEmpty)
				return null;
			var obj = new T();
			obj.MergeFrom((byte[]) data);
			return obj;
		}
	}
}