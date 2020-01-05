using System.Threading.Tasks;

namespace P4TLBMasterServer.Discord
{
	public interface ILoginRouteBase
	{
		Task<LoginRouteResult> Start(string login, string jsonData);
	}

	public struct LoginRouteResult
	{
		public bool Accepted;
	}
}