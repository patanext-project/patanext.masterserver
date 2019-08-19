using System.Threading.Tasks;
using Grpc.Core;
using P4TLB.MasterServer;
using P4TLBMasterServer;

namespace project.Messages
{
	[Implementation(typeof(GameServerService))]
	public class GameServerServiceImpl : GameServerService.GameServerServiceBase
	{
		public World World;
	}
}