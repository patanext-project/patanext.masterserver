using System.Net;

namespace P4TLBMasterServer
{
	public class LocalEndPointManager : ManagerBase
	{
		private const string addr = "https://ipv{0}.lafibre.info/ip.php";

		private IPAddress ipv4;
		private IPAddress ipv6;

		public override void OnCreate()
		{
			ipv4 = IPAddress.Parse(new WebClient().DownloadString(string.Format(addr, 4)));
			ipv6 = IPAddress.Parse(new WebClient().DownloadString(string.Format(addr, 6)));
		}

		public IPAddress ToGlobalIpv4Address()
		{
			return ipv4;
		}
	}
}