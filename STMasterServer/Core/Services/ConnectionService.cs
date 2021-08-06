using System;
using System.Collections.Generic;
using GameHost.Core.Ecs;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion;
using MagicOnion.Server;
using project.DataBase;
using STMasterServer.Shared.Services;

namespace project.Core.Services
{
	public class ConnectionService : STServiceBase<IConnectionService>, IConnectionService
	{
		public async UnaryResult<string> GetLogin(string representation)
		{
			await DependencyResolver.AsTask;
			
			var entity = db.GetEntity<UserEntity>(representation);
			if (entity.IsNull)
				throw new RpcException(new(StatusCode.NotFound, "no such account"));

			return (await entity.GetAsync<UserAccount>()).Login;
		}

		public UnaryResult<bool> Disconnect(string token)
		{
			Console.WriteLine("yo " + token);
			return UnaryResult(false);
		}
		
		private IEntityDatabase db;

		public ConnectionService([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
			DependencyResolver.Add(() => ref db);
		}
	}
}