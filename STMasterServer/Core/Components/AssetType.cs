using project.Core.Entities;
using project.DataBase.Ecs;

namespace project.Core.Components
{
	public struct AssetType : IEntityComponent<AssetEntity>
	{
		public string Type;

		public AssetType(string type) => Type = type;
	}
}