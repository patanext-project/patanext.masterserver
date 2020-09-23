using System.Collections.Generic;
using MagicOnion;
using MagicOnion.Server;
using STMasterServer.Shared.Services;

namespace project.Core.Services
{
	public class AuthenticationService : ServiceBase<IAuthenticationService>, IAuthenticationService
	{
		public UnaryResult<ConnectResult> Connect(string guid, string password, Dictionary<string, string> additionalData)
		{
			return default;
		}
	}
}