using System.Collections.Generic;
using PataNext.MasterServer.Utils;
using project.DataBase.Ecs;

using SongItem = project.DataBase.DbEntityRepresentation<project.Core.Entities.AssetEntity>;

namespace PataNext.MasterServer.Components.Game.Presets.UnitPreset
{
	public struct UnitPresetAbilitySet : IEntityComponent
	{
		public Dictionary<SongItem, ComboAbilityView> Abilities;

		public UnitPresetAbilitySet(Dictionary<SongItem, ComboAbilityView> abilities)
		{
			Abilities = abilities;
		}
	}
}