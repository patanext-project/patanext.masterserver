using PataNext.MasterServer.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Game.Items
{
	public struct ItemEquippedBy : IEntityComponent
	{
		public DbEntityRepresentation<UnitEntity>[] UnitRepresentations;

		public DbEntityKey<UnitEntity>[] ToEntities(IEntityDatabase db)
		{
			var array = new DbEntityKey<UnitEntity>[UnitRepresentations.Length];
			for (var i = 0; i < array.Length; i++)
				array[i] = UnitRepresentations[i].ToEntity(db);

			return array;
		}
	}
}