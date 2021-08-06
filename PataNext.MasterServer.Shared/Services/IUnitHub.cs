using System.Threading.Tasks;
using MagicOnion;
using MessagePack;
using STMasterServer.Shared.Services;

namespace PataNext.MasterServerShared.Services
{
	/// <summary>
	/// Provide services for everything related to units (soldier, leader, uberhero, ...)
	/// </summary>
	public interface IUnitHub : IStreamingHub<IUnitHub, IUnitHubReceiver>, IServiceSupplyUserToken
	{
		Task                  ApplyPreset(string unitId, string presetId);
		Task<UnitInformation> GetDetails(string  unitId);


		Task SupplyUserToken(UserToken token);
	}

	public interface IUnitHubReceiver
	{
		void OnHierarchyUpdate(string unitId);
	}

	[MessagePackObject(true)]
	public struct UnitInformation
	{
		/// <summary>
		/// Save ID of the Unit
		/// </summary>
		public string SaveId;

		/// <summary>
		/// Hard Preset ID of the Unit (contains archetype, kit, abilities, ...)
		/// </summary>
		public string HardPresetId;

		/// <summary>
		/// Soft Preset ID of the Unit (contains archetype, kit, abilities, ...)
		/// </summary>
		/// <remarks>
		///	The preset is read-only, changes are made on <see cref="HardPresetId"/>
		/// </remarks>
		public string SoftPresetId;

		/// <summary>
		/// Archetype of the Unit (patapon_std_unit, uberhero_std_unit, ...)
		/// </summary>
		public string HierarchyId;
	}
}