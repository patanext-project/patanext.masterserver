using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using GameHost.Injection.Dependency;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion;
using PataNext.MasterServer.Components.Account;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using PataNext.MasterServer.Systems.Core;
using PataNext.MasterServerShared.Services;
using project;
using project.Core.Services;
using project.Core.Systems;
using project.DataBase;
using STMasterServer.Shared.Services;

namespace PataNext.MasterServer.Services
{
	public class GameSaveService : STServiceBase<IGameSaveService>, IGameSaveService
	{
		private IEntityDatabase     db;
		private ConnectedUserSystem connectedUserSystem;
		private GameSaveProvider    gameSaveProvider;

		public GameSaveService([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
			DependencyResolver.Add(() => ref db);
			DependencyResolver.Add(() => ref connectedUserSystem);
			DependencyResolver.Add(() => ref gameSaveProvider);
		}

		private MatchFilter<GameSaveEntity> saveMatchFilter;

		protected override void OnDependenciesCompleted(IEnumerable<object> obj)
		{
			DependencyResolver.AddDependency(new TaskDependency(async () => { saveMatchFilter = await db.GetMatchFilter<GameSaveEntity>(); }));
		}

		public async UnaryResult<string> CreateSave(UserToken userToken, string name)
		{
			await DependencyResolver.AsTask;

			if (!connectedUserSystem.TryMatch(userToken, out var dbUserRepresentation))
				throw new Exception("invalid token");

			if ((await saveMatchFilter.IsFieldEqual((GameSaveUserOwner    c) => c.Entity, dbUserRepresentation)
			                          .IsFieldEqual((GameSaveAlmightyName c) => c.Value, name)
			                          .RunAsync(1)).Any())
				throw new Exception("save already exist");

			return db.RepresentationOf(await gameSaveProvider.CreateSave(dbUserRepresentation, name));
		}

		public async UnaryResult<string[]> ListSaves(string userGuid)
		{
			await DependencyResolver.AsTask;

			var saveList = await saveMatchFilter.IsFieldEqual((GameSaveUserOwner c) => c.Entity, userGuid)
			                                    .RunAsync();

			return saveList.Select(s => db.RepresentationOf(s))
			               .ToArray();
		}

		public async UnaryResult<SaveDetails> GetDetails(string saveId)
		{
			await DependencyResolver.AsTask;

			var saveEntity = db.GetEntity<GameSaveEntity>(saveId);
			if (saveEntity.IsNull)
				throw new RpcException(new Status(StatusCode.NotFound, $"no save with id {saveId} found"));

			var almightyName = await saveEntity.GetAsync<GameSaveAlmightyName>();
			var ownedBy      = await saveEntity.GetAsync<GameSaveUserOwner>();

			return new SaveDetails
			{
				AlmightyName = almightyName.Value,
				UserId       = ownedBy.Entity.Value
			};
		}

		public async UnaryResult<string> GetFavoriteSave(string userId)
		{
			await DependencyResolver.AsTask;

			var userEntity = db.GetEntity<UserEntity>(userId);
			if (userEntity.IsNull)
				throw new RpcException(new Status(StatusCode.NotFound, $"no user with id {userId} found."));

			if (!await userEntity.HasAsync<UserFavoriteGameSave>())
				return string.Empty;

			return (await userEntity.GetAsync<UserFavoriteGameSave>()).Entity.Value;
		}

		public async UnaryResult<bool> SetFavoriteSave(UserToken userToken, string saveId)
		{
			await DependencyResolver.AsTask;

			if (!connectedUserSystem.TryMatch(userToken, out var dbUserRepresentation))
				throw new Exception("invalid token");

			if (string.IsNullOrEmpty(saveId) || db.GetEntity<GameSaveEntity>(saveId).IsNull)
			{
				await dbUserRepresentation.ToEntity(db)
				                          .ReplaceAsync(new UserFavoriteGameSave(string.Empty));
				return false;
			}

			await dbUserRepresentation.ToEntity(db)
			                          .ReplaceAsync(new UserFavoriteGameSave(saveId));
			return true;
		}
	}
}