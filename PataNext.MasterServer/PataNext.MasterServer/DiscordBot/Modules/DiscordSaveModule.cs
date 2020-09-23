using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using GameHost.Core.Ecs;
using GameHost.Injection;
using PataNext.MasterServer.Components.Account;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using project;
using project.DataBase;

namespace PataNext.MasterServer.DiscordBot
{
	[Name("Save Operations")]
	[Group("save")]
	public class DiscordSaveModule : ModuleBase<SocketCommandContext>
	{
		private readonly IEntityDatabase db;

		public DiscordSaveModule(WorldCollection worldCollection)
		{
			db = new ContextBindingStrategy(worldCollection.Ctx, true).Resolve<IEntityDatabase>();
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

			var saveEntity = db.CreateEntity<GameSaveEntity>();
			await saveEntity.ReplaceAsync(new GameSaveUserOwner(userEntity));
			await saveEntity.ReplaceAsync(new GameSaveAlmightyName(mightyName));
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
			filter.ByField((GameSaveUserOwner c) => c.Entity, userEntity);

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

					var details = string.Empty;
					details += $"Id\n";
					details += $"Played Hours\n";
					details += $"Raid Rank\n";
					details += $"Avg Versus Rank\n";
					embed.AddField(mightyName.Value, details, true);

					var result = string.Empty;
					result += $"`{db.RepresentationOf(saveEntity)}`\n";
					result += $"?\n";
					result += $"0r (hasn't joined any raids)\n";
					result += $"0p (hasn't played any VS)\n";
					embed.AddField("Stats", result, true);

					embed.AddField("<:patapeek:733019671569367202>", "║");
				}
			}

			await ReplyAsync(embed: embed.Build());
		}

		private async Task<DbEntityKey<GameSaveEntity>> GetNamedSave(DbEntityKey<UserEntity> user, string almightyName)
		{
			var filter = await db.GetMatchFilter<GameSaveEntity>();
			filter.ByField((GameSaveUserOwner c) => c.Entity, user);
			filter.ByField((GameSaveAlmightyName c) => c.Value, almightyName);

			return (await filter.RunAsync(1)).FirstOrDefault();
		}

		private async Task<DbEntityKey<UserEntity>> GetCurrentUserEntity()
		{
			var filter = await db.GetMatchFilter<UserEntity>();
			filter.ByField((DiscordAccount c) => c.Id, Context.User.Id);

			return (await filter.RunAsync(1)).FirstOrDefault();
		}
	}
}