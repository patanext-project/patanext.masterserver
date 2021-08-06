using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using MagicOnion;
using MessagePack;
using STMasterServer.Shared.Services;

namespace PataNext.MasterServerShared.Services
{
	public interface IFormationHub : IStreamingHub<IFormationHub, IFormationReceiver>, IServiceSupplyUserToken
	{
		Task<CurrentSaveFormation> GetFormation(string saveId);

		Task UpdateSquad(string saveId, [Range(0, 2)] int squadIndex, string[] newSoldiers);
		
		Task SupplyUserToken(UserToken token);
	}

	public interface IFormationReceiver
	{
		void OnFormationUpdate(string fromSaveId);
	}

	[MessagePackObject(true)]
	public struct CurrentSaveFormation
	{
		[MessagePackObject(true)]
		public struct Squad
		{
			public string   Leader;
			public string[] Soldiers;
		}

		public Squad[] Squads;
		public string  FlagBearer;
		public string  UberHero;
	}
}