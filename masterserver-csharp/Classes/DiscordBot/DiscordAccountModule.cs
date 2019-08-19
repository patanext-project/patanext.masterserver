using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace P4TLBMasterServer.DiscordBot
{
	[Name("User Information")]
	public class DiscordAccountModule : ModuleBase<SocketCommandContext>
	{
		public World World { get; set; }

		[Command("all_users")]
		[Alias("users")]
		[Summary("Returns all registered users on the master server.")]
		public async Task GetAllUsers(int min = 1, int length = 0)
		{
			var userDbMgr = World.GetOrCreateManager<UserDatabaseManager>();
			var users = userDbMgr.GetAllUsers(min, length).OrderBy((u) => u.Id);

			var embedBuilder = new EmbedBuilder()
			            .WithAuthor(Context.User)
			            .WithTitle($"Showing {users.Count()} users (out of {userDbMgr.GetUserCount()})")
			            .WithColor(Color.Red)
			            .WithFooter("🔹 = Discord User");

			foreach (var user in users)
			{
				var newLogin = user.Login;
				if (newLogin.StartsWith("DISCORD_"))
				{
					var discordId = newLogin.Replace("DISCORD_", string.Empty);
					if (ulong.TryParse(discordId, out var discordIdInteger))
					{
						newLogin = "🔹 " + Context.Client.GetUser(discordIdInteger).Mention;
					}
				}

				
				var idStr = user.Id.ToString();
				if (user.Id < 10)
					idStr = "0" + idStr;
				if (user.Id < 100)
					idStr = "0" + idStr;
				embedBuilder.Description += $"`id:{idStr}`\t {newLogin}\n";
			}

			await Context.Channel.SendMessageAsync($"✅ `&all_users` result\n🤖 Requested from {Context.User.Mention}", embed: embedBuilder.Build());
		}
	}
}