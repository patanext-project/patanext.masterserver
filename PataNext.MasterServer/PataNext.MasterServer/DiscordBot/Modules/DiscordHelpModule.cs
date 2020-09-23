using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace PataNext.MasterServer.DiscordBot
{
	public class DiscordHelpModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; set; }

		[Command("help")]
		[Alias("?")]
		[Summary("Get the help page")]
		public async Task Help()
		{
			var builder = new EmbedBuilder()
			{
				Color       = Color.Gold,
				Description = "Available Commands"
			};

			foreach (var module in Commands.Modules)
			{
				if (module.Name == nameof(DiscordHelpModule))
					continue;

				string description = null;

				foreach (var cmd in module.Commands)
				{
					var result = await cmd.CheckPreconditionsAsync(Context);
					if (result.IsSuccess)
						description += $"{cmd.Aliases.First()}\n";
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
		[Summary("Get help about a specific command")]
		public async Task HelpAsync([Remainder] string command)
		{
			var result = Commands.Search(Context, command);

			if (!result.IsSuccess)
			{
				Console.WriteLine("no success");
				await ReplyAsync($"No command named **{command}** exists.");
				return;
			}

			var fields = new List<EmbedFieldBuilder>();
			foreach (var match in result.Commands)
			{
				fields.AddRange(new[]
				{
					new EmbedFieldBuilder
					{
						Name     = "Parameters",
						Value    = new Func<string>(() =>
						{
							if (match.Command.Parameters.Count == 0)
								return "<:10_silence:672102738644959262>";
							return string.Join(", ", match.Command.Parameters);
						})(),
						IsInline = true
					},
					new EmbedFieldBuilder
					{
						Name     = "Summary",
						Value    = string.IsNullOrEmpty(match.Command.Summary) ? "<:10_silence:672102738644959262>" : match.Command.Summary,
						IsInline = true
					},
					new EmbedFieldBuilder
					{
						Name  = "Remarks",
						Value = string.IsNullOrEmpty(match.Command.Remarks) ? "<:10_silence:672102738644959262>" : match.Command.Remarks
					}
				});
			}

			var builder = new EmbedBuilder()
			{
				Author = new EmbedAuthorBuilder
				{
					Name = "Help For"
				},
				Color  = Color.DarkOrange,
				Title  = command,
				Fields = fields
			};

			await ReplyAsync("", false, builder.Build());
		}
	}
}