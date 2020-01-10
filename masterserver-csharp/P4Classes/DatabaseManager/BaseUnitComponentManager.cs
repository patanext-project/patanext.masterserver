using System.Threading.Tasks;
using Google.Protobuf;
using P4TLB.MasterServer;
using P4TLBMasterServer;

namespace project.P4Classes
{
	public abstract class BaseUnitComponentManager : ManagerBase
	{
		public DatabaseManager DatabaseManager { get; private set; }
		public EntityManager EntityManager { get; private set; }

		public override void OnCreate()
		{
			base.OnCreate();
			DatabaseManager = World.GetOrCreateManager<DatabaseManager>();
			EntityManager = World.GetOrCreateManager<EntityManager>();
		}

		public override void OnNotification<T>(object caller, string eventName, T data)
		{
			switch (data)
			{
				case OnProgramInitialized initialized:
					CheckAllUnitsOnStart();
					break;
				case IOnRelatedUnitEvent onUnitEvent:
					OnUnitEvent(caller, eventName, onUnitEvent);
					break;
			}
		}

		protected virtual void CheckAllUnitsOnStart()
		{
		}

		protected virtual void OnUnitEvent<T>(object caller, string eventName, T data)
			where T : IOnRelatedUnitEvent
		{
		}
	}
}