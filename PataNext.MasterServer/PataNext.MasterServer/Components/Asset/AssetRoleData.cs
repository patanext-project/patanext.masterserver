using System.Collections.Generic;
using PataNext.MasterServer.Utils;
using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

using EqRoot = project.DataBase.DbEntityRepresentation<project.Core.Entities.AssetEntity>;
using EqItem = project.DataBase.DbEntityRepresentation<project.Core.Entities.AssetEntity>;
using Profile = System.String;
using SongItem = project.DataBase.DbEntityRepresentation<project.Core.Entities.AssetEntity>;
using Ability = project.DataBase.DbEntityRepresentation<project.Core.Entities.AssetEntity>;

namespace PataNext.MasterServer.Components.Asset
{
	public struct AssetRoleData : IEntityComponent
	{
		public Dictionary<EqRoot, EqItem[]>                                AllowedEquipments;
		public Dictionary<Profile, Dictionary<SongItem, ComboAbilityView>> DefaultAbilities;

		public Profile[] Profiles;

		public AssetRoleData(Dictionary<EqRoot, EqItem[]>                                allowedEquipments,
		                     Dictionary<Profile, Dictionary<SongItem, ComboAbilityView>> defaultAbilities,
		                     Profile[] profiles)
		{
			AllowedEquipments = allowedEquipments;
			DefaultAbilities  = defaultAbilities;
			Profiles          = profiles;
		}
	}
}