using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using P4TLB.MasterServer;
using P4TLBMasterServer;

namespace project.Messages
{
	[Implementation(typeof(AuthenticationService))]
	public class AuthenticationServiceImpl : AuthenticationService.AuthenticationServiceBase
	{
		public World World;

		/// An user can be a player or a server
		public override Task<UserLoginResponse> UserLogin(UserLoginRequest request, ServerCallContext context)
		{
			ulong id;

			Console.WriteLine($"User login request [login: {request.Login}]");

			var userDbMgr = World.GetOrCreateManager<UserDatabaseManager>();
			if ((id = userDbMgr.GetIdFromLogin(request.Login)) == 0)
			{
				Console.WriteLine("No user found....");
				return Task.FromResult(new UserLoginResponse {Error = UserLoginResponse.Types.ErrorCode.Invalid});
			}

			var clientMgr = World.GetOrCreateManager<ClientManager>();
			// Find if the peer is already connected or not
			if (clientMgr.GetClientIdByUserId(id) > 0) // already connected...
			{
				return Task.FromResult(new UserLoginResponse {Error = UserLoginResponse.Types.ErrorCode.AlreadyConnected});
			}
			
			var account   = userDbMgr.FindById(id);
			
			// Connect the user...
			var client = clientMgr.ConnectClient(account.Login);
			// link user to client
			clientMgr.ReplaceData(client, account);

			// todo, need to return the token and accounts details, get the client from the connection and set the current user...
			return Task.FromResult(new UserLoginResponse
			{
				Error    = UserLoginResponse.Types.ErrorCode.Success,
				ClientId = client.Id,
				UserId   = account.Id,
				Token    = client.Token
			});
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