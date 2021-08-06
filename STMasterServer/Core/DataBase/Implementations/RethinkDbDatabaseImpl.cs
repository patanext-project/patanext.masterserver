using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using BidirectionalMap;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;

namespace project.DataBase.Implementations
{
	public class RethinkDbDatabaseImpl : IEntityDatabase
	{
		private static readonly RethinkDB R = RethinkDB.R;

		public readonly IConnection Connection;

		private readonly BiMap<long, Guid>          entityToDbGuid            = new BiMap<long, Guid>();
		private          Dictionary<string, object> temporaryInsertDictionary = new Dictionary<string, object>();

		private Dictionary<string, bool> tableExistMap = new ();

		public RethinkDbDatabaseImpl(IConnection connection)
		{
			Connection = connection;
		}

		public DbEntityKey<T> CreateEntity<T>(string? wantedRepresentation = null) where T : IEntityDescription
		{
			// todo: cache the queryexp? (gc)

			if (wantedRepresentation == null)
			{
				var result = GetEntityTable<T>().Result
				                                .Insert(new object())
				                                .RunWrite(Connection);
				return new DbEntityKey<T>(this, GetOrCreateEntityLong(result.GeneratedKeys[0]));
			}
			else if (Guid.TryParse(wantedRepresentation, out var guid))
			{
				var result = GetEntityTable<T>().Result
				                                .Insert(new IdOnlyStruct(guid))
				                                .RunWrite(Connection);
				return new DbEntityKey<T>(this, GetOrCreateEntityLong(guid));
			}
			
			throw new NotImplementedException("Non GUID representation not implemented yet");
		}

		public async ValueTask ReplaceComponentAsync<TEntity, TComponent>(DbEntityKey<TEntity> entity, TComponent value) where TEntity : IEntityDescription
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var guid   = entityToDbGuid.Forward[entity.Id];
			var baseRq = GetEntityTable<TEntity>().Result.Get(guid.ToString());

			var name = typeof(TComponent).Name;
			await baseRq.Replace(p => p.Without(name))
			            .RunWriteAsync(Connection);
			await baseRq.Update(new JObject
			{
				{name, JObject.FromObject(value)}
			}).RunWriteAsync(Connection);
		}

		public async ValueTask<bool> HasComponentAsync<TEntity, TComponent>(DbEntityKey<TEntity> entity) where TEntity : IEntityDescription
		{
			var guid = entityToDbGuid.Forward[entity.Id];
			//Console.WriteLine($"Has {typeof(TComponent)} on {typeof(TEntity)}<{guid}>(#{entity.Id})");
			return await (await GetEntityTable<TEntity>()).Get(guid.ToString()).HasFields(typeof(TComponent).Name).RunResultAsync<bool>(Connection);
		}

		public async ValueTask<TComponent> GetComponentAsync<TEntity, TComponent>(DbEntityKey<TEntity> entity) where TEntity : IEntityDescription
		{
			var guid = entityToDbGuid.Forward[entity.Id];
			//Console.WriteLine($"Get {typeof(TComponent)} on {typeof(TEntity)}<{guid}>(#{entity.Id})");
			return await (await GetEntityTable<TEntity>()).Get(guid.ToString()).GetField(typeof(TComponent).Name).RunResultAsync<TComponent>(Connection);
		}

		public async ValueTask<MatchFilter<TEntity>> GetMatchFilter<TEntity>() where TEntity : IEntityDescription
		{
			return new RethinkDbMatchFilter<TEntity>(this, await GetEntityTable<TEntity>());
		}

		public string RepresentationOf<T>(DbEntityKey<T> entity) where T : IEntityDescription
		{
			return entityToDbGuid.Forward[entity.Id].ToString();
		}

		public DbEntityKey<T> GetEntity<T>(ReadOnlySpan<char> representation) where T : IEntityDescription
		{
			if (Guid.TryParse(representation, out var guid))
				return new DbEntityKey<T>(this, GetOrCreateEntityLong(guid));

			// what should we return exactly?
			Console.WriteLine("no entity found for rep: " + representation.ToString() + "\n" + Environment.StackTrace);
			return default;
		}

		public async Task<Table> GetEntityTable<T>()
			where T : IEntityDescription
		{
			var target = typeof(T).Name;
			lock (tableExistMap)
			{
				if (!tableExistMap.ContainsKey(target))
				{
					R.TableList()
					 .Contains(target)
					 .Do_(tableExists => R.Branch(tableExists, R.Table(target), R.TableCreate(target)))
					 .Run(Connection);
					tableExistMap[target] = true;
				}
			}

			return R.Table(target);
		}

		public long GetOrCreateEntityLong(Guid dbId)
		{
			lock (entityToDbGuid)
			{
				if (!entityToDbGuid.Reverse.ContainsKey(dbId))
				{
					var id = entityToDbGuid.Count() + 1;
					entityToDbGuid.Add(id, dbId);
					return id;
				}

				return entityToDbGuid.Reverse[dbId];
			}
		}

		public readonly struct IdOnlyStruct
		{
			public readonly Guid id;

			public IdOnlyStruct(Guid id)
			{
				this.id = id;
			}
		}
	}
}