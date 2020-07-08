using System;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Core;
using Newtonsoft.Json;
using P4TLB.MasterServer;
using project;

namespace P4TLBMasterServer.Discord
{
	public class DiscordLoginRoute : ManagerBase, ILoginRouteBase
	{
		public async Task<LoginRouteResult> Start(DataUserAccount targetAccount, string jsonData, ServerCallContext context)
		{
			var userId = targetAccount.Login.Replace("DISCORD_", String.Empty);
			var lobbyMgr = World.GetOrCreateManager<DiscordLobby>();
			var searchResult = await lobbyMgr.SearchLobby(userId);
			if (!searchResult.IsSuccessStatusCode)
				throw new Exception("Lobbies couldn't be searched");

			return new LoginRouteResult {Accepted = true};
			
			var search = await get_dynamic(searchResult);
			foreach (var server in search)
			{
				Console.WriteLine($"{server.owner_id} == {userId} ({server.owner_id == userId})");
				if (server.owner_id == userId)
				{
					return new LoginRouteResult {Accepted = true};
				}
			}
			
			return new LoginRouteResult {Accepted = false};
		}

		private async Task<dynamic> get_dynamic(HttpResponseMessage msg)
		{
			dynamic t = JsonConvert.DeserializeObject(await msg.Content.ReadAsStringAsync());
			return t;
		}
	}
}