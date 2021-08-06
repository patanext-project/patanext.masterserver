using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Injection.Dependency;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion.Server.Hubs;
using PataNext.MasterServer.Components.Asset;
using PataNext.MasterServer.Components.Game.Items;
using PataNext.MasterServer.Components.Game.Presets.UnitPreset;
using PataNext.MasterServer.Components.Game.Unit;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using PataNext.MasterServer.Providers;
using PataNext.MasterServer.Systems;
using PataNext.MasterServerShared;
using PataNext.MasterServerShared.Services;
using project;
using project.Core.Entities;
using project.Core.Services;
using project.DataBase;
using RethinkDb.Driver.Ast;
using STMasterServer.Shared.Services;
using Status = Grpc.Core.Status;

namespace PataNext.MasterServer.Services
{
	public class PresetHub : STGameServerStreamingHubBase<IUnitPresetHub, IUnitPresetHubReceiver>, IUnitPresetHub
	{
		private UnitPresetProvider      presetProvider;
		private UnitPresetProfileSystem presetProfileSystem;

		public PresetHub([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
			DependencyResolver.Add(() => ref presetProvider);
			DependencyResolver.Add(() => ref presetProfileSystem);
		}

		private MatchFilter<UnitPresetEntity> presetFilter;

		protected override void OnDependenciesCompleted(IEnumerable<object> obj)
		{
			base.OnDependenciesCompleted(obj);

			DependencyResolver.AddDependency(new TaskDependency(async () => { presetFilter = await db.GetMatchFilter<UnitPresetEntity>(); }));
		}

		private enum EPermissionLevel
		{
			Read,
			Write
		}

		private async ValueTask<DbEntityKey<UnitPresetEntity>> getPresetEntity(string presetId, EPermissionLevel permissionLevel)
		{
			if (IsUser == false && permissionLevel == EPermissionLevel.Write)
				throw new RpcException(new Status(StatusCode.PermissionDenied, "A server-only cannot write to a preset"));

			var presetEntity = db.GetEntity<UnitPresetEntity>(presetId);
			if (presetEntity.IsNull)
				throw new RpcException(new Status(StatusCode.NotFound, $"No preset with id {presetId} found."));

			if (permissionLevel == EPermissionLevel.Read)
				return presetEntity;

			var saveEntity = (await presetEntity.GetAsync<GameSaveRelationComponent>()).Value.ToEntity(db);
			if (saveEntity.IsNull)
				throw new RpcException(new Status(StatusCode.Internal, $"presetId {presetId} has no save attached (asked for read and write perm)"));

			var userRepresentation = (await saveEntity.GetAsync<GameSaveUserOwner>()).Entity;
			if (userRepresentation != CurrentToken.Representation)
				throw new RpcException(new Status(StatusCode.PermissionDenied, $"User {userRepresentation.Value} cannot write to preset {presetId} (asked for read and write perm)"));

			return presetEntity;
		}
		
		private async ValueTask<DbEntityKey<UnitEntity>> getUnitEntity(string unitId, EPermissionLevel permissionLevel)
		{
			if (IsUser == false && permissionLevel == EPermissionLevel.Write)
				throw new RpcException(new Status(StatusCode.PermissionDenied, "A server-only cannot write to an unit preset"));

			var unitEntity = db.GetEntity<UnitEntity>(unitId);
			if (unitEntity.IsNull)
				throw new RpcException(new Status(StatusCode.NotFound, $"No unit with id {unitId} found."));

			if (permissionLevel == EPermissionLevel.Read)
				return unitEntity;

			var saveEntity = (await unitEntity.GetAsync<GameSaveRelationComponent>()).Value.ToEntity(db);
			if (saveEntity.IsNull)
				throw new RpcException(new Status(StatusCode.Internal, $"unitId {unitId} has no save attached (asked for read and write perm)"));

			var userRepresentation = (await saveEntity.GetAsync<GameSaveUserOwner>()).Entity;
			if (userRepresentation != CurrentToken.Representation)
				throw new RpcException(new Status(StatusCode.PermissionDenied, $"User {userRepresentation.Value} cannot write to unit {unitId} (asked for read and write perm)"));

			return unitEntity;
		}

		public async Task<string[]> GetSoftPresets(string saveId)
		{
			presetFilter.Reset();
			presetFilter.Has<GameSaveRelationComponent>()
			            .None<UnitPresetHardAttach>()
			            .IsFieldEqual((GameSaveRelationComponent c) => c.Value, saveId);

			return (await presetFilter.RunAsync()).Select(key => key.Database.RepresentationOf(key))
			                                      .ToArray();
		}

		public async Task<string> CreatePreset(string saveId, string kitId)
		{
			if (!IsUser)
				throw new RpcException(new Status(StatusCode.PermissionDenied, "A server-only cannot create a preset"));

			presetFilter.Reset();
			presetFilter.Has<GameSaveRelationComponent>()
			            .None<UnitPresetHardAttach>()
			            .IsFieldEqual((GameSaveRelationComponent c) => c.Value.Value, saveId);

			const int maxPresets = 10;

			var entityList = await presetFilter.RunAsync(maxPresets);
			if (entityList.Count == maxPresets)
				throw new RpcException(new Status(StatusCode.PermissionDenied, $"preset limit reached for save {saveId}"));

			var presetEntity = await presetProvider.CreateSoftPreset(db.GetEntity<GameSaveEntity>(saveId), default, db.GetEntity<AssetEntity>(kitId), "created preset with " + kitId);
			return ((DbEntityRepresentation<UnitPresetEntity>) presetEntity).Value;
		}

		public Task ResetPreset(string presetId, string kitId)
		{
			throw new NotImplementedException();
		}

		public async Task<UnitPresetInformation> GetDetails(string presetId)
		{
			var presetEntity = await getPresetEntity(presetId, EPermissionLevel.Read);

			return new()
			{
				CustomName  = (await presetEntity.GetAsync<UnitPresetCustomName>()).Value,
				ArchetypeId = (await presetEntity.GetAsync<UnitPresetArchetypeTarget>()).Asset.Value,
				KitId       = (await presetEntity.GetAsync<UnitPresetKitTarget>()).Asset.Value,
				RoleId      = (await presetEntity.GetAsync<UnitPresetRoleTarget>()).Asset.Value
			};
		}

		public async Task SetEquipments(string presetId, Dictionary<string, string> request)
		{
			var presetEntity = await getPresetEntity(presetId, EPermissionLevel.Write);

			// First verify if the equipment are valid for the preset role
			var role              = await presetEntity.GetAsync<UnitPresetRoleTarget>();
			var allowedEquipments = (await role.Asset.ToEntity(db).GetAsync<AssetRoleData>()).AllowedEquipments;

			var equipment = new Dictionary<DbEntityKey<AssetEntity>, DbEntityKey<ItemEntity>>();
			foreach (var (attachment, item) in request)
			{
				var keyEntity = db.GetEntity<AssetEntity>(attachment);
				var valueEntity = db.GetEntity<ItemEntity>(item);
				if (keyEntity.IsNull)
					throw new RpcException(new(StatusCode.NotFound, $"No equipment_root with rep {attachment} found."));
				if (valueEntity.IsNull)
					throw new RpcException(new(StatusCode.NotFound, $"No equipment with rep {item} found."));

				equipment[keyEntity] = valueEntity;
			}

			using var postMapRemove = new Scheduler();
			foreach (var (attachment, id) in equipment)
			{
				var type = (await (await id.GetAsync<SourceAssetComponent>()).Value.ToEntity(db).GetAsync<AssetItemType>()).Asset;

				if (!allowedEquipments.TryGetValue(attachment, out var allowed)
				    || !allowed.Contains(type))
				{
					postMapRemove.Schedule(args => args.map.Remove(args.key), (map: equipment, key: attachment), default);
				}
			}

			postMapRemove.Run();

			async ValueTask updateSoftPreset()
			{
				var current = await presetEntity.GetAsync<UnitPresetEquipmentSet>();

				foreach (var (key, value) in equipment)
				{
					current.EquipmentMap[key] = value;
				}

				await presetEntity.ReplaceAsync(current);
			}

			async Task updateHardPreset()
			{
				var unitEntity = (await presetEntity.GetAsync<UnitPresetHardAttach>()).Unit.ToEntity(db);
				foreach (var (key, value) in equipment)
				{
					if (await value.HasAsync<ItemEquippedBy>() && false == await value.HasAsync<ShareableInventoryItem>())
					{
						var entities = (await value.GetAsync<ItemEquippedBy>()).ToEntities(db);
						var tasks    = new Task[entities.Length];
						for (var i = 0; i < entities.Length; i++)
						{
							var index = i;
							tasks[i] = Task.Run(async () => await presetProfileSystem.SetDefaultEquipment(entities[index], key));
						}

						await Task.WhenAll();
					}
					
					await presetProfileSystem.SetEquipmentDirect(unitEntity, presetEntity, key, value);
				}
			}

			if (await presetEntity.HasAsync<UnitPresetHardAttach>())
				await updateHardPreset();
			else
				await updateSoftPreset();

			BroadcastToGroups(g => g.OnPresetUpdate(presetId));
		}

		public async Task<Dictionary<string, string>> GetEquipments(string presetId)
		{
			var presetEntity = await getPresetEntity(presetId, EPermissionLevel.Read);
			var dictionary   = new Dictionary<string, string>();
			foreach (var (keyRepresentation, valueRepresentation) in (await presetEntity.GetAsync<UnitPresetEquipmentSet>()).EquipmentMap)
			{
				dictionary[keyRepresentation.Value] = valueRepresentation.Value;
			}

			return dictionary;
		}

		public async Task<Dictionary<string, Dictionary<string, MessageComboAbilityView>>> GetAbilities(string presetId)
		{
			var presetEntity = await getPresetEntity(presetId, EPermissionLevel.Read);
			var dictionary   = new Dictionary<string, Dictionary<string, MessageComboAbilityView>>();
			foreach (var (profileId, profile) in (await presetEntity.GetAsync<UnitPresetEquipmentSet>()).Profiles)
			{
				dictionary[profileId] = new();
				foreach (var (keyRepresentation, comboAbilityView) in profile.AbilityMap)
					dictionary[profileId][keyRepresentation.Value] = new MessageComboAbilityView
					{
						Top = comboAbilityView.Top.Value,
						Mid = comboAbilityView.Mid.Value,
						Bot = comboAbilityView.Bot.Value,
					};
			}

			return dictionary;
		}

		public async Task CopyPresetToTargetUnit(string softPresetId, string unitId)
		{
			var presetEntity = await getPresetEntity(softPresetId, EPermissionLevel.Read);
			var unitEntity   = await getUnitEntity(unitId, EPermissionLevel.Write);

			var hardTarget = await presetProvider.CopyToHardPreset(presetEntity, unitEntity);
			
			BroadcastToGroups(g => g.OnPresetUpdate(((DbEntityRepresentation<UnitPresetEntity>) hardTarget).Value));
		}

		protected override void UserOnJoinServer()
		{

		}

		protected override void ServerOnUserJoined(DbEntityKey<UserEntity> userEntity, IGroup userGroup)
		{

		}

		public Task SupplyUserToken(UserToken token)
		{
			return BaseSupplyUserToken(token);
		}
	}
}