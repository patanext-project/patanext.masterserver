using System.Threading.Tasks;
using MessagePack;

namespace STMasterServer.Shared.Services
{
	/// <summary>
	/// Represent a service that should supply an user and a token associated with this user.
	/// </summary>
	public interface IServiceSupplyUserToken
	{
		// Must implement a function with this method:
		// Task SupplyUserToken(UserToken)
	}

	[MessagePackObject(true)]
	public struct UserToken
	{
		public string Representation;
		public string Token;

		public UserToken(string representation, string token)
		{
			Representation = representation;
			Token          = token;
		}
	}
}