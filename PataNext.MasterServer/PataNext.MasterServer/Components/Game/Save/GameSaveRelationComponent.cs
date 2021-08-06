using PataNext.MasterServer.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.GameSave
{
	public struct GameSaveRelationComponent : IEntityComponent
	{
		public DbEntityRepresentation<GameSaveEntity> Value;

		public GameSaveRelationComponent(DbEntityRepresentation<GameSaveEntity> value)
		{
			Value = value;
		}
	}
}