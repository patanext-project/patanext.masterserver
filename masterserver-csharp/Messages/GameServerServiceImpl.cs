using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Grpc.Core;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using P4TLBMasterServer.Relay;

namespace project.Messages
{
	public class ServerUserConnectionToken
	{
		public Dictionary<ulong, string> UserToConnectionTokens = new Dictionary<ulong, string>();
		public HashSet<ulong>            ToAcknowledge          = new HashSet<ulong>();
	}

	[Implementation(typeof(GameServerService))]
	public class GameServerServiceImpl : GameServerService.GameServerServiceBase
	{
		public World                  World         { get; set; }
		public ClientManager          ClientManager { get; set; }
		public ConnectedServerManager ServerManager { get; set; }

		/// <summary>
		/// Get Connection Token (client only)
		/// </summary>
		/// <param name="request"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		/// <exception cref="RpcException"></exception>
		public override async Task<ConnectionTokenResponse> GetConnectionToken(ConnectionTokenRequest request, ServerCallContext context)
		{
			if (!ClientManager.GetClient(request.ClientToken, out var client))
				throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid Client Token"));
			var clientUserId = ClientManager.GetOrCreateData<DataUserAccount>(client).Id;

			var (success, serverData) = await ServerManager.TryGetServer(request.ServerUserId, request.ServerUserLogin);
			if (!success)
				throw new RpcException(new Status(StatusCode.NotFound, "Server not found"));

			ClientManager.TryGetClientFromUserId(serverData.ServerId, out var serverClient);
			var serverToUserToken = ClientManager.GetOrCreateData<ServerUserConnectionToken>(serverClient);
			var tokenDictionary   = serverToUserToken.UserToConnectionTokens;
			var toAck             = serverToUserToken.ToAcknowledge;

			tokenDictionary[clientUserId] = GenerateToken();
			toAck.Add(clientUserId);

			var serverClientEvent = ClientManager.GetOrCreateData<ClientEventList>(serverClient);
			serverClientEvent.Add(nameof(GlobalEvents.OnNewConnectionTokens));

			return new ConnectionTokenResponse {ConnectToken = tokenDictionary[clientUserId]};
		}

		/// <summary>
		/// Try connect to a game server (client only)
		/// </summary>
		/// <param name="request"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		/// <exception cref="RpcException"></exception>
		public override async Task<ConnectionResponse> TryConnect(ConnectionRequest request, ServerCallContext context)
		{
			if (!ClientManager.GetClient(request.ClientToken, out var client))
				return new ConnectionResponse {Error = ConnectionResponse.Types.ErrorCode.InvalidToken};
			var clientUserId = ClientManager.GetOrCreateData<DataUserAccount>(client).Id;

			var (success, serverData) = await ServerManager.TryGetServer(request.ServerUserId, request.ServerUserLogin);
			if (!success)
				throw new RpcException(new Status(StatusCode.NotFound, "Server not found"));

			ClientManager.TryGetClientFromUserId(serverData.ServerId, out var serverClient);
			var serverToUserToken = ClientManager.GetOrCreateData<ServerUserConnectionToken>(serverClient);
			// Hold on, the client never asked for a token? if that the case, return an error
			if (!serverToUserToken.UserToConnectionTokens.ContainsKey(clientUserId))
				return new ConnectionResponse {Error = ConnectionResponse.Types.ErrorCode.NoConnectionTokenAsked};
			// The server is still waiting for the token to be acknowledged.
			if (serverToUserToken.ToAcknowledge.Contains(clientUserId))
				return new ConnectionResponse {Error = ConnectionResponse.Types.ErrorCode.ServerAckPending};

			// everything was acknowledged, so we can can finally remove the token from this side...
			if (serverToUserToken.UserToConnectionTokens.ContainsKey(clientUserId))
			{
				serverToUserToken.UserToConnectionTokens.Remove(clientUserId);
			}

			// add the user to the server user list...
			var userList = ClientManager.GetOrCreateData<ServerUserList>(serverClient);
			userList.UserIds.Add(clientUserId);

			var serverLink = ClientManager.GetOrCreateData<UserServerLink>(client);
			serverLink.ServerId = serverData.ServerId;

			return new ConnectionResponse
			{
				Error           = ConnectionResponse.Types.ErrorCode.Ok,
				IsIpv6          = false,
				EndPointAddress = ClientManager.GetOrCreateData<ServerEndPoint>(serverClient).Value.Address.ToString(),
				EndPointPort    = ClientManager.GetOrCreateData<ServerEndPoint>(serverClient).Value.Port
			};
		}

		/// <summary>
		/// Get pending connection tokens... (server only)
		/// </summary>
		/// <param name="request"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public override async Task<GetPendingConnectionTokenResponse> GetPendingConnectionTokens(GetPendingConnectionTokenRequest request, ServerCallContext context)
		{
			if (!ClientManager.GetClient(request.ClientToken, out var client))
				return new GetPendingConnectionTokenResponse {Error = GetPendingConnectionTokenResponse.Types.ErrorCode.InvalidClientToken};
			var userLink = ClientManager.GetOrCreateData<DataUserAccount>(client);
			if (userLink.Type != AccountType.Server)
				throw new RpcException(new Status(StatusCode.PermissionDenied, "Not a game server."));

			var serverToUserToken = ClientManager.GetOrCreateData<ServerUserConnectionToken>(client);
			var response          = new GetPendingConnectionTokenResponse();
			response.Error = GetPendingConnectionTokenResponse.Types.ErrorCode.Ok;
			foreach (var userIdToAck in serverToUserToken.ToAcknowledge)
			{
				response.List.Add(new GetPendingConnectionTokenResponse.Types.ClientConnectionToken
				{
					Token  = serverToUserToken.UserToConnectionTokens[userIdToAck],
					UserId = userIdToAck
				});
			}

			return response;
		}

		/// <summary>
		/// Acknowledge an user connection token (server only)
		/// </summary>
		/// <param name="request"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public override async Task<AcknowledgeTokenResponse> AcknowledgeToken(AcknowledgeTokenRequest request, ServerCallContext context)
		{
			if (!ClientManager.GetClient(request.ClientToken, out var client))
				return new AcknowledgeTokenResponse {Error = AcknowledgeTokenResponse.Types.ErrorCode.InvalidClientToken};
			var userLink = ClientManager.GetOrCreateData<DataUserAccount>(client);
			if (userLink.Type != AccountType.Server)
				throw new RpcException(new Status(StatusCode.PermissionDenied, "Not a game server."));

			var serverToUserToken = ClientManager.GetOrCreateData<ServerUserConnectionToken>(client);
			var response          = new AcknowledgeTokenResponse();
			response.Error = AcknowledgeTokenResponse.Types.ErrorCode.Ok;
			if (serverToUserToken.ToAcknowledge.Contains(request.AcknowledgedUserId))
			{
				serverToUserToken.ToAcknowledge.Remove(request.AcknowledgedUserId);
			}

			return response;
		}

		public override async Task<SetServerInformationResponse> UpdateServerInformation(SetServerInformationRequest request, ServerCallContext context)
		{
			if (!ClientManager.GetClient(request.ClientToken, out var client))
				return new SetServerInformationResponse {Error = SetServerInformationResponse.Types.ErrorCode.InvalidClientToken};
			var userLink = ClientManager.GetOrCreateData<DataUserAccount>(client);
			if (userLink.Type != AccountType.Server)
				throw new RpcException(new Status(StatusCode.PermissionDenied, "Not a game server."));

			var (success, serverData) = await ServerManager.TryGetServer(userLink.Id, userLink.Login);
			if (!success)
				throw new RpcException(new Status(StatusCode.Unknown, "Unexpected error"));

			serverData.Name             = request.Name;
			serverData.CurrentUserCount = request.SlotCount;
			serverData.MaxUsers         = request.SlotLimit;
			ServerManager.Update(ref serverData);

			var userList = ClientManager.GetOrCreateData<ServerUserList>(client);
			foreach (var user in userList.UserIds)
			{
				if (ClientManager.TryGetClientFromUserId(user, out var userClient))
				{
					var clientEventList = ClientManager.GetOrCreateData<ClientEventList>(userClient);
					clientEventList.Add(nameof(GlobalEvents.OnClientServerUpdate));
				}
			}

			return new SetServerInformationResponse {Error = SetServerInformationResponse.Types.ErrorCode.Ok};
		}

		/// <summary>
		/// Get server list
		/// </summary>
		/// <param name="request"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public override async Task<ServerListResponse> GetServerList(ServerListRequest request, ServerCallContext context)
		{
			if (ServerManager.ServerDictionary.Count == 0)
				return new ServerListResponse();

			var dictionary = new Dictionary<ulong, ServerData>(ServerManager.ServerDictionary);
			var response   = new ServerListResponse();
			response.Servers.Capacity = dictionary.Count + 1;

			var element      = 0;
			var waitPerCount = 5;
			foreach (var (key, value) in dictionary)
			{
				if (!string.IsNullOrEmpty(request.QueryString) && !value.Name.Contains(request.QueryString))
					continue;

				response.Servers.Add(new ServerInformation
				{
					ServerUserId    = value.ServerId,
					ServerUserLogin = value.ServerLogin,
					Name            = value.Name,
					SlotCount       = value.CurrentUserCount,
					SlotLimit       = value.MaxUsers
				});

				if (element++ > waitPerCount)
				{
					element = 0;
					await Task.Delay(100);
				}
			}

			return response;
		}

		public override async Task<ServerInformationResponse> GetServerInformation(ServerInformationRequest request, ServerCallContext context)
		{
			var (success, serverData) = await ServerManager.TryGetServer(request.ServerUserId, request.ServerUserLogin);
			if (!success)
				throw new RpcException(new Status(StatusCode.NotFound, "Server not found"));

			return new ServerInformationResponse
			{
				Information = new ServerInformation
				{
					ServerUserId    = serverData.ServerId,
					ServerUserLogin = serverData.ServerLogin,
					Name            = serverData.Name,
					SlotCount       = serverData.CurrentUserCount,
					SlotLimit       = serverData.MaxUsers
				}
			};
		}

		private static string GenerateToken()
		{
			var length     = 24;
			var privateStr = default(string);
			using (var rng = new RNGCryptoServiceProvider())
			{
				var bytes = new byte[(length * 6 + 7) / 8];
				rng.GetBytes(bytes);
				privateStr = Convert.ToBase64String(bytes);
			}

			privateStr = privateStr.Replace(':', 'c');

			return $"{privateStr}";
		}
	}
}