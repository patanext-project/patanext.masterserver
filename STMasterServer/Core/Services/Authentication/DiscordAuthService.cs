using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using GameHost.Injection;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion;
using Newtonsoft.Json;
using PataNext.MasterServer.Components.Account;
using project.Core.Systems;
using project.DataBase;
using STMasterServer.Shared.Services.Authentication;

namespace project.Core.Services.Authentication
{
	public class DiscordAuthService : STServiceBase<IDiscordAuthService>, IDiscordAuthService
	{
		private DiscordLobby        discordLobby;
		private ConnectedUserSystem connectedUserSystem;
		private IEntityDatabase     db;
		
		public DiscordAuthService([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
			DependencyResolver.Add(() => ref discordLobby);
			DependencyResolver.Add(() => ref connectedUserSystem);
			DependencyResolver.Add(() => ref db);
		}

		public async UnaryResult<DiscordAuthBegin> BeginAuth(ulong    userId)
		{
			await DependencyResolver.AsTask;
			
			var userList = await (await db.GetMatchFilter<UserEntity>())
			                     .IsFieldEqual((DiscordAccount c) => c.Id, userId)
			                     .RunAsync(1);

			if (userList.Count == 0)
				throw new RpcException(new(StatusCode.NotFound, $"no account attached to id {userId}"));
			
			var token = userId + "_t" + new Random().Next();
			discordLobby.waitingTokens[token] = new() {StepToken = token, Lobby = userId + "_l", Entity = userList[0]};

			return new()
			{
				RequiredLobbyName = "",
				StepToken         = token
			};
		}

		public async UnaryResult<ConnectResult>                 FinalizeAuth(string lobbyId, string token)
		{
			await DependencyResolver.AsTask;

			var searchResult = await discordLobby.SearchLobbies();
			if (!searchResult.IsSuccessStatusCode)
				throw new Exception("Lobbies couldn't be searched!");

			async UnaryResult<ConnectResult> connect()
			{
				foreach (var kvp in discordLobby.waitingTokens)
					Console.WriteLine($"{kvp.Key} - {kvp.Value.StepToken} <=> {token}");
				
				var user = discordLobby.waitingTokens[token].Entity;
				discordLobby.waitingTokens.Remove(token);

				var userToken = connectedUserSystem.GetOrCreateToken(user);
				return new()
				{
					Guid  = db.RepresentationOf(user),
					Token = userToken
				};
			}

			return await connect();
			
			dynamic search = JsonConvert.DeserializeObject(await searchResult.Content.ReadAsStringAsync())!;
			foreach (var server in search)
			{
				if (server.owner_id == discordLobby.waitingTokens[token].StepToken)
					return await connect();
			}

			throw new RpcException(new Status(StatusCode.PermissionDenied, ""));
		}
	}

	public class DiscordLobby : AppSystem
	{
		public struct WaitingToken
		{
			public string StepToken;
			public string Lobby;

			public DbEntityKey<UserEntity> Entity;
		}
		
		public Dictionary<string, WaitingToken> waitingTokens = new();
		
		private const string appId = "609427243395055616";
		private const string media = "application/json";
		private const string url   = "https://discordapp.com/api/v6/lobbies";

		private HttpClient client;
		
		public DiscordLobby(WorldCollection wc) : base(wc)
		{
			var token = Environment.GetEnvironmentVariable("P4MS-DCB", EnvironmentVariableTarget.User);
			if (!string.IsNullOrEmpty(token))
			{
				client                                     = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", token);
			}
		}

		public async Task<HttpResponseMessage> SearchLobbies()
		{
			var json = JsonConvert.SerializeObject(new
			{
				application_id = appId,
				limit          = 50
			});
			return await client.PostAsync($"{url}/search", new StringContent(json, Encoding.UTF8, media));
		}
	}
}