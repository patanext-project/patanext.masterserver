using System;
using System.Linq;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion;
using project.Core.Components;
using project.Core.Entities;
using project.DataBase;
using STMasterServer.Shared.Services.Assets;

namespace project.Core.Services.Assets
{
	public class ViewableAssetService : STServiceBase<IViewableAssetService>, IViewableAssetService
	{
		private IEntityDatabase db;

		public ViewableAssetService([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
			DependencyResolver.Add(() => ref db);
		}

		public async UnaryResult<STAssetPointer> GetPointer(string assetGuid)
		{
			await DependencyResolver.AsTask;

			var entity = db.GetEntity<AssetEntity>(assetGuid);
			if (entity.IsNull)
				throw new RpcException(new Status(StatusCode.NotFound, $"No asset with guid " + assetGuid));

			var component = await entity.GetAsync<AssetPointer>();
			return new()
			{
				Author = component.Author,
				Mod    = component.Mod,
				Id     = component.Id
			};
		}

		public async UnaryResult<STAssetDetails> GetDetails(string assetGuid)
		{
			await DependencyResolver.AsTask;
			
			var entity  = db.GetEntity<AssetEntity>(assetGuid);
			var pointer = await GetPointer(assetGuid);

			return new()
			{
				Pointer     = pointer,
				Name        = (await entity.GetAsync<AssetName>()).Value,
				Description = (await entity.GetAsync<AssetDescription>()).Value,
				Type        = (await entity.GetAsync<AssetType>()).Type
			};
		}

		public async UnaryResult<string> GetGuid(string author, string mod, string id)
		{
			await DependencyResolver.AsTask;

			var filter = await db.GetMatchFilter<AssetEntity>();
			filter.IsFieldEqual((AssetPointer c) => c.Author, author);
			filter.IsFieldEqual((AssetPointer c) => c.Mod, mod);
			filter.IsFieldEqual((AssetPointer c) => c.Id, id);

			var list = await filter.RunAsync(1);
			if (list.Count > 0)
			{
				return ((DbEntityRepresentation<AssetEntity>)list[0]).Value;
			}

			return string.Empty;
		}

		public async UnaryResult<STAssetGroupMetadata> GetAssetGroupMetadata(string assetGroupId)
		{
			await DependencyResolver.AsTask;

			var re = await (await db.GetMatchFilter<AssetEntity>())
			               .IsFieldEqual((AssetGroupTarget c) => c.Value, assetGroupId)
			               .RunAsync(1);

			if (re.Count == 0)
				throw new RpcException(new Status(StatusCode.NotFound, "no group found with id " + assetGroupId));

			var groupAssetEntity = re[0];
			var component        = (await groupAssetEntity.GetAsync<AssetGroupMetadata>());
			return new()
			{
				Name       = component.PublicName,
				LastUpdate = component.LastUpdate
			};
		}
	}
}