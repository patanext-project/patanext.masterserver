using project.Core.Entities;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Asset
{
	public struct UnitKitAsset : IEntityComponent<AssetEntity>
	{
		public string Id;
		public string Name;

		public UnitKitAsset(string id, string name)
		{
			Id   = id;
			Name = name;
		}
	}
}