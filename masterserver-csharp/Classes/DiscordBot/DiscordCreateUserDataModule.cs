using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using P4TLB.MasterServer;

namespace P4TLBMasterServer.DiscordBot
{
	[Name("Account Information")]
	public class DiscordCreateUserDataModule : ModuleBase<SocketCommandContext>
	{
		public World World { get; set; }

		[Command("account")]
		[Summary("")]
		public async Task CreateUserFromDiscord()
		{
			ulong id = 0;
			DataUserAccount account;
			
			var userDbMgr = World.GetOrCreateManager<UserDatabaseManager>();
			if ((id = userDbMgr.GetIdFromLogin(GetLogin(Context.User))) == 0)
			{
				account = userDbMgr.CreateAccount(GetLogin(Context.User), out var success);
				if (success)
				{
					await ReplyAsync("Creating a new account just for you! Details of the account sent in your PMs.");
					await MessageToUserAccountDetails(Context.User, account);
				}
				else
				{
					await ReplyAsync("Couldn't create an account...");
				}

				return;
			}
			
			account = userDbMgr.FindById(id);

			await ReplyAsync("Account details sent in your PMs.");
			await MessageToUserAccountDetails(Context.User, account);
		}

		[Command("user")]
		[Summary("Retrieve the public information of an user.")]
		public async Task GetUserAccount(string login)
		{
			ulong           id = 0;

			var userDbMgr = World.GetOrCreateManager<UserDatabaseManager>();
			if ((id = userDbMgr.GetIdFromLogin(login)) > 0)
			{
				await MessageOnChannelAccountDetails(Context.User, userDbMgr.FindById(id));
			}
			else
			{
				await ReplyAsync("No account found with login: " + login);
			}
		}
		
		[Command("user")]
		[Summary("Retrieve the public information of an user.")]
		public async Task GetUserAccount(ulong id)
		{
			var userDbMgr = World.GetOrCreateManager<UserDatabaseManager>();
			var account = userDbMgr.FindById(id);
			if (account != null)
			{
				await MessageOnChannelAccountDetails(Context.User, account);
			}
			else
			{
				await ReplyAsync("No account found with id: " + id);
			}
		}
		
		private async Task MessageOnChannelAccountDetails(IUser request, DataUserAccount account)
		{
			var embedBuilder = new EmbedBuilder()
			                   .WithAuthor(request)
			                   .WithTitle("Account details")
			                   .WithColor(Color.DarkRed);

			embedBuilder.Description += $"**ID** {account.Id}\n";
			embedBuilder.Description += $"**Login** {account.Login}\n";
			embedBuilder.Description += $"**Type** {account.Type}\n";

			await ReplyAsync(embed: embedBuilder.Build());
		}

		private async Task MessageToUserAccountDetails(IUser user, DataUserAccount account)
		{
			var embedBuilder = new EmbedBuilder()
			                   .WithAuthor(user)
			                   .WithTitle("Account details")
			                   .WithColor(Color.DarkRed);

			embedBuilder.Description += $"**ID** {account.Id}\n";
			embedBuilder.Description += $"**Login** {account.Login}\n";
			embedBuilder.Description += $"**Type** {account.Type}\n";

			await user.SendMessageAsync(embed: embedBuilder.Build());
		}

		private string GetLogin(IUser user)
		{
			return "DISCORD_" + user.Id;
		}
 	}
}