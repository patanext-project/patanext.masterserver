using PataNext.MasterServer.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Game.Presets.UnitPreset
{
	public struct UnitPresetHardAttach : IEntityComponent
	{
		public DbEntityRepresentation<UnitEntity> Unit;

		public UnitPresetHardAttach(DbEntityRepresentation<UnitEntity> unit)
		{
			Unit = unit;
		}
	}
}