using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace P4TLBMasterServer.Discord
{
	public class DiscordLoginRoute : ManagerBase, ILoginRouteBase
	{
		public async Task<LoginRouteResult> Start(string login, string jsonData)
		{
			var userId = login.Replace("DISCORD_", String.Empty);
			var lobbyMgr = World.GetOrCreateManager<DiscordLobby>();
			var searchResult = await lobbyMgr.SearchLobby(userId);
			if (!searchResult.IsSuccessStatusCode)
				throw new Exception("Lobbies couldn't be searched");

			var search = await get_dynamic(searchResult);
			foreach (var server in search)
			{
				if (server.owner_id == userId)
				{
					return new LoginRouteResult {Accepted = true};
					break;
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