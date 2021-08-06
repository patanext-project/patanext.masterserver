using project.DataBase.Ecs;

namespace project.Core.Components
{
	public struct AssetType : IEntityComponent
	{
		public string Type;

		public AssetType(string type) => Type = type;
	}
}