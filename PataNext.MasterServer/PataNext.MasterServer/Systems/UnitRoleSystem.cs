using System;
using System.Linq;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using PataNext.MasterServer.Components.Asset;
using project.Core.Components;
using project.Core.Entities;
using project.DataBase;

namespace PataNext.MasterServer.Systems
{
	public class UnitRoleSystem : AppSystem
	{
		private IEntityDatabase db;

		public UnitRoleSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref db);
		}

		public async ValueTask<DbEntityKey<AssetEntity>> GetDefaultRole(DbEntityKey<AssetEntity> kitAssetEntity)
		{
			if ((await kitAssetEntity.GetAsync<AssetType>()).Type != "kit")
				throw new Exception($"not a kit");

			var kitData = await kitAssetEntity.GetAsync<AssetKitData>();
			if (kitData.Roles.FirstOrDefault() is { } baseRole && !string.IsNullOrEmpty(baseRole.Value))
			{
				return baseRole.ToEntity(db);
			}

			Console.WriteLine("no role found for " + kitAssetEntity);
			return default;
		}
	}
}