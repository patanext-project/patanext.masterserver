using System.Threading.Tasks;
using P4TLB.MasterServer;
using project.P4Classes.Components;
using project.P4Classes.Entities;

namespace project.P4Classes
{
	public class UnitKitComponentManager : BaseUnitComponentManager
	{
		protected override async void OnUnitEvent<T>(object caller, string eventName, T data)
		{
			if (data is OnUnitCreated || data is OnUnitUpdated)
			{
				await EntityManager.ReplaceComponent<UnitEntityDescription, P4KitComponent, P4KitData>(new UnitEntityDescription {Id = data.UnitData.Id}, new P4KitComponent
				{
					Serialized = new P4KitData {KitId = 1, UnitId = data.UnitData.Id}
				});
			}
		}

		public async Task<P4KitData> GetCurrentKit(ulong unitId)
		{
			return (await EntityManager.GetComponent<UnitEntityDescription, P4KitComponent, P4KitData>(new UnitEntityDescription {Id = unitId})).Serialized;
		}
	}
}