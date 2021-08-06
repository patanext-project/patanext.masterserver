using project.DataBase.Ecs;

namespace PataNext.MasterServer.Entities
{
	public struct GameSaveAlmightyName : IEntityComponent
	{
		public string Value;

		public GameSaveAlmightyName(string value)
		{
			Value = value;
		}
	}
}