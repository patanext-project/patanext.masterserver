using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using P4TLBMasterServer.Relay;
using project.P4Classes;

namespace P4TLBMasterServer.DiscordBot
{
	[Name("Servers")]
	public class DiscordGetServerModule : ModuleBase<SocketCommandContext>
	{
		public World World { get; set; }

		[Command("servers")]
		[Summary("Return connected servers")]
		public async Task GetAllServers()
		{
			var serverMgr = World.GetOrCreateManager<ConnectedServerManager>();
			var embedBuilder = new EmbedBuilder()
			                   .WithAuthor(Context.User)
			                   .WithTitle($"Connected Servers: " + serverMgr.ServerDictionary.Count)
			                   .WithColor(Color.DarkRed);
			var kitMgr = World.GetOrCreateManager<UnitKitComponentManager>();
			foreach (var (key, server) in serverMgr.ServerDictionary)
			{
				embedBuilder.AddField($"**{server.Name.ToUpper()}**", string.Join("\n",
					"`login ` " + server.ServerLogin,
					"`id    ` " + server.ServerId, 
					$"`slots ` {server.CurrentUserCount}/{server.MaxUsers}"));
			}

			await Context.Channel.SendMessageAsync($"✅ `&servers` result\n🤖 Requested from {Context.User.Mention}", embed: embedBuilder.Build());
		}

		[Command("connect")]
		public async Task ConnectAF()
		{
			await Context.Channel.SendMessageAsync($"Don't you dare try to do this command again {Context.User.Mention}.");
		}
	}
}