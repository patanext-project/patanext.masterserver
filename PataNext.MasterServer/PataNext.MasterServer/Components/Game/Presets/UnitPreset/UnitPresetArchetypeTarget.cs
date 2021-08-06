using PataNext.MasterServer.Components.Asset;
using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Game.Unit
{
	public struct UnitPresetArchetypeTarget : IEntityComponent
	{
		public DbEntityRepresentation<AssetEntity> Asset;

		public UnitPresetArchetypeTarget(DbEntityRepresentation<AssetEntity> asset)
		{
			Asset = asset;
		}
	}
}