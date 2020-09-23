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

		public RethinkDbDatabaseImpl(IConnection connection)
		{
			Connection = connection;
		}

		public DbEntityKey<T> CreateEntity<T>() where T : IEntityDescription
		{
			// todo: cache the queryexp? (gc)

			var result = GetEntityTable<T>().Result
			                                .Insert(new object())
			                                .RunWrite(Connection);
			return new DbEntityKey<T>(this, GetOrCreateEntityLong(result.GeneratedKeys[0]));
		}

		public async ValueTask ReplaceComponentAsync<TEntity, TComponent>(DbEntityKey<TEntity> entity, TComponent value) where TEntity : IEntityDescription
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var guid = entityToDbGuid.Forward[entity.Id];
			await GetEntityTable<TEntity>().Result.Get(guid.ToString()).Update(new JObject
			{
				{typeof(TComponent).Name, JObject.FromObject(value)}
			}).RunWriteAsync(Connection);
		}
		
		public async ValueTask<bool> HasComponentAsync<TEntity, TComponent>(DbEntityKey<TEntity> entity) where TEntity : IEntityDescription
		{
			var guid = entityToDbGuid.Forward[entity.Id];
			//Console.WriteLine($"Has {typeof(TComponent)} on {typeof(TEntity)}<{guid}>(#{entity.Id})");
			return await GetEntityTable<TEntity>().Result.Get(guid.ToString()).HasFields(typeof(TComponent).Name).RunResultAsync<bool>(Connection);
		}

		public async ValueTask<TComponent> GetComponentAsync<TEntity, TComponent>(DbEntityKey<TEntity> entity) where TEntity : IEntityDescription
		{
			var guid = entityToDbGuid.Forward[entity.Id];
			//Console.WriteLine($"Get {typeof(TComponent)} on {typeof(TEntity)}<{guid}>(#{entity.Id})");
			return await GetEntityTable<TEntity>().Result.Get(guid.ToString()).GetField(typeof(TComponent).Name).RunResultAsync<TComponent>(Connection);
		}

		public async ValueTask<MatchFilter<TEntity>> GetMatchFilter<TEntity>() where TEntity : IEntityDescription
		{
			return new RethinkDbMatchFilter<TEntity>(this, await GetEntityTable<TEntity>());
		}

		/*public async ValueTask<IReadOnlyList<DbEntityKey<TEntity>>> Match<TEntity, TComponent, TField>(Expression<Func<TComponent, TField>> expr, TField expected, int length = -1)
			where TEntity : IEntityDescription
		{
			var table      = await GetEntityTable<TEntity>();
			var memberInfo = ((MemberExpression) expr.Body).Member;

			switch (memberInfo)
			{
				case FieldInfo fieldInfo:
				{
					var list                                   = await table.Filter(x => x[typeof(TComponent).Name][fieldInfo.Name].Eq(expected)).RunResultAsync<List<IdOnlyStruct>>(Connection);
					var range                                  = Math.Min(length < 0 ? list.Count : length, list.Count);
					var result                                 = new DbEntityKey<TEntity>[range];
					for (var i = 0; i != range; i++) result[i] = new DbEntityKey<TEntity>(this, GetOrCreateEntityLong(list[i].id));

					return result;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(memberInfo));
			}
		}*/

		public string RepresentationOf<T>(DbEntityKey<T> entity) where T : IEntityDescription
		{
			return entityToDbGuid.Forward[entity.Id].ToString();
		}

		public DbEntityKey<T> GetEntity<T>(ReadOnlySpan<char> representation) where T : IEntityDescription
		{
			Console.WriteLine($"Get {representation.ToString()}");
			if (Guid.TryParse(representation, out var guid))
				return new DbEntityKey<T>(this, GetOrCreateEntityLong(guid));

			// what should we return exactly?
			return default;
		}

		public async Task<Table> GetEntityTable<T>()
			where T : IEntityDescription
		{
			var target = typeof(T).Name;
			await R.TableList()
			       .Contains(target)
			       .Do_(tableExists => R.Branch(tableExists, R.Table(target), R.TableCreate(target)))
			       .RunAsync(Connection);
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