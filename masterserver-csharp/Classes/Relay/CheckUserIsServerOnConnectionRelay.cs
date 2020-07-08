using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using P4TLB.MasterServer;
using P4TLBMasterServer.Events;

namespace P4TLBMasterServer.Relay
{
	public class ServerEndPoint
	{
		public IPEndPoint Value;
	}
	
	public struct ServerData
	{
		public Client     Client;	

		public ulong  ServerId;
		public string ServerLogin;

		public string Name             { get; set; }
		public int    CurrentUserCount { get; set; }
		public int    MaxUsers         { get; set; }
	}

	public class ConnectedServerManager : ManagerBase
	{
		public Dictionary<ulong, ServerData> ServerDictionary;

		private UserDatabaseManager userDbMgr;
		private ClientManager clientMgr;

		public override void OnCreate()
		{
			base.OnCreate();

			ServerDictionary = new Dictionary<ulong, ServerData>();
			World.GetOrCreateManager<CheckUserIsServerOnConnectionRelay>();

			userDbMgr = World.GetOrCreateManager<UserDatabaseManager>();
			clientMgr = World.GetOrCreateManager<ClientManager>();
		}

		public void Update(ref ServerData serverData)
		{
			ServerDictionary[serverData.ServerId] = serverData;
		}

		public void Add(Client client, DataUserAccount account)
		{
			if (clientMgr.GetOrCreateData<ServerEndPoint>(client).Value == null)
				throw new InvalidOperationException("Server should have an endpoint for player to connect to it!");

			System.Console.WriteLine("add server: " + account.Id);

			ServerDictionary.Add(account.Id, new ServerData
			{
				Client = client, ServerId = account.Id, ServerLogin = account.Login,
				Name   = $"{account.Login}#{account.Id}"
			});
		}

		public void Remove(ulong id)
		{
			ServerDictionary.Remove(id);
		}

		public async Task<(bool success, ServerData server)> TryGetServer(ulong serverId, string serverLogin)
		{
			if (serverId == 0)
			{
				serverId = await userDbMgr.GetIdFromLogin(serverLogin);
			}

			return serverId == 0 ? (false, default) : (true, ServerDictionary[serverId]);
		}
	}

	public class CheckUserIsServerOnConnectionRelay : ManagerBase
	{
		private ConnectedServerManager connectedListMgr;

		public override void OnCreate()
		{
			base.OnCreate();
			connectedListMgr = World.GetOrCreateManager<ConnectedServerManager>();

			System.Console.WriteLine("Created CheckuserIsSystem");
		}

		public override void OnNotification<T>(object caller, string eventName, T data)
		{
			if (data is OnUserConnection onUserConnection)
			{
				System.Console.WriteLine("On notification: " + onUserConnection.User.Login + "; " + onUserConnection.User.Type);
				if (onUserConnection.User.Type == AccountType.Player)
					return;

				connectedListMgr.Add(onUserConnection.Client, onUserConnection.User);
			}
			else if (data is OnUserDisconnection onUserDisconnection)
			{
				if (onUserDisconnection.User.Type == AccountType.Player)
					return;

				connectedListMgr.Remove(onUserDisconnection.User.Id);
			}
		}
	}
}