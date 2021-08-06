using PataNext.MasterServer.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Account
{
	public struct UserFavoriteGameSave : IEntityComponent
	{
		public DbEntityRepresentation<GameSaveEntity> Entity;

		public UserFavoriteGameSave(DbEntityRepresentation<GameSaveEntity> rep)
		{
			Entity = rep;
		}
	}
}