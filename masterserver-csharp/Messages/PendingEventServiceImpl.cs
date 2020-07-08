using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Grpc.Core;
using P4TLB.MasterServer;
using P4TLBMasterServer;

namespace project.Messages
{
	[Implementation(typeof(PendingEventService))]
	public class PendingEventServiceImpl : PendingEventService.PendingEventServiceBase
	{
		public World World { get; set; }

		public override async Task<CheckEventResponse> GetPending(CheckEventRequest request, ServerCallContext context)
		{
			var clientMgr = World.GetOrCreateManager<ClientManager>();
			if (!clientMgr.GetClient(request.ClientToken, out var client))
				return new CheckEventResponse();

			var eventList = clientMgr.GetOrCreateData<ClientEventList>(client);
			var ev = new CheckEventResponse {Success = true, Events = {eventList}};
			eventList.Clear();
			
			return ev;
		}
	}
}