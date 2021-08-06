using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collections.Pooled;
using GameHost.Core.Ecs;
using Microsoft.Extensions.Logging;
using NetFabric.Hyperlinq;
using PataNext.MasterServer.Components.Asset;
using PataNext.MasterServer.Components.Game.Items;
using PataNext.MasterServer.Components.Game.Presets.UnitPreset;
using PataNext.MasterServer.Components.Game.Unit;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using PataNext.MasterServerShared;
using project.Core;
using project.Core.Entities;
using project.DataBase;
using ZLogger;

namespace PataNext.MasterServer.Systems
{
	[RestrictToApplication(typeof(MasterServerApplication))]
	public class UnitPresetProfileSystem : AppSystem
	{
		private IEntityDatabase db;

		private ILogger logger;

		public UnitPresetProfileSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref db);
			DependencyResolver.Add(() => ref logger);
		}

		public async Task<DbEntityKey<ItemEntity>> GetOrCreateSingletonEquipment(DbEntityKey<GameSaveEntity> saveEntity, DbEntityRepresentation<AssetEntity> itemTypeAsset)
		{
			var itemTypeEntity = itemTypeAsset.ToEntity(db);
			if (!await itemTypeEntity.HasAsync<AssetItemTypeDefaultEquipmentTarget>())
				throw new InvalidOperationException($"no default equipment for {itemTypeAsset.Value}");

			var defaultEquipment = await itemTypeEntity.GetAsync<AssetItemTypeDefaultEquipmentTarget>();

			var matchFilter = await db.GetMatchFilter<ItemEntity>();
			matchFilter.Has<DefaultEquipmentItem>()
			           .Has<GameSaveRelationComponent>()
			           .IsFieldEqual((SourceAssetComponent      c) => c.Value, defaultEquipment.Value)
			           .IsFieldEqual((GameSaveRelationComponent c) => c.Value, saveEntity);

			var results = await matchFilter.RunAsync();
			if (results.Count > 0)
			{
				if (results.Count > 1)
					logger.ZLogWarning("(EquipmentSystem:0) check later");

				return results[0];
			}

			var itemEntity = db.CreateEntity<ItemEntity>();
			await itemEntity.ReplaceAsync(new DefaultEquipmentItem());
			await itemEntity.ReplaceAsync(new SourceAssetComponent(defaultEquipment.Value));
			await itemEntity.ReplaceAsync(new GameSaveRelationComponent(saveEntity));
			await itemEntity.ReplaceAsync(new ShareableInventoryItem());

			return itemEntity;
		}
		
		// WARNING: There is no verification on whether or not the equipment is incorrect
		public async ValueTask SetEquipmentDirect(DbEntityKey<UnitEntity>       entityKey,
		                                          DbEntityKey<UnitPresetEntity> presetEntity,
		                                          DbEntityKey<AssetEntity>      equipmentRoot,
		                                          DbEntityKey<ItemEntity>       itemEntity)
		{
			var tasks = new Task[2];
			tasks[0] = Task.Run(async () =>
			{
				var set = (await presetEntity.GetAsync<UnitPresetEquipmentSet>());
				set.EquipmentMap[equipmentRoot] = itemEntity;

				await presetEntity.ReplaceAsync(set);
			});

			tasks[1] = Task.Run(async () =>
			{
				ItemEquippedBy equippedBy;
				if (await itemEntity.HasAsync<ItemEquippedBy>())
					equippedBy = await itemEntity.GetAsync<ItemEquippedBy>();
				else
					equippedBy = new() { UnitRepresentations = Array.Empty<DbEntityRepresentation<UnitEntity>>() };

				var hashset = new HashSet<DbEntityRepresentation<UnitEntity>>(equippedBy.UnitRepresentations);
				if (hashset.Add(entityKey))
					equippedBy.UnitRepresentations = hashset.ToArray();

				await itemEntity.ReplaceAsync(equippedBy);
			});

			await Task.WhenAll(tasks);
		}

		public async ValueTask SetDefaultEquipment(DbEntityKey<UnitEntity> entity, DbEntityKey<AssetEntity> equipmentRoot)
		{
			var presetKey = (await entity.GetAsync<UnitHardPresetTarget>()).Value.ToEntity(db);
			var roleKey   = (await entity.GetAsync<UnitPresetRoleTarget>()).Asset.ToEntity(db);
			var saveKey   = (await entity.GetAsync<GameSaveRelationComponent>()).Value.ToEntity(db);

			var roleData = await roleKey.GetAsync<AssetRoleData>();

			if (!roleData.AllowedEquipments.TryGetValue(equipmentRoot, out var allowed))
				throw new InvalidOperationException($"{equipmentRoot} isn't valid on role {roleKey}");

			await SetEquipmentDirect(entity, presetKey, equipmentRoot, await GetOrCreateSingletonEquipment(saveKey, allowed[0].ToEntity(db)));
		}

		public async ValueTask SetProfilesToDefault(DbEntityKey<UnitPresetEntity> presetEntity,
		                                            DbEntityKey<GameSaveEntity>   knownSave, DbEntityKey<AssetEntity> knownRole = default,
		                                            bool                          replaceExisting = true)
		{
			if (knownRole.IsNull)
			{
				knownRole = (await presetEntity.GetAsync<UnitPresetRoleTarget>()).Asset
				                                                                 .ToEntity(db);
			}

			if (knownSave.IsNull)
			{
				knownSave = (await presetEntity.GetAsync<GameSaveRelationComponent>()).Value
				                                                                      .ToEntity(db);
			}

			if (knownRole.IsNull)
				throw new Exception("no role found");

			var roleData = await knownRole.GetAsync<AssetRoleData>();

			UnitPresetEquipmentSet equipmentSet;
			if (!await presetEntity.HasAsync<UnitPresetEquipmentSet>())
			{
				equipmentSet = new()
				{
					Profiles = new()
					{
						[ProfileConsts.DefaultProfile] = new()
						{
							AbilityMap = new(),
						}
					},
					EquipmentMap = new()
				};
			}
			else
				equipmentSet = await presetEntity.GetAsync<UnitPresetEquipmentSet>();

			using var taskList = new PooledList<(DbEntityRepresentation<AssetEntity>, Task<DbEntityKey<ItemEntity>>)>();
			foreach (var (keyRepresentation, values) in roleData.AllowedEquipments)
			{
				if (replaceExisting == false && equipmentSet.EquipmentMap.ContainsKey(keyRepresentation))
					continue;

				taskList.Add((keyRepresentation, GetOrCreateSingletonEquipment(knownSave, values[0].Value)));
			}

			foreach (var (key, task) in taskList)
			{
				equipmentSet.EquipmentMap[key] = await task;
			}

			foreach (var (roleProfile, profileDefaultAbilities) in roleData.DefaultAbilities)
			{
				if (!equipmentSet.Profiles.TryGetValue(roleProfile, out var profile))
					profile = new() {AbilityMap = new()};

				foreach (var (keyRepresentation, values) in profileDefaultAbilities)
				{
					if (replaceExisting == false && profile.AbilityMap.ContainsKey(keyRepresentation))
						continue;

					profile.AbilityMap[keyRepresentation] = values;
				}
			}

			await presetEntity.ReplaceAsync(equipmentSet);
		}
	}
}