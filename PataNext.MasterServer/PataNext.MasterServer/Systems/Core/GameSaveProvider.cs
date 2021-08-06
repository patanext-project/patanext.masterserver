using System;
using System.Linq;
using System.Threading.Tasks;
using Collections.Pooled;
using GameHost.Core.Ecs;
using PataNext.MasterServer.Components.Account;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using PataNext.MasterServer.Providers;
using project;
using project.Core.Entities;
using project.DataBase;

namespace PataNext.MasterServer.Systems.Core
{
	public class GameSaveProvider : AppSystem
	{
		private UnitProvider       unitProvider;
		private UnitPresetProvider presetProvider;
		private IEntityDatabase    db;

		public GameSaveProvider(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref unitProvider);
			DependencyResolver.Add(() => ref presetProvider);
			DependencyResolver.Add(() => ref db);
		}

		/// <summary>
		/// Create a standardized save
		/// </summary>
		/// <remarks>
		///	This function will not check for duplicated save.
		/// </remarks>
		/// <returns></returns>
		public async Task<DbEntityKey<GameSaveEntity>> CreateSave(DbEntityRepresentation<UserEntity> userRepresentation, string almightyName)
		{
			async Task<DbEntityKey<UnitEntity>> createUnit(DbEntityKey<GameSaveEntity> saveEntity, UnitProvider.EArchetype archetype, UnitProvider.EBaseKit kit)
			{
				var archetypeOpt = await unitProvider.GetBaseArchetype(archetype);
				if (archetypeOpt.IsNone)
					throw new Exception("no archetype");
				var kitOpt = await unitProvider.GetBaseKit(kit);
				if (kitOpt.IsNone)
					throw new Exception("no kit");

				return await unitProvider.CreateUnit(saveEntity, archetypeOpt.Value, kitOpt.Value);
			}

			async Task<DbEntityKey<UnitPresetEntity>> createPreset(DbEntityKey<GameSaveEntity> saveEntity, UnitProvider.EArchetype archetype, UnitProvider.EBaseKit kit)
			{
				var archetypeOpt = await unitProvider.GetBaseArchetype(archetype);
				if (archetypeOpt.IsNone)
					throw new Exception("no archetype");
				var kitOpt = await unitProvider.GetBaseKit(kit);
				if (kitOpt.IsNone)
					throw new Exception("no kit");

				return await presetProvider.CreateSoftPreset(saveEntity, archetypeOpt.Value, kitOpt.Value, kit + " Base");
			}
			
			await DependencyResolver.AsTask;

			var saveEntity = db.CreateEntity<GameSaveEntity>();

			using var taskList = new PooledList<ValueTask>
			{
				saveEntity.ReplaceAsync(new GameSaveUserOwner(userRepresentation)),
				saveEntity.ReplaceAsync(new GameSaveAlmightyName(almightyName)),
				saveEntity.ReplaceAsync(new GameSaveCurrentArmyFormation
				{
					FlagBearer = await createUnit(saveEntity, UnitProvider.EArchetype.Patapon, UnitProvider.EBaseKit.Hatadan),
					UberHero   = await createUnit(saveEntity, UnitProvider.EArchetype.UberHero, UnitProvider.EBaseKit.Yarida),
					Squads = await Task.Run(async () =>
					{
						var squads = new GameSaveCurrentArmyFormation.Squad[3];
						for (var i = 0; i < 3; i++)
						{
							GameSaveCurrentArmyFormation.Squad squad;
							squad.Soldiers = Enumerable.Range(0, 3).Select(_ =>
							{
								return (DbEntityRepresentation<UnitEntity>) createUnit(saveEntity,
									UnitProvider.EArchetype.Patapon,
									i switch
									{
										0 => UnitProvider.EBaseKit.Taterazay,
										1 => UnitProvider.EBaseKit.Yarida,
										2 => UnitProvider.EBaseKit.Yumiyacha,
										_ => throw new Exception(i.ToString())
									}).Result;
							}).ToArray();
							squad.Leader = await createUnit(saveEntity,
								UnitProvider.EArchetype.Patapon,
								i switch
								{
									0 => UnitProvider.EBaseKit.Taterazay,
									1 => UnitProvider.EBaseKit.Yarida,
									2 => UnitProvider.EBaseKit.Yumiyacha,
									_ => throw new Exception(i.ToString())
								});

							squads[i] = squad;
						}

						return squads;
					})
				})
			};

			// Create 3 presets for uberhero (tate, yumi, yari)
			for (var i = 0; i < 3; i++)
			{
				taskList.Add(new(createPreset(saveEntity,
					UnitProvider.EArchetype.UberHero,
					i switch
					{
						0 => UnitProvider.EBaseKit.Taterazay,
						1 => UnitProvider.EBaseKit.Yarida,
						2 => UnitProvider.EBaseKit.Yumiyacha,
						_ => throw new(i.ToString())
					})));
			}

			foreach (var task in taskList)
				await task;

			// If the user has no favorite save right now, set it to the new save
			var linkedUserEntity = userRepresentation.ToEntity(db);
			if (!await linkedUserEntity.HasAsync<UserFavoriteGameSave>())
				await linkedUserEntity.ReplaceAsync(new UserFavoriteGameSave(saveEntity));

			return saveEntity;
		}
	}
}