using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace P4TLBMasterServer.DiscordBot
{
	[Name("Help Module")]
	public class DiscordHelpModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; set; }

		[Command("help")]
		[Alias("?")]
		public async Task Help()
		{
			var builder = new EmbedBuilder()
			{
				Color       = Color.Gold,
				Description = "Available Commands"
			};
            
			foreach (var module in Commands.Modules)
			{
				if (module.Name == "Help Module")
					continue;
				
				string description = null;
				
				foreach (var cmd in module.Commands)
				{
					var result = await cmd.CheckPreconditionsAsync(Context);
					if (result.IsSuccess)
						description += $"&{cmd.Aliases.First()}\n";
				}
                
				if (!string.IsNullOrWhiteSpace(description))
				{
					builder.AddField(x =>
					{
						x.Name     = module.Name;
						x.Value    = description;
						x.IsInline = false;
					});
				}
			}

			await ReplyAsync("", false, builder.Build());
		}
		
		[Command("help")]
		public async Task HelpAsync(string command)
		{
			var result = Commands.Search(Context, command);

			if (!result.IsSuccess)
			{
				await ReplyAsync($"No command named **{command}** exists.");
				return;
			}
			
			var builder = new EmbedBuilder()
			{
				Color       = Color.DarkOrange,
				Description = $"Result for **{command}**"
			};

			foreach (var match in result.Commands)
			{
				var cmd = match.Command;

				builder.AddField(x =>
				{
					x.Name = string.Join(", ", cmd.Aliases);
					x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" + 
					          $"Summary: {cmd.Summary}";
					x.IsInline = false;
				});
			}

			await ReplyAsync("", false, builder.Build());
		}
	}
}