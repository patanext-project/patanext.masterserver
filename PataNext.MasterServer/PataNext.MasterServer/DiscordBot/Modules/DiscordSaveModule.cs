using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using GameHost.Core.Ecs;
using GameHost.Injection;
using PataNext.MasterServer.Components.Account;
using PataNext.MasterServer.Components.Game.Presets.UnitPreset;
using PataNext.MasterServer.Components.Game.Unit;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using PataNext.MasterServer.Providers;
using PataNext.MasterServer.Systems;
using PataNext.MasterServer.Systems.Core;
using project;
using project.DataBase;

namespace PataNext.MasterServer.DiscordBot
{
	[Name("Save Operations")]
	[Group("save")]
	public class DiscordSaveModule : ModuleBase<SocketCommandContext>
	{
		private readonly IEntityDatabase  db;
		private readonly UnitProvider     unitProvider;
		private readonly UnitRoleSystem   roleSystem;
		private readonly UnitPresetProfileSystem  unitProfileSystem;
		private readonly GameSaveProvider gameSaveProvider;

		public DiscordSaveModule(IEntityDatabase db, UnitProvider unitProvider, GameSaveProvider gameSaveProvider, UnitRoleSystem roleSystem, UnitPresetProfileSystem unitProfileSystem)
		{
			this.db               = db;
			this.unitProvider     = unitProvider;
			this.gameSaveProvider = gameSaveProvider;
			this.roleSystem       = roleSystem;
			this.unitProfileSystem  = unitProfileSystem;
		}

		private Task GetUserErrorMessage(string task) => ReplyAsync($"**SAVE '{task.ToUpper()}' FAILED!**\nError when getting the user. You may not have an account created?");

		[Command("create")]
		public async Task Create(string mightyName)
		{
			var userEntity = await GetCurrentUserEntity();
			if (userEntity.IsNull)
			{
				await GetUserErrorMessage("Creation");
				return;
			}

			if (!(await GetNamedSave(userEntity, mightyName)).IsNull)
			{
				await ReplyAsync($"A save with Almighty Name '{mightyName}' already exist!");
				return;
			}

			if (mightyName.Length > 12)
			{
				await ReplyAsync("The Almighty name can only be under or equal to 12 letters!");
				return;
			}

			var sw         = new Stopwatch();
			sw.Start();
			
			var saveEntity = await gameSaveProvider.CreateSave(userEntity, mightyName);
			
			sw.Stop();
			
			await ReplyAsync($"Save {(DbEntityRepresentation<GameSaveEntity>) saveEntity} created! ({(int) sw.Elapsed.TotalMilliseconds} ms)");
		}

		public async Task Delete(string mightyName)
		{
			var userEntity = await GetCurrentUserEntity();
			if (userEntity.IsNull)
			{
				await GetUserErrorMessage("Delete");
				return;
			}

			var foundEntity = await GetNamedSave(userEntity, mightyName);
			if (foundEntity.IsNull)
			{
				await ReplyAsync($"No save found with Almighty Name '{mightyName}'");
				return;
			}

			await ReplyAsync("This command can't delete yet save :(");
		}

		[Command("setfavorite")]
		public async Task SetPreferred(string mightyName)
		{
			var userEntity = await GetCurrentUserEntity();
			if (userEntity.IsNull)
			{
				await GetUserErrorMessage("Favorite set");
				return;
			}

			var saveEntity = await GetNamedSave(userEntity, mightyName);
			if (saveEntity.IsNull)
			{
				await ReplyAsync($"No save found with Almighty Name '{mightyName}'");
				return;
			}

			await userEntity.ReplaceAsync(new UserFavoriteGameSave {Entity = saveEntity});
			await ReplyAsync("Favorite save set to " + mightyName);
		}

		[Command("list")]
		[Summary("Display the saves of your account.")]
		public async Task List()
		{
			var userEntity = await GetCurrentUserEntity();
			if (userEntity.IsNull)
			{
				await GetUserErrorMessage("List");
				return;
			}

			var embed = new EmbedBuilder();
			embed.WithAuthor(Context.User);

			var filter = await db.GetMatchFilter<GameSaveEntity>();
			filter.IsFieldEqual((GameSaveUserOwner c) => c.Entity, userEntity);

			DbEntityRepresentation<GameSaveEntity> favoriteSave = default;
			if (await userEntity.HasAsync<UserFavoriteGameSave>())
				favoriteSave = (await userEntity.GetAsync<UserFavoriteGameSave>()).Entity;

			var saveList = await filter.RunAsync();
			if (saveList.Count == 0)
			{
				embed.Title = "No saves found!";
			}
			else
			{
				embed.Title = $"Found {saveList.Count} save(s).";
				foreach (var saveEntity in saveList)
				{
					var mightyName = await saveEntity.GetAsync<GameSaveAlmightyName>();

					var field = string.Empty;
					field += $"`{db.RepresentationOf(saveEntity)}`\n";
					if (favoriteSave == saveEntity)
						field += $"⭐ Favorite Save\n";
					embed.AddField(mightyName.Value, field, true);

					var details = string.Empty;
					details += $"Played Hours\n";
					details += $"Raid Rank\n";
					details += $"Avg Versus Rank\n";
					embed.AddField("Stats Keys", details, true);

					var result = string.Empty;
					result += $"?\n";
					result += $"0r (hasn't joined any raids)\n";
					result += $"0p (hasn't played any VS)\n";
					embed.AddField("Stats Values", result, true);
				}
			}

			await ReplyAsync(embed: embed.Build());
		}

		private async Task<DbEntityKey<GameSaveEntity>> GetNamedSave(DbEntityKey<UserEntity> user, string almightyName)
		{
			var filter = await db.GetMatchFilter<GameSaveEntity>();
			filter.IsFieldEqual((GameSaveUserOwner    c) => c.Entity, user);
			filter.IsFieldEqual((GameSaveAlmightyName c) => c.Value, almightyName);

			return (await filter.RunAsync(1)).FirstOrDefault();
		}

		private async Task<DbEntityKey<UserEntity>> GetCurrentUserEntity()
		{
			var filter = await db.GetMatchFilter<UserEntity>();
			filter.IsFieldEqual((DiscordAccount c) => c.Id, Context.User.Id);

			return (await filter.RunAsync(1)).FirstOrDefault();
		}
	}
}