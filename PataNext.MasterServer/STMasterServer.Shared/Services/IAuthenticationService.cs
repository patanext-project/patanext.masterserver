using System.Collections.Generic;
using MagicOnion;

namespace STMasterServer.Shared.Services
{
	public interface IAuthenticationService : IService<IAuthenticationService>
	{
		UnaryResult<ConnectResult> Connect(string guid, string password, Dictionary<string, string> additionalData);
	}

	public struct ConnectResult
	{

	}
}