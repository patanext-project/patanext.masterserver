using System;
using System.Threading.Tasks;
using Grpc.Core;
using P4TLB.MasterServer;
using P4TLBMasterServer;

namespace project.Messages
{
	[Implementation(typeof(Authentication))]
	public class AuthenticationImpl : Authentication.AuthenticationBase
	{
		public World World;

		public override Task<UserLoginResponse> UserLogin(UserLoginRequest request, ServerCallContext context)
		{
			ulong id;
			
			var   userDbMgr = World.GetOrCreateManager<UserDatabaseManager>();
			if ((id = userDbMgr.GetIdFromLogin(request.Login)) == 0)
				return Task.FromResult(new UserLoginResponse {Error = UserLoginResponse.Types.ErrorCode.Invalid});

			var account = userDbMgr.FindById(id);

			// todo, need to return the token and accounts details, get the client from the connection and set the current user...
			return Task.FromResult(new UserLoginResponse {Error = UserLoginResponse.Types.ErrorCode.Success});
		}

		public override Task<UserSignUpResponse> UserSignUp(UserSignUpRequest request, ServerCallContext context)
		{
			return base.UserSignUp(request, context);
		}
	}
}