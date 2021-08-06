using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using GameHost.Injection.Dependency;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion.Server.Hubs;
using PataNext.MasterServer.Components.Asset;
using PataNext.MasterServer.Components.GameSave;
using PataNext.MasterServer.Entities;
using PataNext.MasterServerShared.Services;
using project;
using project.Core.Components;
using project.Core.Entities;
using project.Core.Services;
using project.DataBase;
using project.DataBase.Implementations;
using STMasterServer.Shared.Services;
using STMasterServer.Shared.Services.Assets;

namespace PataNext.MasterServer.Services
{
	public class ItemHub : STGameServerStreamingHubBase<IItemHub, IItemHubReceiver>, IItemHub
	{
		public ItemHub([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
		}

		private MatchFilter<ItemEntity> itemFilter;

		protected override void OnDependenciesCompleted(IEnumerable<object> obj)
		{
			base.OnDependenciesCompleted(obj);

			DependencyResolver.AddDependency(new TaskDependency(async () => { itemFilter = await db.GetMatchFilter<ItemEntity>(); }));
		}

		protected override void UserOnJoinServer()
		{
		}

		protected override void ServerOnUserJoined(DbEntityKey<UserEntity> userEntity, IGroup userGroup)
		{
		}

		public Task SupplyUserToken(UserToken token)
		{
			return BaseSupplyUserToken(token);
		}

		private DbEntityKey<ItemEntity> getItem(string itemGuid)
		{
			var entity = db.GetEntity<ItemEntity>(itemGuid);
			if (entity.IsNull)
				throw new RpcException(new Status(StatusCode.NotFound, $"No item found {itemGuid}"));

			return entity;
		}

		public async Task<string> GetAssetSource(string itemGuid)
		{
			return (await getItem(itemGuid).GetAsync<SourceAssetComponent>()).Value.Value;
		}

		public async Task<PNPlayerItem> GetItemDetails(string itemGuid)
		{
			var assetEntity = (await getItem(itemGuid).GetAsync<SourceAssetComponent>()).Value.ToEntity(db);
			var pointer     = await assetEntity.GetAsync<AssetPointer>();

			return new()
			{
				AssetDetails = new()
				{
					Pointer = new()
					{
						Author = pointer.Author,
						Mod    = pointer.Mod,
						Id     = pointer.Id
					},
					Name        = (await assetEntity.GetAsync<AssetName>()).Value,
					Description = (await assetEntity.GetAsync<AssetDescription>()).Value,
					Type        = (await assetEntity.GetAsync<AssetType>()).Type
				},
				ItemType = (await assetEntity.GetAsync<AssetItemType>()).Asset.Value
			};
		}

		public async Task<STAssetPointer> GetAssetPointer(string itemGuid)
		{
			var assetEntity = (await getItem(itemGuid).GetAsync<SourceAssetComponent>()).Value.ToEntity(db);
			var pointer     = await assetEntity.GetAsync<AssetPointer>();

			return new()
			{
				Author = pointer.Author,
				Mod    = pointer.Mod,
				Id     = pointer.Id
			};
		}

		public async Task<string[]> GetInventory(string saveId, string[] assetTypes)
		{
			if (assetTypes?.Length > 0)
			{
				var assetTypeFilter = await db.GetMatchFilter<AssetEntity>();
				assetTypeFilter.IsFieldMatchAny((AssetType c) => c.Type, assetTypes);

				var validAssets = (await assetTypeFilter.RunAsync())
				                  .Select(asset => new DbEntityRepresentation<AssetEntity>(asset))
				                  .ToArray();


				itemFilter.Reset();
				itemFilter.IsFieldMatchAny((SourceAssetComponent   c) => c.Value, validAssets)
				          .IsFieldEqual((GameSaveRelationComponent c) => c.Value, saveId);
				
				foreach (var asset in validAssets)
					Console.WriteLine(asset.Value);
			}
			else
			{
				itemFilter.Reset();
				itemFilter.IsFieldEqual((GameSaveRelationComponent c) => c.Value, saveId);
			}

			var list = await itemFilter.RunAsync();
			Console.WriteLine(list.Count);

			var array = new string[list.Count];
			for (var i = 0; i < list.Count; i++)
			{
				array[i] = db.RepresentationOf(list[i]);
			}

			return array;
		}
	}
}