using project;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.GameSave
{
	public struct GameSaveUserOwner : IEntityComponent
	{
		public DbEntityRepresentation<UserEntity> Entity;

		public GameSaveUserOwner(DbEntityRepresentation<UserEntity> entity)
		{
			Entity = entity;
		}
	}
}