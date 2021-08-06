using System;
using GameHost.Core.Ecs;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion;
using MagicOnion.Server;
using project.Core.Systems;
using project.DataBase;
using STMasterServer.Shared.Services.Authentication;

namespace project.Core.Services.Authentication
{
	public class StandardAuthService : STServiceBase<IStandardAuthService>, IStandardAuthService
	{
		private ConnectedUserSystem connectedUserSystem;
		private IEntityDatabase     db;
		
		public StandardAuthService([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
			DependencyResolver.Add(() => ref connectedUserSystem);
			DependencyResolver.Add(() => ref db);
		}
		
		public async UnaryResult<ConnectResult> ConnectViaGuid(string guid, string password)
		{
			await DependencyResolver.AsTask;
			
			var userList = await (await db.GetMatchFilter<UserEntity>())
			                     .IsFieldEqual((UserAccount c) => c.Password, password)
			                     .RunAsync(1);

			if (userList.Count == 0)
				throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Guid or Password"), "invalid pwd");

			var token = connectedUserSystem.GetOrCreateToken(new DbEntityRepresentation<UserEntity> {Value = guid});
			return new ConnectResult
			{
				Guid  = guid,
				Token = token
			};
		}

		public async UnaryResult<ConnectResult> ConnectViaLogin(string login, string password)
		{
			await DependencyResolver.AsTask;
			
			var userList = await (await db.GetMatchFilter<UserEntity>())
			                 .IsFieldEqual((UserAccount c) => c.Login, login)
			                 .RunAsync(1);
			
			if (userList.Count == 0)
				throw new Exception("invalid login");
			
			return await ConnectViaGuid(db.RepresentationOf(userList[0]), password);
		}
	}
}