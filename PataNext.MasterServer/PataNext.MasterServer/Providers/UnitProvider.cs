using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collections.Pooled;
using GameHost.Core;
using GameHost.Core.Ecs;
using GameHost.Injection.Dependency;
using NetFabric.Hyperlinq;
using PataNext.MasterServer.Components.Asset;
using PataNext.MasterServer.Components.Game.Presets.UnitPreset;
using PataNext.MasterServer.Components.Game.Unit;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using PataNext.MasterServer.Systems;
using project.Core;
using project.Core.Components;
using project.Core.Entities;
using project.DataBase;

namespace PataNext.MasterServer.Providers
{
	[RestrictToApplication(typeof(MasterServerApplication))]
	[UpdateAfter(typeof(CreateDefaultAssetSystem))]
	public class UnitProvider : AppSystem
	{
		public enum EBaseKit
		{
			Taterazay,
			Yarida,
			Yumiyacha,
			Shurika,
			Hatadan // hatapon lol
		}

		/// <summary>
		/// Visual archetype of an unit
		/// </summary>
		public enum EArchetype
		{
			/// <summary>
			/// Represent a normal Patapon
			/// </summary>
			Patapon = 0,

			/// <summary>
			/// Represent the UberHero (the player)
			/// </summary>
			UberHero = 2
		}

		private IEntityDatabase          db;
		private CreateDefaultAssetSystem createDefaultAssetSystem;
		private UnitPresetProfileSystem          unitProfileSystem;

		public UnitProvider(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref db);
			DependencyResolver.Add(() => ref createDefaultAssetSystem);
			DependencyResolver.Add(() => ref unitProfileSystem);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			DependencyResolver.AddDependency(new TaskDependency(createDefaultAssetSystem.CompletionTaskSource.Task));
		}

		public async Task<Option<DbEntityKey<AssetEntity>>> GetBaseKit(EBaseKit kit)
		{
			// We need to wait for core assets to be loaded/created
			await DependencyResolver.AsTask;

			var wantedId = $"kit/{kit switch {EBaseKit.Yarida => "yarida", EBaseKit.Yumiyacha => "yumiyacha", EBaseKit.Shurika => "shurika", EBaseKit.Hatadan => "hatadan", _ => "taterazay"}}";

			var filter = await db.GetMatchFilter<AssetEntity>();
			var entityList = await filter.Has<CoreAsset>()
			                             .IsFieldEqual((AssetType    c) => c.Type, "kit")
			                             .IsFieldEqual((AssetPointer c) => c.Id, wantedId)
			                             .RunAsync(1);

			return entityList.First<IReadOnlyList<DbEntityKey<AssetEntity>>, DbEntityKey<AssetEntity>>();
		}

		public async Task<Option<DbEntityKey<AssetEntity>>> GetBaseArchetype(EArchetype archetype)
		{
			// We need to wait for core assets to be loaded/created
			await DependencyResolver.AsTask;

			var wantedId = $"archetype/{archetype switch {EArchetype.UberHero => "uberhero_std_unit", _ => "patapon_std_unit"}}";

			var filter = await db.GetMatchFilter<AssetEntity>();
			var entityList = await filter.Has<CoreAsset>()
			                             .IsFieldEqual((AssetType    c) => c.Type, "archetype")
			                             .IsFieldEqual((AssetPointer c) => c.Id, wantedId)
			                             .RunAsync(1);

			return entityList.First<IReadOnlyList<DbEntityKey<AssetEntity>>, DbEntityKey<AssetEntity>>();
		}

		public async Task<DbEntityKey<UnitEntity>> CreateUnit(DbEntityKey<GameSaveEntity> save,
		                                                      DbEntityKey<AssetEntity>    archetype,
		                                                      DbEntityKey<AssetEntity>    kit)
		{
			await DependencyResolver.AsTask;

			using var taskList = new PooledList<ValueTask>();

			var preset = db.CreateEntity<UnitPresetEntity>();
			taskList.Add(preset.ReplaceAsync(new UnitPresetCustomName($"Hard Preset")));
			taskList.Add(preset.ReplaceAsync(new UnitPresetKitTarget(kit))); 
			taskList.Add(preset.ReplaceAsync(new UnitPresetArchetypeTarget(archetype)));

			if (await kit.HasAsync<AssetKitData>())
			{
				var kitData = await kit.GetAsync<AssetKitData>();
				if (kitData.Roles.Length == 0)
					Console.WriteLine($"invalid kit data for {(DbEntityRepresentation<AssetEntity>) kit}");
				
				if (kitData.Roles[0] is {} baseRole)
				{
					if (string.IsNullOrEmpty(baseRole.Value))
						Console.WriteLine($"invalid kit data for {(DbEntityRepresentation<AssetEntity>) kit}");
					
					await preset.ReplaceAsync(new UnitPresetRoleTarget(baseRole));
					await unitProfileSystem.SetProfilesToDefault(preset, knownSave: save, knownRole: baseRole.ToEntity(db), replaceExisting: true);
				}
			}
			
			taskList.Add(preset.ReplaceAsync(new GameSaveRelationComponent(save)));

			var unit = db.CreateEntity<UnitEntity>();
			taskList.Add(unit.ReplaceAsync(new UnitSoftPresetTarget(preset)));
			taskList.Add(unit.ReplaceAsync(new UnitHardPresetTarget(preset)));
			taskList.Add(unit.ReplaceAsync(new UnitStatistic()));
			taskList.Add(unit.ReplaceAsync(new UnitRankingAlpha()));
			taskList.Add(unit.ReplaceAsync(new GameSaveRelationComponent(save)));

			taskList.Add(preset.ReplaceAsync(new UnitPresetHardAttach(unit)));

			foreach (var task in taskList)
				await task;

			return unit;
		}

		public async Task AssignPreset(DbEntityKey<UnitEntity> unit, DbEntityKey<UnitPresetEntity> preset)
		{
			await DependencyResolver.AsTask;

			await unit.ReplaceAsync(new UnitSoftPresetTarget(preset));
		}
	}
}