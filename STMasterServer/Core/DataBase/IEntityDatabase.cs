using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace project.DataBase
{
	public interface IEntityDescription
	{
	}

	public interface IDbEntityKey
	{
		public long Id { get; }
	}

	public struct DbEntityRepresentation<T> : IEquatable<DbEntityRepresentation<T>>
		where T : IEntityDescription
	{
		public string Value;

		public DbEntityRepresentation(DbEntityKey<T> origin)
		{
			Value = origin.Database.RepresentationOf(origin);
		}

		public static implicit operator DbEntityRepresentation<T>(DbEntityKey<T> origin)
		{
			return new DbEntityRepresentation<T>(origin);
		}

		public DbEntityKey<T> ToEntity(IEntityDatabase db)
		{
			return db.GetEntity<T>(Value);
		}

		public bool Equals(DbEntityRepresentation<T> other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object? obj)
		{
			return obj is DbEntityRepresentation<T> other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==(DbEntityRepresentation<T> left, DbEntityRepresentation<T> right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(DbEntityRepresentation<T> left, DbEntityRepresentation<T> right)
		{
			return !left.Equals(right);
		}
	}

	public struct DbEntityKey<T> : IDbEntityKey
		where T : IEntityDescription
	{
		private readonly IEntityDatabase db;

		public bool IsNull => Id == 0;

		public long Id { get; }

		public IEntityDatabase Database => db;

		public DbEntityKey(IEntityDatabase db, long id)
		{
			this.db = db;
			Id      = id;
		}

		public async ValueTask<bool> HasAsync<TComponent>()
		{
			return await db.HasComponentAsync<T, TComponent>(this);
		}

		public async ValueTask<TComponent> GetAsync<TComponent>()
		{
			return await db.GetComponentAsync<T, TComponent>(this);
		}

		public async ValueTask ReplaceAsync<TComponent>(TComponent value)
		{
			await db.ReplaceComponentAsync(this, value);
		}
	}

	public interface IEntityDatabase
	{
		DbEntityKey<T> CreateEntity<T>() where T : IEntityDescription;

		ValueTask<bool>       HasComponentAsync<TEntity, TComponent>(DbEntityKey<TEntity>     entity) where TEntity : IEntityDescription;
		ValueTask             ReplaceComponentAsync<TEntity, TComponent>(DbEntityKey<TEntity> entity, TComponent value) where TEntity : IEntityDescription;
		ValueTask<TComponent> GetComponentAsync<TEntity, TComponent>(DbEntityKey<TEntity>     entity) where TEntity : IEntityDescription;

		ValueTask<MatchFilter<TEntity>> GetMatchFilter<TEntity>()
			where TEntity : IEntityDescription;

		string         RepresentationOf<T>(DbEntityKey<T> entity) where T : IEntityDescription;
		DbEntityKey<T> GetEntity<T>(ReadOnlySpan<char>    representation) where T : IEntityDescription;
	}

	public abstract class MatchFilter<TEntity>
		where TEntity : IEntityDescription
	{
		public MatchFilter<TEntity> Reset()
		{
			OnReset();
			return this;
		}

		public MatchFilter<TEntity> Has<TComponent>()
		{
			OnHas<TComponent>();
			return this;
		}

		public MatchFilter<TEntity> ByFields<TComponent, TField>(Dictionary<Expression<Func<TComponent, TField>>, TField> map)
		{
			OnByFields(map);
			return this;
		}

		public MatchFilter<TEntity> ByField<TComponent, TField>(Expression<Func<TComponent, TField>> expr, TField expected)
		{
			return ByFields(new Dictionary<Expression<Func<TComponent, TField>>, TField> {{expr, expected}});
		}

		protected abstract void OnReset();
		protected abstract void OnHas<TComponent>();
		protected abstract void OnByFields<TComponent, TField>(Dictionary<Expression<Func<TComponent, TField>>, TField> map);

		public abstract ValueTask<IReadOnlyList<DbEntityKey<TEntity>>> RunAsync(int limit = 0);
	}
}