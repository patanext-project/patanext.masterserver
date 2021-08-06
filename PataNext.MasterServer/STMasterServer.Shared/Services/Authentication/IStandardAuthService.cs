using System.Security.Cryptography;
using System.Text;
using MagicOnion;
using MessagePack;

namespace STMasterServer.Shared.Services.Authentication
{
	/// <summary>
	/// Standard auth service (guid/login + password)
	/// </summary>
	public interface IStandardAuthService : IService<IStandardAuthService>
	{
		UnaryResult<ConnectResult> ConnectViaGuid(string  guid,  string password);
		UnaryResult<ConnectResult> ConnectViaLogin(string login, string password);
	}

	[MessagePackObject(true)]
	public struct ConnectResult
	{
		/// <summary>
		/// The token generated for this session. This should be a secret between the server and the user.
		/// </summary>
		public string Token;

		/// <summary>
		/// The user guid
		/// </summary>
		public string Guid;
	}

	public static class StandardAuthUtility
	{
		public static string CreateMD5(string input)
		{
			// Use input string to calculate MD5 hash
			using var md5        = MD5.Create();
			var       inputBytes = Encoding.ASCII.GetBytes(input);
			var       hashBytes  = md5.ComputeHash(inputBytes);

			// Convert the byte array to hexadecimal string
			var sb = new StringBuilder();
			foreach (var t in hashBytes)
			{
				sb.Append(t.ToString("X2"));
			}
			return sb.ToString();
		}
	}
}