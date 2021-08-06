using System;
using System.Threading.Tasks;
using Collections.Pooled;
using GameHost.Core.Ecs;
using PataNext.MasterServer.Components.Asset;
using PataNext.MasterServer.Components.Game.Presets.UnitPreset;
using PataNext.MasterServer.Components.Game.Unit;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using PataNext.MasterServer.Systems;
using project.Core.Components;
using project.Core.Entities;
using project.DataBase;

namespace PataNext.MasterServer.Providers
{
	public class UnitPresetProvider : AppSystem
	{
		private UnitPresetProfileSystem presetProfileSystem;
		private IEntityDatabase         db;

		public UnitPresetProvider(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref presetProfileSystem);
			DependencyResolver.Add(() => ref db);
		}

		public async Task<DbEntityKey<UnitPresetEntity>> CreateSoftPreset(DbEntityKey<GameSaveEntity> save,
		                                                                  DbEntityKey<AssetEntity>    archetype,
		                                                                  DbEntityKey<AssetEntity>    kit,
		                                                                  string                      name)
		{
			await DependencyResolver.AsTask;

			using var taskList = new PooledList<ValueTask>();

			var preset = db.CreateEntity<UnitPresetEntity>();
			taskList.Add(preset.ReplaceAsync(new UnitPresetCustomName(name ?? "Soft Preset")));
			taskList.Add(preset.ReplaceAsync(new UnitPresetKitTarget(kit)));
			taskList.Add(preset.ReplaceAsync(new UnitPresetArchetypeTarget(archetype)));

			if (await kit.HasAsync<AssetKitData>())
			{
				var kitData = await kit.GetAsync<AssetKitData>();
				if (kitData.Roles.Length == 0)
					Console.WriteLine($"invalid kit data for {(DbEntityRepresentation<AssetEntity>) kit}");

				if (kitData.Roles[0] is { } baseRole)
				{
					if (string.IsNullOrEmpty(baseRole.Value))
						Console.WriteLine($"invalid kit data for {(DbEntityRepresentation<AssetEntity>) kit}");

					await preset.ReplaceAsync(new UnitPresetRoleTarget(baseRole));
					await presetProfileSystem.SetProfilesToDefault(preset, knownSave: save, knownRole: baseRole.ToEntity(db), replaceExisting: true);
				}
			}

			taskList.Add(preset.ReplaceAsync(new GameSaveRelationComponent(save)));

			foreach (var task in taskList)
				await task;

			return preset;
		}

		public async ValueTask<DbEntityKey<UnitPresetEntity>> CopyToHardPreset(DbEntityKey<UnitPresetEntity> from, DbEntityKey<UnitEntity> to)
		{
			var hardPreset = (await to.GetAsync<UnitHardPresetTarget>())
			                 .Value
			                 .ToEntity(db);

			using var taskList = new PooledList<ValueTask>()
			{
				hardPreset.ReplaceAsync(await from.GetAsync<UnitPresetKitTarget>()),
				hardPreset.ReplaceAsync(await from.GetAsync<UnitPresetRoleTarget>()),
				hardPreset.ReplaceAsync(await from.GetAsync<UnitPresetEquipmentSet>()),
			};

			Console.WriteLine($"{(DbEntityRepresentation<UnitPresetEntity>) from} -> {(DbEntityRepresentation<UnitEntity>) to} ({(DbEntityRepresentation<UnitPresetEntity>) hardPreset})");
			
			foreach (var task in taskList)
				await task;

			Console.WriteLine($"Completed! (Current Kit = {hardPreset.GetAsync<UnitPresetKitTarget>().Result.Asset.ToEntity(db).GetAsync<AssetName>().Result.Value}, Archetype = {hardPreset.GetAsync<UnitPresetArchetypeTarget>().Result.Asset.ToEntity(db).GetAsync<AssetName>().Result.Value})");

			return hardPreset;
		}
	}
}