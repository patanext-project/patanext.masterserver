using System;
using System.Net;
using System.Threading.Tasks;
using Grpc.Core;
using P4TLB.MasterServer;
using project;

namespace P4TLBMasterServer
{
	public class LocalServerRoute : ManagerBase, ILoginRouteBase
	{
		public Task<LoginRouteResult> Start(DataUserAccount targetAccount, string jsonData, ServerCallContext context)
		{
			var peerAddr = context.Peer.Substring(5);
			var portDelimiter = -1;
			for (var i = peerAddr.Length - 1; i >= 0; i--)
			{
				if (peerAddr[i] == ':')
				{
					portDelimiter = i;
					break;
				}
			}
			
			if (portDelimiter <= 0)
				throw new Exception();

			peerAddr = peerAddr.Substring(0, portDelimiter);
			if (peerAddr == "127.0.0.1" || peerAddr.StartsWith("192."))
				return Task.FromResult<LoginRouteResult>(new LoginRouteResult {Accepted = true});
			Logger.Log($"host={context.Host} peer={peerAddr}");
			return Task.FromResult(new LoginRouteResult {Accepted = false});
		}
	}
}