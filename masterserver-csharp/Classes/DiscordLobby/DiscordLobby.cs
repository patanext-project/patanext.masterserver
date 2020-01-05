using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace P4TLBMasterServer.Discord
{
	public class DiscordLobby : ManagerBase
	{
		private const string appId = "609427243395055616";
		private const string media = "application/json";
		private const string url   = "https://discordapp.com/api/v6/lobbies";

		private HttpClient client;

		public override void OnCreate()
		{
			var token = Environment.GetEnvironmentVariable("P4MS-DCB", EnvironmentVariableTarget.User);
			if (!string.IsNullOrEmpty(token))
			{
				client                                     = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", token);
			}
		}
		
		public async Task<HttpResponseMessage> SearchLobby(string discordUserId)
		{
			var json = JsonConvert.SerializeObject(new
			{
				application_id = appId,
				/*filter = new
				{
					key = discordUserId,
					value = "0",
					cast = 2,
					comparison = 1
				},
				sort = new
				{
					key = discordUserId,
					cast = 2,
					near_value = "0"
				},*/
				limit = 50
			});
			return await client.PostAsync($"{url}/search", new StringContent(json, Encoding.UTF8, media));
		}
	}
}