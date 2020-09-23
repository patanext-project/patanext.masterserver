using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using project.Core.Components;
using project.Core.Entities;
using project.DataBase;

namespace PataNext.MasterServer.DiscordBot
{
	[Group("asset")]
	public class DiscordAssetModule : ModuleBase<SocketCommandContext>
	{
		public const int ItemPerPage = 10;

		public IEntityDatabase db { get; set; }

		[Command("browse")]
		[Summary("Browse assets of a specific type")]
		public async Task Browse(string assetType, int page = 0)
		{
			var entityList = await (await db.GetMatchFilter<AssetEntity>())
			                       .Has<AssetType>()
			                       .Has<AssetName>()
			                       .Has<AssetPointer>()
			                       .ByField((AssetType c) => c.Type, assetType)
			                       .RunAsync();
			if (entityList.Count == 0)
			{
				await ReplyAsync($"No assets found with type '{assetType}'");
				return;
			}

			var pageCount = entityList.Count / ItemPerPage;
			page = Math.Clamp(page, 0, pageCount);

			var embed = new EmbedBuilder();
			embed.WithAuthor(Context.User);
			embed.WithColor(Color.Teal);
			embed.WithTitle($"'{assetType}' Assets (Total {entityList.Count})");
			for (var i = page; i < Math.Min(entityList.Count, page + ItemPerPage); i++)
			{
				var entity = entityList[i];
				var desc   = $"`{db.RepresentationOf(entity)}`";

				embed.AddField((await entity.GetAsync<AssetName>()).Value, desc);
			}

			embed.WithFooter("To get details about an asset, type '&asset info <HASH>'\nThe HASH is the text in the code blocks.\nYou also have the possibility to do `&asset info <AUTHOR> <MOD> <ID>`");

			await ReplyAsync(embed: embed.Build());
		}

		[Command("info")]
		[Summary("Get information about a specific asset")]
		public async Task Info(string hashOrAuthor, string mod = "", string nameId = "")
		{
			var sw = new Stopwatch();
			sw.Start();
			
			DbEntityKey<AssetEntity> asset;
			if (string.IsNullOrEmpty(mod))
			{
				asset = db.GetEntity<AssetEntity>(hashOrAuthor);
			}
			else
			{
				asset = (await (await db.GetMatchFilter<AssetEntity>())
				               .ByField((AssetPointer ptr) => ptr.Author, hashOrAuthor)
				               .ByField((AssetPointer ptr) => ptr.Mod, mod)
				               .ByField((AssetPointer ptr) => ptr.Id, nameId)
				               .RunAsync(1)).FirstOrDefault();
			}

			if (asset.IsNull)
			{
				await ReplyAsync("No asset found with your arguments.");
				sw.Stop();
				return;
			}

			if ((await Task.WhenAll(
					asset.HasAsync<AssetName>().AsTask(),
					asset.HasAsync<AssetDescription>().AsTask(),
					asset.HasAsync<AssetPointer>().AsTask()))
				.Contains(false))
			{
				await ReplyAsync("The asset you asked was malformed. (whether no name and/or no description)");
				sw.Stop();
				return;
			}

			var embed = new EmbedBuilder();
			embed.WithColor(Color.DarkRed);
			embed.WithTitle((await asset.GetAsync<AssetName>()).Value);
			embed.WithDescription((await asset.GetAsync<AssetDescription>()).Value);

			var pointer = await asset.GetAsync<AssetPointer>();
			embed.AddField("Author", pointer.Author, true);
			embed.AddField("Mod Source", pointer.Mod, true);
			embed.AddField("Name Id", pointer.Id);
			// TODO: Return an icon of the asset
			embed.WithAuthor($"Asset Hash {db.RepresentationOf(asset)}", Context.User.GetAvatarUrl());

			embed.WithFooter($"Elapsed {sw.Elapsed.Seconds}s {sw.Elapsed.Milliseconds}ms");
			sw.Stop();

			await ReplyAsync(embed: embed.Build());
		}
	}
}