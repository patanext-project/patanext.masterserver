using System.Collections.Generic;
using System.Threading.Tasks;
using PataNext.MasterServer.Components.Asset;
using PataNext.MasterServer.Entities;
using PataNext.MasterServer.Utils;
using project.Core.Components;
using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Game.Presets.UnitPreset
{
	public struct UnitPresetEquipmentSet : IEntityComponent
	{
		public struct Profile
		{
			// Key=Song Value=AbilityView(Top,Mid,Bot)
			public Dictionary<DbEntityRepresentation<AssetEntity>, ComboAbilityView> AbilityMap;
		}

		public Dictionary<string, Profile> Profiles;

		// Key=Attachment Value=Equipment
		public Dictionary<DbEntityRepresentation<AssetEntity>, DbEntityRepresentation<ItemEntity>> EquipmentMap;

		/*public enum IsMapValidErrorMessage
		{
			None,
			MapIsNull,
			InvalidAttachmentType,
			InvalidEquipmentType
		}

		public async ValueTask<(bool, IsMapValidErrorMessage)> IsMapValid<TDatabase>(TDatabase db)
			where TDatabase : IEntityDatabase
		{
			if (Map == null)
				return (false, IsMapValidErrorMessage.MapIsNull);

			foreach (var (keyEntity, valueEntity) in Map)
			{
				if ((await keyEntity.ToEntity(db)
				                    .GetAsync<AssetType>())
					.Type != "attachment")
					return (false, IsMapValidErrorMessage.InvalidAttachmentType);
				if ((await (await valueEntity.ToEntity(db)
				                             .GetAsync<SourceAssetComponent>())
				           .Value.ToEntity(db)
				           .GetAsync<AssetType>())
					.Type != "equipment") // stinky
					return (false, IsMapValidErrorMessage.InvalidEquipmentType);
			}

			return (true, IsMapValidErrorMessage.None);
		}*/
	}
}