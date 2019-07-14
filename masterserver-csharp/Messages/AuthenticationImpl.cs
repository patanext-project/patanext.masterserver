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
		
		public override Task<UserLoginResponse> SendUserLogin(UserLoginRequest request, ServerCallContext context)
		{
			var result = new UserLoginResponse();
			var databaseMgr = World.GetOrCreateManager<DatabaseManager>();
			
			return Task.FromResult(result);
		}

		public override Task<UserSignUpResponse> SendUserSignUp(UserSignUpRequest request, ServerCallContext context)
		{
			return base.SendUserSignUp(request, context);
		}
	}
}