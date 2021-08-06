using System;
using project.DataBase.Ecs;

namespace project.Core.Components
{
	public struct AssetPointer : IEntityComponent, IEquatable<AssetPointer>
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
			return $"{Author}.{Mod}/{Id}";
		}

		public bool Equals(AssetPointer other)
		{
			return Author == other.Author && Mod == other.Mod && Id == other.Id;
		}

		public override bool Equals(object? obj)
		{
			return obj is AssetPointer other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Author, Mod, Id);
		}
	}

	public struct AssetName : IEntityComponent
	{
		public string Value;

		public AssetName(string name) => Value = name;
	}

	public struct AssetDescription : IEntityComponent
	{
		public string Value;

		public AssetDescription(string desc) => Value = desc;
	}
}