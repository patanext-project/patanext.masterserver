using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PataNext.MasterServer.Components.Asset;
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

		private string[] cachedCategories;

		[Command("browse")]
		[Summary("Browse assets of a specific type")]
		public async Task Browse(string assetType = "", int page = 0)
		{
			if (string.IsNullOrEmpty(assetType))
			{
				if (cachedCategories == null)
				{
					// heavy task, so do it only once
					var allAssets = await (await db.GetMatchFilter<AssetEntity>())
					                      .Has<AssetType>()
					                      .RunAsync();

					var set = new HashSet<string>();
					foreach (var entity in allAssets)
					{
						set.Add((await entity.GetAsync<AssetType>()).Type);
					}

					cachedCategories = set.ToArray();
				}

				var str = "You did not specified a category! **Available categories**\n";
				str += string.Join(", ", cachedCategories);

				await ReplyAsync(str);
				return;
			}
			
			var entityList = await (await db.GetMatchFilter<AssetEntity>())
			                       .Has<AssetType>()
			                       .Has<AssetName>()
			                       .Has<AssetPointer>()
			                       .IsFieldEqual((AssetType c) => c.Type, assetType)
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
				               .IsFieldEqual((AssetPointer ptr) => ptr.Author, hashOrAuthor)
				               .IsFieldEqual((AssetPointer ptr) => ptr.Mod, mod)
				               .IsFieldEqual((AssetPointer ptr) => ptr.Id, nameId)
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
			if (await asset.HasAsync<AssetType>())
				embed.AddField("Category", (await asset.GetAsync<AssetType>()).Type, true);

			if (await asset.HasAsync<AssetKitData>())
			{
				var kitData = await asset.GetAsync<AssetKitData>();
				var roleStr = string.Empty;
				foreach (var roleRepresentation in kitData.Roles)
				{
					var roleName = (await roleRepresentation.ToEntity(db)
					                                        .GetAsync<AssetName>()).Value;
					roleStr += $"{roleName} ➖ `{roleRepresentation.Value}` \n";
				}

				embed.AddField("Roles", roleStr);
			}

			if (await asset.HasAsync<AssetRoleData>())
			{
				var roleData        = await asset.GetAsync<AssetRoleData>();
				var allowedEquipStr = string.Empty;
				foreach (var (keyEntity, valueEntities) in roleData.AllowedEquipments)
				{
					allowedEquipStr += $"{(await keyEntity.ToEntity(db).GetAsync<AssetName>()).Value} **[**\n```";
					foreach (var valueEntity in valueEntities)
					{
						allowedEquipStr += $"  {(await valueEntity.ToEntity(db).GetAsync<AssetName>()).Value}\n";
					}
					allowedEquipStr += "```**]**\n";
				}

				embed.AddField("Equipments", allowedEquipStr);
			}

			embed.AddField("Name Id", pointer.Id);
			// TODO: Return an icon of the asset
			embed.WithAuthor($"Asset Hash {db.RepresentationOf(asset)}", Context.User.GetAvatarUrl());

			embed.WithFooter($"Elapsed {sw.Elapsed.Seconds}s {sw.Elapsed.Milliseconds}ms");
			sw.Stop();

			await ReplyAsync(embed: embed.Build());
		}
	}
}