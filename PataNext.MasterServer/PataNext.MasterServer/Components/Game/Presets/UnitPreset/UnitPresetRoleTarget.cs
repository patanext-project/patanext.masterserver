using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Game.Presets.UnitPreset
{
	public struct UnitPresetRoleTarget : IEntityComponent
	{
		/// <summary>
		/// The asset must be of 'role' type.
		/// </summary>
		public DbEntityRepresentation<AssetEntity> Asset;

		public UnitPresetRoleTarget(DbEntityRepresentation<AssetEntity> asset)
		{
			Asset = asset;
		}
	}
}