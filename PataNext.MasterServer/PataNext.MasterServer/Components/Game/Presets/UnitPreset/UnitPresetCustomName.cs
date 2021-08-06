using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Game.Presets.UnitPreset
{
	public struct UnitPresetCustomName : IEntityComponent
	{
		public string Value;

		public UnitPresetCustomName(string value)
		{
			Value = value;
		}
	}
}