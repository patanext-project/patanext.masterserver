using System.Linq;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion;
using MagicOnion.Server.Hubs;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using PataNext.MasterServerShared.Services;
using project;
using project.Core.Services;
using project.DataBase;
using STMasterServer.Shared.Services;

namespace PataNext.MasterServer.Services
{
	public class FormationHub : STGameServerStreamingHubBase<IFormationHub, IFormationReceiver>, IFormationHub
	{
		public FormationHub([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
		}

		protected override void UserOnJoinServer()
		{
		}

		protected override void ServerOnUserJoined(DbEntityKey<UserEntity> userEntity, IGroup userGroup)
		{
		}

		public async Task<CurrentSaveFormation> GetFormation(string saveId)
		{
			await DependencyResolver.AsTask;

			if (string.IsNullOrEmpty(saveId) && IsUser)
			{
				// TODO: Find the user save
			}
			
			var saveEntity = db.GetEntity<GameSaveEntity>(saveId);
			if (saveEntity.IsNull)
				throw new RpcException(new Status(StatusCode.NotFound, $"No save with {saveId}"));
			
			if (!await saveEntity.HasAsync<GameSaveCurrentArmyFormation>())
				throw new RpcException(new Status(StatusCode.NotFound, $"No formation in {saveId}"));

			CurrentSaveFormation result;
			var                  current = await saveEntity.GetAsync<GameSaveCurrentArmyFormation>();

			result.UberHero   = current.UberHero.Value;
			result.FlagBearer = current.FlagBearer.Value;
			result.Squads = current.Squads.Select(squadEntity => new CurrentSaveFormation.Squad
			{
				Leader   = squadEntity.Leader.Value,
				Soldiers = squadEntity.Soldiers.Select(soldierEntity => soldierEntity.Value).ToArray()
			}).ToArray();

			return result;
		}

		public async Task UpdateSquad(string saveId, int squadIndex, string[] newSoldiers)
		{
			await DependencyResolver.AsTask;
			
			if (!IsUser)
				throw new RpcException(new Status(StatusCode.PermissionDenied, "not a user (either not connected or a server)"));

			var save = db.GetEntity<GameSaveEntity>(saveId);
			if (save.IsNull)
				throw new RpcException(new Status(StatusCode.NotFound, $"No save with id {saveId} found"));
			if ((await save.GetAsync<GameSaveUserOwner>()).Entity != CurrentUser)
				throw new RpcException(new Status(StatusCode.PermissionDenied, "not the owner of the save"));

			var current = await save.GetAsync<GameSaveCurrentArmyFormation>();
			if ((uint) squadIndex >= current.Squads.Length)
				throw new RpcException(new Status(StatusCode.OutOfRange, $"squadIndex({squadIndex}) is out of range! (max squads: {current.Squads.Length})"));

			current.Squads[squadIndex].Soldiers = newSoldiers.Select(soldier =>
			{
				var toEntity = db.GetEntity<UnitEntity>(soldier);
				if (toEntity.IsNull)
					throw new RpcException(new Status(StatusCode.NotFound, $"no unit with id {soldier} found"));

				// TODO: check if those soldiers are correct.
				return (DbEntityRepresentation<UnitEntity>) toEntity;
			}).ToArray();

			await save.ReplaceAsync(current);

			foreach (var (_, group) in ServerUserGroups) Broadcast(group).OnFormationUpdate(saveId);
		}
		
		public Task SupplyUserToken(UserToken token)
		{
			return BaseSupplyUserToken(token);
		}
	}
}