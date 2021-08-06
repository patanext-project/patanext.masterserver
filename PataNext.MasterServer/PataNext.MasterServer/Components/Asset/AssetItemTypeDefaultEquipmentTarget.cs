using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Asset
{
	public struct AssetItemTypeDefaultEquipmentTarget : IEntityComponent
	{
		public DbEntityRepresentation<AssetEntity> Value;

		public AssetItemTypeDefaultEquipmentTarget(DbEntityRepresentation<AssetEntity> value)
		{
			Value = value;
		}
	}
}