using System.Threading.Tasks;
using Grpc.Core;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using project.Messages;
using project.P4Classes.Components;
using project.P4Classes.Entities;

namespace project.P4Classes
{
	[Implementation(typeof(P4UnitKitService))]
	public class UnitKitServiceImpl : P4UnitKitService.P4UnitKitServiceBase
	{
		public World               World               { get; set; }
		public EntityManager       EntityManager       { get; set; }
		public ClientManager       ClientManager       { get; set; }
		public UnitDatabaseManager UnitDatabaseManager { get; set; }

		public override async Task<GetKitResponse> GetCurrentKit(GetKitRequest request, ServerCallContext context)
		{
			var entity = new UnitEntityDescription {Id = request.UnitId};
			var result = new GetKitResponse();
			if (!await EntityManager.HasComponent<UnitEntityDescription, P4KitComponent>(entity))
			{
				result.Error = GetKitResponse.Types.ErrorCode.NotFound;
				return result;
			}

			var kit = await EntityManager.GetComponent<UnitEntityDescription, P4KitComponent, P4KitData>(entity);
			kit.CheckComponentValidity(entity, World);

			result.KitId           = kit.Serialized.KitId;
			result.KitCustomNameId = kit.Serialized.KitCustomNameId;
			return result;
		}

		public override async Task<SetKitResponse> SetCurrentKit(SetKitRequest request, ServerCallContext context)
		{
			if (!ClientManager.GetClient(request.ClientToken, out var client))
				return new SetKitResponse {Error = SetKitResponse.Types.ErrorCode.Unauthorized};

			var entity = new UnitEntityDescription {Id = request.UnitId};
			if (!await EntityManager.DbExists(entity))
				return new SetKitResponse {Error = SetKitResponse.Types.ErrorCode.EntityNotFound};

			var dbData = await UnitDatabaseManager.FindUnit(entity.Id);
			if (dbData.UserId != ClientManager.GetOrCreateData<DataUserAccount>(client).Id)
				return new SetKitResponse {Error = SetKitResponse.Types.ErrorCode.Unauthorized};

			var success = await EntityManager.ReplaceComponent<UnitEntityDescription, P4KitComponent, P4KitData>(entity, new P4KitComponent
			{
				Serialized = new P4KitData
				{
					UnitId          = entity.Id,
					KitId           = request.KitId,
					KitCustomNameId = request.KitCustomNameId
				}
			});

			return success switch
			{
				false => new SetKitResponse {Error = SetKitResponse.Types.ErrorCode.UnknownError},
				true => new SetKitResponse {Error  = SetKitResponse.Types.ErrorCode.Ok}
			};
		}
	}
}