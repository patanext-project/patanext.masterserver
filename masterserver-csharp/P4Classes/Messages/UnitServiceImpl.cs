using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Grpc.Core;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using project.Messages;
using project.P4Classes.Entities;

namespace project.P4Classes
{
	[Implementation(typeof(P4UnitService))]
	public class UnitServiceImpl : P4UnitService.P4UnitServiceBase
	{
		public EntityManager       EntityManager { get; set; }
		public ClientManager ClientManager { get; set; }
		public UnitDatabaseManager UnitDbMgr     { get; set; }

		public override Task<UnitServiceGetPendingEventResponse> GetPendingEvents(UnitServiceGetPendingEventRequest request, ServerCallContext context)
		{
			if (!ClientManager.GetClient(request.ClientToken, out var client))
				throw new RpcException(new Status(StatusCode.Unauthenticated, "invalid client token"));

			var hashset  = ClientManager.GetOrCreateData<ClientUnitUpdateEvent>(client).HashSet;
			var response = new UnitServiceGetPendingEventResponse();
			if (hashset.Count > 0)
				response.Units.AddRange(hashset.Select(u => new UnitServiceGetPendingEventResponse.Types.RUnitId {UnitId = u}));
			hashset.Clear();

			return Task.FromResult(response);
		}

		public override async Task<CheckExistsResponse> UnitExists(CheckExistsRequest request, ServerCallContext context)
		{
			return request.UnitId switch
			{
				0 => new CheckExistsResponse {Exists = false},
				_ => new CheckExistsResponse {Exists = await EntityManager.DbExists(new UnitEntityDescription {Id = request.UnitId})}
			};
		}

		public override async Task<GetUnitDataResponse> GetUnitData(GetUnitDataRequest request, ServerCallContext context)
		{
			var data = await UnitDbMgr.FindUnit(request.UnitId);
			return new GetUnitDataResponse {Data = data};
		}
	}
}