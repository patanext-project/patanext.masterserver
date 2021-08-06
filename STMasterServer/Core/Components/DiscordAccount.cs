using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Account
{
	public struct DiscordAccount : IEntityComponent
	{
		public ulong Id;

		public DiscordAccount(ulong id) => Id = id;
	}
}