using System.Threading.Tasks;
using project.Core.Components;
using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Asset
{
	public struct AssetItemType : IEntityComponent
	{
		public DbEntityRepresentation<AssetEntity> Asset;

		public AssetItemType(DbEntityRepresentation<AssetEntity> eqTypeAsset)
		{
			Asset = eqTypeAsset;
		}

		public async ValueTask<bool> IsValid<TDatabase>(TDatabase db)
			where TDatabase : IEntityDatabase
		{
			return (await Asset.ToEntity(db)
			                   .GetAsync<AssetType>()).Type == "item_type";
		}
	}
}