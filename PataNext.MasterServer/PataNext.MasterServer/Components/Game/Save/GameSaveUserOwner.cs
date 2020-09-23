using PataNext.MasterServer.Entities;
using project;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.GameSave
{
	public struct GameSaveUserOwner : IEntityComponent<GameSaveEntity>
	{
		public DbEntityRepresentation<UserEntity> Entity;

		public GameSaveUserOwner(DbEntityRepresentation<UserEntity> entity)
		{
			Entity = entity;
		}
	}
}