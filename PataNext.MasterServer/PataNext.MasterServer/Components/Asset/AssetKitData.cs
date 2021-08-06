using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Asset
{
	public struct AssetKitData : IEntityComponent
	{
		public DbEntityRepresentation<AssetEntity>[] Roles;

		public AssetKitData(DbEntityRepresentation<AssetEntity>[] roles)
		{
			Roles = roles;
		}
	}
}