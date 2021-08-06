using System.Collections.Generic;
using System.Threading.Tasks;
using MagicOnion;
using MessagePack;
using STMasterServer.Shared.Services;

namespace PataNext.MasterServerShared.Services
{
	public interface IUnitPresetHub : IStreamingHub<IUnitPresetHub, IUnitPresetHubReceiver>, IServiceSupplyUserToken
	{
		/// <summary>
		/// Get the soft presets of a save
		/// </summary>
		/// <param name="saveId">The save representation</param>
		/// <remarks>
		///	It may be possible that some presets will be hidden if they're not public
		/// </remarks>
		/// <returns>Soft Preset representations</returns>
		public Task<string[]> GetSoftPresets(string saveId);
		
		/// <summary>
		/// Create a new preset with a requested kit.
		/// </summary>
		/// <param name="kitId">The kit representation</param>
		/// <returns>The preset representation</returns>
		/// <remarks>
		///	This can throw an error if you have too much presets in your save.
		/// </remarks>
		public Task<string> CreatePreset(string saveId, string kitId);

		public Task ResetPreset(string presetId, string kitId);

		public Task<UnitPresetInformation> GetDetails(string presetId);

		/// <summary>
		/// Set the equipment of a preset
		/// </summary>
		/// <param name="presetId">The id</param>
		/// <param name="equipment">Equipment Map (EquipId Asset, ItemId in InventoryEntity)</param>
		/// <remarks>
		///	The equipped items will have a new owner, and so will be removed from previous one (except if it's a basic equipment (ShareableInventoryItem component))
		/// </remarks>
		/// <returns></returns>
		public Task SetEquipments(string presetId, Dictionary<string, string> equipment);

		public Task<Dictionary<string, string>> GetEquipments(string presetId);

		public Task<Dictionary<string, Dictionary<string, MessageComboAbilityView>>> GetAbilities(string presetId);

		/// <summary>
		/// Copy the content of a Soft preset to an unit hard preset
		/// </summary>
		public Task CopyPresetToTargetUnit(string softPresetId, string unitId);

		Task SupplyUserToken(UserToken token);
	}

	public interface IUnitPresetHubReceiver
	{
		void OnPresetUpdate(string presetId);
	}

	[MessagePackObject(true)]
	public struct UnitPresetInformation
	{
		public string CustomName;
		public string ArchetypeId;
		public string KitId;
		public string RoleId;
	}
}