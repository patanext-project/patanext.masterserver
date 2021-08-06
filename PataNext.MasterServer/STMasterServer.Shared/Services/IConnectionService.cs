using System.Collections.Generic;
using MagicOnion;

namespace STMasterServer.Shared.Services
{
	public interface IConnectionService : IService<IConnectionService>
	{
		public UnaryResult<string> GetLogin(string representation);
		public UnaryResult<bool>   Disconnect(string token);
	}
}