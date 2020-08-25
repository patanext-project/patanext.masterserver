using System;
using System.Threading.Tasks;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using project.P4Classes.Entities;

namespace project.P4Classes.Components
{
	public class P4PresetCurrentEquipmentComponent : IComponent<UnitEntityDescription, P4PresetCurrentEquipmentData>
	{
		public P4PresetCurrentEquipmentData Serialized { get; set; }
		
		public async Task<bool> OnGetOperationRequested(UnitEntityDescription entity, World world, DatabaseManager dbMgr)
		{
			Serialized = await ComponentInvoke.Get<UnitEntityDescription, P4PresetCurrentEquipmentComponent, P4PresetCurrentEquipmentData>(entity, dbMgr);
			return Serialized != null;
		}

		public async Task<bool> OnUpdateOperationRequested(UnitEntityDescription entity, World world, DatabaseManager dbMgr)
		{
			return await ComponentInvoke.Update<UnitEntityDescription, P4PresetCurrentEquipmentComponent, P4PresetCurrentEquipmentData>(entity, this, dbMgr);
		}

		public async Task<bool> OnRemoveOperationRequested(UnitEntityDescription entity, World world, DatabaseManager dbMgr)
		{
			return await ComponentInvoke.Remove<UnitEntityDescription, P4PresetCurrentEquipmentComponent, P4PresetCurrentEquipmentData>(entity, dbMgr);
		}

		public void CheckComponentValidity(UnitEntityDescription entity, World world)
		{
			if (entity.Id != Serialized.UnitId)
				throw new Exception("Entity is expected to have the same ID as the serialized data...");
		}
	}
}