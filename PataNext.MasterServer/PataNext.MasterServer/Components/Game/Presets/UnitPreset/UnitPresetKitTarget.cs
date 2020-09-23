using PataNext.MasterServer.Entities;
using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Game.Presets.UnitPreset
{
	public struct UnitPresetKitTarget : IEntityComponent<UnitPresetEntity>
	{
		public DbEntityRepresentation<AssetEntity> Asset;

		public UnitPresetKitTarget(DbEntityRepresentation<AssetEntity> asset)
		{
			Asset = asset;
		}
	}
}