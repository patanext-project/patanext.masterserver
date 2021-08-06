using System.Collections.Generic;
using PataNext.MasterServer.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.GameSave
{
	// TODO: Is this the right way to do the formation?
	public struct GameSaveCurrentArmyFormation : IEntityComponent
	{
		public struct Squad
		{
			public DbEntityRepresentation<UnitEntity>   Leader;
			public DbEntityRepresentation<UnitEntity>[] Soldiers;
		}

		public DbEntityRepresentation<UnitEntity> FlagBearer;
		public DbEntityRepresentation<UnitEntity> UberHero;
		public Squad[]                            Squads;
	}
}