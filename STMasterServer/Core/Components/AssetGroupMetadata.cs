using System;
using project.DataBase.Ecs;

namespace project.Core.Components
{
	public struct AssetGroupMetadata : IEntityComponent
	{
		public string   PublicName;
		public DateTime LastUpdate;
		public string   DownloadUrl;
	}
}