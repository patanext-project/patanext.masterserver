using project;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Account
{
	public struct DiscordAccount : IEntityComponent<UserEntity>
	{
		public ulong Id;

		public DiscordAccount(ulong id) => Id = id;
	}
}