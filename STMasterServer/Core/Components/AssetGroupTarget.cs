using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace project.Core.Components
{
	public struct AssetGroupTarget : IEntityComponent
	{
		public DbEntityRepresentation<AssetEntity> Value;

		public AssetGroupTarget(DbEntityRepresentation<AssetEntity> value)
		{
			Value = value;
		}
	}
}