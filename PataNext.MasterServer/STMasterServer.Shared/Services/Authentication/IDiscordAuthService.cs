using MagicOnion;
using MessagePack;

namespace STMasterServer.Shared.Services.Authentication
{
	public interface IDiscordAuthService : IService<IDiscordAuthService>
	{
		UnaryResult<DiscordAuthBegin> BeginAuth(ulong userId);

		UnaryResult<ConnectResult> FinalizeAuth(string lobbyId, string token);
	}

	[MessagePackObject(true)]
	public struct DiscordAuthBegin
	{
		public string RequiredLobbyName;
		public string StepToken;
	}
}