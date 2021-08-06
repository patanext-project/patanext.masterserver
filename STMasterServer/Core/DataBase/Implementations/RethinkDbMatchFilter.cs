using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;

namespace project.DataBase.Implementations
{
	public class RethinkDbMatchFilter<TEntity> : MatchFilter<TEntity>
		where TEntity : IEntityDescription
	{
		public readonly RethinkDbDatabaseImpl Impl;
		public readonly ReqlExpr              OriginalExpr;

		public ReqlExpr CurrentExpr;

		public RethinkDbMatchFilter(RethinkDbDatabaseImpl impl, ReqlExpr expr)
		{
			Impl        = impl;
			CurrentExpr = OriginalExpr = expr;
		}

		protected override void OnReset()
		{
			CurrentExpr = OriginalExpr;
		}

		protected override void OnHas<TComponent>()
		{
			CurrentExpr = CurrentExpr.Filter(x => x.HasFields(typeof(TComponent).Name));
		}

		protected override void OnNone<TComponent>()
		{
			CurrentExpr = CurrentExpr.Filter(x => x.HasFields(typeof(TComponent).Name).Not());
		}

		protected override void OnByFields<TComponent, TField>(Dictionary<Expression<Func<TComponent, TField>>, TField> map)
		{
			foreach (var (expression, expected) in map)
			{
				var memberInfo = ((MemberExpression) expression.Body).Member;

				CurrentExpr = memberInfo switch
				{
					FieldInfo fieldInfo => CurrentExpr.Filter(x => x[typeof(TComponent).Name][fieldInfo.Name].Eq(expected)),
					_ => throw new ArgumentOutOfRangeException(nameof(memberInfo))
				};
			}
		}
		
		protected override void OnByFieldsMultipleChoice<TComponent, TField>(Dictionary<Expression<Func<TComponent, TField>>, TField[]> map)
		{
			foreach (var (expression, expected) in map)
			{
				var memberInfo = ((MemberExpression) expression.Body).Member;

				CurrentExpr = memberInfo switch
				{
					FieldInfo fieldInfo => CurrentExpr.Filter(x =>
					{
						return RethinkDB.R.Expr(expected.Select(f => (object) f!).ToArray()).Contains(x[typeof(TComponent).Name][fieldInfo.Name]);
					}),
					_ => throw new ArgumentOutOfRangeException(nameof(memberInfo))
				};
			}
		}

		public override async ValueTask<IReadOnlyList<DbEntityKey<TEntity>>> RunAsync(int limit = 0)
		{
			var expr = CurrentExpr;
			if (limit > 0)
				expr = expr.Limit(limit);

			var list   = await expr.RunResultAsync<List<RethinkDbDatabaseImpl.IdOnlyStruct>>(Impl.Connection);
			var range  = Math.Min(limit <= 0 ? list.Count : limit, list.Count);
			var result = new DbEntityKey<TEntity>[range];

			for (var i = 0; i != range; i++) result[i] = new DbEntityKey<TEntity>(Impl, Impl.GetOrCreateEntityLong(list[i].id));

			return result;
		}
	}
}