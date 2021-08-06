using System;
using System.Threading.Tasks;
using MagicOnion;
using MessagePack;

namespace STMasterServer.Shared.Services.Assets
{
	public interface IViewableAssetService : IService<IViewableAssetService>
	{
		UnaryResult<STAssetPointer> GetPointer(string assetGuid);
		UnaryResult<STAssetDetails> GetDetails(string assetGuid);
		UnaryResult<string> GetGuid(string    author, string mod, string id);

		UnaryResult<STAssetGroupMetadata> GetAssetGroupMetadata(string assetGroupId);
	}

	[MessagePackObject(true)]
	public struct STAssetPointer
	{
		//[Key(0)]
		public string Author;

		//[Key(1)]
		public string Mod;

		//[Key(2)]
		public string Id;
	}

	[MessagePackObject(true)]
	public struct STAssetDetails
	{
		public STAssetPointer Pointer;
		public string         Type;
		public string         Name;
		public string         Description;
	}

	[MessagePackObject(true)]
	public struct STAssetGroupMetadata
	{
		public string   Name;
		public DateTime LastUpdate;
	}
}