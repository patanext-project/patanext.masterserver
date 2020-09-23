using PataNext.MasterServer.Entities;
using project;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Account
{
	public struct UserFavoriteGameSave : IEntityComponent<UserEntity>
	{
		public DbEntityRepresentation<GameSaveEntity> Entity;
	}
}