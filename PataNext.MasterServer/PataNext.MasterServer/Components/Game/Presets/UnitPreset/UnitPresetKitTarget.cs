using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Game.Presets.UnitPreset
{
	public struct UnitPresetKitTarget : IEntityComponent
	{
		/// <summary>
		/// The asset must be of 'kit' type.
		/// </summary>
		public DbEntityRepresentation<AssetEntity> Asset;

		public UnitPresetKitTarget(DbEntityRepresentation<AssetEntity> asset)
		{
			Asset = asset;
		}
	}
}