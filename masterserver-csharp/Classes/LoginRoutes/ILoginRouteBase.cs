using System.Threading.Tasks;
using Grpc.Core;
using P4TLB.MasterServer;

namespace P4TLBMasterServer
{
	public interface ILoginRouteBase
	{
		Task<LoginRouteResult> Start(DataUserAccount targetAccount, string jsonData, ServerCallContext context);
	}

	public struct LoginRouteResult
	{
		public bool Accepted;
	}
}