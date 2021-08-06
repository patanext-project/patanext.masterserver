using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion;
using MagicOnion.Server.Hubs;
using project.Core.Components;
using project.Core.Systems;
using project.DataBase;
using STMasterServer.Shared.Services;

namespace project.Core.Services
{
	[Flags]
	public enum STConnectionType
	{
		NotConnected  = 0,
		User          = 1,
		Server        = 2,
		UserAndServer = User | Server
	}

	public abstract class STGameServerStreamingHubBase<TInterface, TReceiver> : STStreamingHubBase<TInterface, TReceiver>
		where TInterface : IStreamingHub<TInterface, TReceiver>, IServiceSupplyUserToken
	{
		protected IEntityDatabase     db;
		protected ConnectedUserSystem connectedUserSystem;

		protected STGameServerStreamingHubBase([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
			DependencyResolver.Add(() => ref db);
			DependencyResolver.Add(() => ref connectedUserSystem);
		}

		public STConnectionType ConnectionType { get; private set; }
		public bool             IsUser         => (ConnectionType & STConnectionType.User) != 0;
		public bool             IsServer       => (ConnectionType & STConnectionType.Server) != 0;

		public IReadOnlyDictionary<string, IGroup>? ServerUserGroups { get; private set; }
		public IGroup?                              CurrentUserGroup { get; private set; }

		protected UserToken               CurrentToken;
		protected DbEntityKey<UserEntity> CurrentUser;
		protected DbEntityKey<UserEntity> CurrentServer;

		protected abstract void UserOnJoinServer();
		protected abstract void ServerOnUserJoined(DbEntityKey<UserEntity> userEntity, IGroup userGroup);
		
		// The reason why we can't directly implement SupplyUserToken is that MagicOnion doesn't recognize base/super methods...
		// issue: https://github.com/Cysharp/MagicOnion/issues/330
		protected async Task BaseSupplyUserToken(UserToken token)
		{
			if (!connectedUserSystem.TryMatch(token, out var representation))
			{
				CurrentUser = default;
				throw new RpcException(new Status(StatusCode.NotFound, "invalid token"), $"Invalid Token '{token.Representation}'");
			}

			CurrentToken = token;
			CurrentUser  = representation.ToEntity(db);

			if (CurrentUserGroup != null)
				Group.RawGroupRepository.TryRemove(CurrentUserGroup.GroupName);

			CurrentUserGroup = await Group.AddAsync(representation.Value);

			/* TODO: there should be a way to connect your own account as a server */
			ConnectionType = await CurrentUser.HasAsync<UserIsServerAccount>()
				? STConnectionType.Server
				: STConnectionType.User;

			if (IsServer)
				CurrentServer = CurrentUser;
		}

		protected void BroadcastToGroups(Action<TReceiver> action)
		{
			if (ServerUserGroups != null)
				foreach (var (_, group) in ServerUserGroups)
					action(Broadcast(group));

			if (CurrentUserGroup != null)
				action(Broadcast(CurrentUserGroup));
		}
	}
}