using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using P4TLBMasterServer.Discord;
using P4TLBMasterServer.Events;

namespace project.Messages
{
	[Implementation(typeof(AuthenticationService))]
	public class AuthenticationServiceImpl : AuthenticationService.AuthenticationServiceBase
	{
		public World World { get; set; }

		private Dictionary<string, int> m_RequestDelays = new Dictionary<string, int>();
		
		/// An user can be a player or a server
		public override async Task<UserLoginResponse> UserLogin(UserLoginRequest request, ServerCallContext context)
		{
			ulong id;

			if (m_RequestDelays.TryGetValue(request.Login, out var blockTimeout)
			    && blockTimeout > Environment.TickCount)
			{
				Console.WriteLine("calm calm " + request.Login);
				return new UserLoginResponse {Error = UserLoginResponse.Types.ErrorCode.ConnectionAlreadyPending};
			}
			
			// 5 seconds delay
			m_RequestDelays[request.Login] = Environment.TickCount + 5_000;

			Console.WriteLine($"User login request [login: {request.Login}]");

			var userDbMgr = World.GetOrCreateManager<UserDatabaseManager>();
			if ((id = userDbMgr.GetIdFromLogin(request.Login)) == 0)
			{
				Console.WriteLine("No user found....");
				return new UserLoginResponse {Error = UserLoginResponse.Types.ErrorCode.Invalid};
			}

			ILoginRouteBase route = null;
			if (request.Login.StartsWith("DISCORD_"))
			{
				route = World.GetOrCreateManager<DiscordLoginRoute>();
			}
			
			if (route == null)
				throw new Exception("Route is null");

			Console.WriteLine(request.RouteData);
			
			var routeResult = await route.Start(request.Login, request.RouteData);
			if (!routeResult.Accepted)
				return new UserLoginResponse {Error = UserLoginResponse.Types.ErrorCode.Invalid};
			
			var clientMgr = World.GetOrCreateManager<ClientManager>();
			// Find if the peer is already connected or not
			if (clientMgr.GetClientIdByUserId(id) > 0) // already connected...
			{
				return new UserLoginResponse {Error = UserLoginResponse.Types.ErrorCode.AlreadyConnected};
			}
			
			var account   = userDbMgr.FindById(id);
			
			// Connect the user...
			var client = clientMgr.ConnectClient(account.Login);
			// link user to client
			clientMgr.ReplaceData(client, account);
			clientMgr.LinkUserClient(account, client);
			
			Console.WriteLine($"User connected [login: {request.Login}]");
			
			clientMgr.GetOrCreateData<ClientEventList>(client).Add("test login");
			
			World.Notify(this, "OnUserConnection", new OnUserConnection {User = account, Client = client});

			// todo, need to return the token and accounts details, get the client from the connection and set the current user...
			return new UserLoginResponse
			{
				Error    = UserLoginResponse.Types.ErrorCode.Success,
				ClientId = client.Id,
				UserId   = account.Id,
				Token    = client.Token
			};
		}

		// NOT DONE YET.
		public override Task<UserSignUpResponse> UserSignUp(UserSignUpRequest request, ServerCallContext context)
		{
			return base.UserSignUp(request, context);
		}

		public override Task<DisconnectReply> Disconnect(DisconnectRequest request, ServerCallContext context)
		{
			Console.WriteLine("Received a disconnect request!");
			
			var clientMgr = World.GetOrCreateManager<ClientManager>();
			if (!clientMgr.GetClient(request.Token, out var client))
				throw new Exception("Client not found for token " + request.Token);
			
			var user = clientMgr.GetOrCreateData<DataUserAccount>(client);
			if (user != null)
			{
				clientMgr.UnlinkUserClient(user, client);
			}
			
			clientMgr.DisconnectClientByToken(request.Token);
			
			return Task.FromResult<DisconnectReply>(new DisconnectReply() {});
		}
	}
}