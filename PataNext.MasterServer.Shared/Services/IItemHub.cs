using System.Threading.Tasks;
using MagicOnion;
using MessagePack;
using STMasterServer.Shared.Services;
using STMasterServer.Shared.Services.Assets;

namespace PataNext.MasterServerShared.Services
{
	public interface IItemHub : IStreamingHub<IItemHub, IItemHubReceiver>, IServiceSupplyUserToken
	{
		Task<string> GetAssetSource(string itemGuid);
		
		// ++++++++++++++++++++++++++
		// Those methods will save a few call since they are a lot used
		Task<STAssetPointer> GetAssetPointer(string itemGuid);

		Task<PNPlayerItem> GetItemDetails(string itemGuid); 
		// +++

		// Should inventory methods be moved to another service?
		Task<string[]> GetInventory(string saveId, string[] assetTypes);

		Task SupplyUserToken(UserToken token);
	}

	public interface IItemHubReceiver
	{
		// Ownership update, etc...
		void OnInventoryUpdate();
		void OnItemUpdate(string itemGuid);
	}

	[MessagePackObject(true)]
	public struct PNPlayerItem
	{
		public STAssetDetails AssetDetails;
		public string         ItemType;
		public int            StackCount; // If it's 0 then it's not stackable (but the item exist!)
		
		// add more stuff like enchantment, level, ...
	}
}