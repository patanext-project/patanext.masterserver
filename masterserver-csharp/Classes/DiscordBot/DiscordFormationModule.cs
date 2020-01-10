using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using P4TLB.MasterServer;
using project.P4Classes;

namespace P4TLBMasterServer.DiscordBot
{
	[Name("Army Formation info")]
	public class DiscordFormationModule : ModuleBase<SocketCommandContext>
	{
		public World World { get; set; }

		[Command("formation")]
		[Summary("Return your formation or an user formation.")]
		public async Task GetAllUsers(IUser user = null)
		{
			var clientMgr      = World.GetOrCreateManager<ClientManager>();
			var userDbMgr      = World.GetOrCreateManager<UserDatabaseManager>();
			var formationDbMgr = World.GetOrCreateManager<FormationDatabaseManager>();

			if (user == null)
				user = Context.User;

			var login = $"DISCORD_{user.Id}";

			ulong userId;
			if ((userId = userDbMgr.GetIdFromLogin(login)) <= 0)
			{
				await Context.Channel.SendMessageAsync("Invalid User (or user did not registered).");
				return;
			}

			ulong formationId;
			if ((formationId = await formationDbMgr.FindFormationIdByUserId(userId)) <= 0)
			{
				await Context.Channel.SendMessageAsync("No army formation found for requested user. It may be possible that he didn't launched the game.");
				return;
			}

			var formation = await formationDbMgr.FindFormation(formationId);
			if (formation == null)
			{
				await Context.Channel.SendMessageAsync("Unexpected error :|");
				return;
			}

			var embedBuilder = new EmbedBuilder()
			                   .WithAuthor(user)
			                   .WithTitle($"Army Formation: " + formation.Name)
			                   .WithColor(Color.DarkRed);
			var kitMgr = World.GetOrCreateManager<UnitKitComponentManager>();

			var i = 0;
			foreach (var army in formation.Armies)
			{
				var field  = $"Army #{i++}";
				var values = new string[army.Units.Count];
				for (var u = 0; u < values.Length; u++)
				{
					values[u] = $"{(P4OfficialKit) (await kitMgr.GetCurrentKit(army.Units[u])).KitId} Unit #{u} (id={army.Units[u]})";
				}

				embedBuilder.AddField(field, string.Join("\n", values));
			}

			await Context.Channel.SendMessageAsync($"✅ `&formation` result\n🤖 Requested from {Context.User.Mention}", embed: embedBuilder.Build());
		}
	}
}