using System.Threading.Tasks;
using Grpc.Core;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using project.Messages;

namespace project.P4Classes
{
	[Implementation(typeof(P4ArmyFormationService))]
	public class ArmyFormationServiceImpl : P4ArmyFormationService.P4ArmyFormationServiceBase
	{
		public World World { get; set; }

		public override async Task<GetFormationOfPlayerResult> GetFormationOfPlayer(DataOfPlayerRequest request, ServerCallContext context)
		{
			if (request.UserId == 0 && string.IsNullOrEmpty(request.UserLogin))
				return new GetFormationOfPlayerResult {Error = GetFormationOfPlayerResult.Types.ErrorCode.InvalidRequest};

			var formationDbMgr = World.GetOrCreateManager<FormationDatabaseManager>();
			var formationId    = 0u;
			if (request.UserId > 0)
			{
				formationId = await formationDbMgr.FindFormationIdByUserId(request.UserId);
			}

			// if request.UserId was 0 or if we didn't found it with the id...
			if (formationId == 0)
			{
				var   userDbMgr = World.GetOrCreateManager<UserDatabaseManager>();
				ulong id;
				if ((id = userDbMgr.GetIdFromLogin(request.UserLogin)) <= 0)
				{
					return new GetFormationOfPlayerResult {Error = GetFormationOfPlayerResult.Types.ErrorCode.InvalidRequest};
				}

				formationId = await formationDbMgr.FindFormationIdByUserId(request.UserId);
			}

			// still nothing?
			if (formationId == 0)
			{
				return new GetFormationOfPlayerResult {Error = GetFormationOfPlayerResult.Types.ErrorCode.NoFormation};
			}

			var formation = await formationDbMgr.FindFormation(formationId);
			return new GetFormationOfPlayerResult {Result = formation, Error = GetFormationOfPlayerResult.Types.ErrorCode.Success};
		}
	}
}