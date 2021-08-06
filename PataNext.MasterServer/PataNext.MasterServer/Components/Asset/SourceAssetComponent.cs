using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Asset
{
	public struct SourceAssetComponent : IEntityComponent
	{
		public DbEntityRepresentation<AssetEntity> Value;

		public SourceAssetComponent(DbEntityRepresentation<AssetEntity> value)
		{
			Value = value;
		}
	}
}