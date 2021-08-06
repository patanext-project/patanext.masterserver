using System;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion.Server.Hubs;
using PataNext.MasterServer.Components.Game.Unit;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using PataNext.MasterServerShared.Services;
using project;
using project.Core.Services;
using project.DataBase;
using STMasterServer.Shared.Services;

namespace PataNext.MasterServer.Services
{
	public class UnitHub : STGameServerStreamingHubBase<IUnitHub, IUnitHubReceiver>, IUnitHub
	{
		public UnitHub([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
		}

		protected override void UserOnJoinServer()
		{

		}

		protected override void ServerOnUserJoined(DbEntityKey<UserEntity> userEntity, IGroup userGroup)
		{

		}

		public Task ApplyPreset(string unitId, string presetId)
		{
			throw new NotImplementedException(nameof(ApplyPreset));
		}

		public async Task<UnitInformation> GetDetails(string unitId)
		{
			await DependencyResolver.AsTask;

			var unitEntity = db.GetEntity<UnitEntity>(unitId);
			if (unitEntity.IsNull)
				throw new RpcException(new Status(StatusCode.NotFound, $"UnitId {unitId}"));

			UnitInformation information;
			information.SaveId       = (await unitEntity.GetAsync<GameSaveRelationComponent>()).Value.Value;
			information.HardPresetId = (await unitEntity.GetAsync<UnitHardPresetTarget>()).Value.Value;
			information.SoftPresetId = (await unitEntity.GetAsync<UnitSoftPresetTarget>()).Value.Value;
			information.HierarchyId  = string.Empty;

			return information;
		}
		
		public Task SupplyUserToken(UserToken token)
		{
			return BaseSupplyUserToken(token);
		}
	}
}