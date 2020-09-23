using project.Core.Entities;
using project.DataBase.Ecs;

namespace project.Core.Components
{
	public struct AssetPointer : IEntityComponent<AssetEntity>
	{
		public string Author;
		public string Mod;
		public string Id;

		public AssetPointer(string author, string mod, string id)
		{
			Author = author;
			Mod    = mod;
			Id     = id;
		}

		public override string ToString()
		{
			return $"{Author}:{Mod}/{Id}";
		}
	}

	public struct AssetName : IEntityComponent<AssetEntity>
	{
		public string Value;

		public AssetName(string name) => Value = name;
	}

	public struct AssetDescription : IEntityComponent<AssetEntity>
	{
		public string Value;

		public AssetDescription(string desc) => Value = desc;
	}
}