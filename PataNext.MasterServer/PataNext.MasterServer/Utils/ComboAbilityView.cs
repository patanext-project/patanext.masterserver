using project.Core.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Utils
{
	public struct ComboAbilityView : IEntityComponent
	{
		public DbEntityRepresentation<AssetEntity> Top, Mid, Bot;

		public ComboAbilityView(DbEntityRepresentation<AssetEntity> top = default,
		                        DbEntityRepresentation<AssetEntity> mid = default,
		                        DbEntityRepresentation<AssetEntity> bot = default)
		{
			Top = top;
			Mid = mid;
			Bot = bot;
		}
	}
}