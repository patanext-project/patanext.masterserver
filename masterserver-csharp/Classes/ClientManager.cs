using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using P4TLB.MasterServer;

namespace P4TLBMasterServer
{
	public class ClientManager : ManagerBase
	{
		private Dictionary<string, Client> m_ClientFromTokens;
		private Dictionary<int, Client>    m_Clients;
		private Dictionary<int, Dictionary<Type, object>> m_ObjectMapFromClients;

		private Dictionary<ulong, int> m_UserToClient;

		private int m_UniqueConnectionId;

		public int ConnectedCount => m_Clients.Count;

		public override void OnCreate()
		{
			m_ClientFromTokens = new Dictionary<string, Client>(16);
			m_Clients          = new Dictionary<int, Client>(16);
			m_ObjectMapFromClients = new Dictionary<int, Dictionary<Type, object>>(16);
			m_UserToClient = new Dictionary<ulong, int>(16);
			
			m_UniqueConnectionId = 1;
		}

		/// <summary>
		/// Connect a client with a token
		/// </summary>
		/// <param name="tokenData"></param>
		/// <returns></returns>
		public Client ConnectClient(string tokenData = "")
		{
			var client = new Client
			{
				Id    = m_UniqueConnectionId++,
				Token = GetToken(tokenData)
			};
			m_Clients[client.Id]             = client;
			m_ClientFromTokens[client.Token] = client;
			m_ObjectMapFromClients[client.Id] = new Dictionary<Type, object>();

			return client;
		}

		/// <summary>
		/// (Force) Disconnect a client
		/// </summary>
		/// <param name="id"></param>
		public void DisconnectClientById(int id)
		{
			if (m_Clients.TryGetValue(id, out var client))
			{
				m_ClientFromTokens.Remove(client.Token);
				m_Clients.Remove(id);
				m_ObjectMapFromClients.Remove(id);
			}
		}

		/// <summary>
		/// Disconnect a client if the token is valid
		/// </summary>
		/// <param name="token"></param>
		public void DisconnectClientByToken(string token)
		{
			if (m_ClientFromTokens.TryGetValue(token, out var client))
			{
				m_ClientFromTokens.Remove(token);
				m_Clients.Remove(client.Id);
				m_ObjectMapFromClients.Remove(client.Id);
			}
		}

		/// <summary>
		/// (Force) disconnect a client
		/// </summary>
		/// <param name="client"></param>
		public void DisconnectClient(Client client)
		{
			m_ClientFromTokens.Remove(client.Token);
			m_Clients.Remove(client.Id);
			m_ObjectMapFromClients.Remove(client.Id);
		}
		
		/// <summary>
		/// Replace a client data of type <see cref="T"/>
		/// </summary>
		/// <param name="client">The client</param>
		/// <param name="data">The data</param>
		/// <typeparam name="T">The type of data</typeparam>
		public void ReplaceData<T>(Client client, T data)
			where T : class
		{
			m_ObjectMapFromClients[client.Id][typeof(T)] = data;
		}

		/// <summary>
		/// Get or create a new data for the client
		/// </summary>
		/// <param name="client">The client</param>
		/// <typeparam name="T">The type of data</typeparam>
		/// <returns>The data</returns>
		public T GetOrCreateData<T>(Client client)
			where T : class, new()
		{
			var type = typeof(T);
			if (m_ObjectMapFromClients[client.Id].TryGetValue(type, out var obj))
				return (T) obj;
			m_ObjectMapFromClients[client.Id][type] = obj = new T();
			return (T) obj;
		}

		public bool TryGetClientFromUserId(ulong userId, out Client client)
		{
			int clientId;
			if ((clientId = GetClientIdByUserId(userId)) <= 0)
			{
				client = default;
				return false;
			}

			return GetClient(clientId, out client);
		}

		/// <summary>
		/// Link the user with the client
		/// </summary>
		/// <param name="userAccount"></param>
		/// <param name="client"></param>
		internal void LinkUserClient(DataUserAccount userAccount, Client client)
		{
			m_UserToClient[userAccount.Id] = client.Id;
		}

		internal void UnlinkUserClient(DataUserAccount userAccount, Client client)
		{
			m_UserToClient.Remove(userAccount.Id);
		}

		internal int GetClientIdByUserId(ulong id)
		{
			m_UserToClient.TryGetValue(id, out var clientId);
			return clientId;
		}

		public bool GetClient(int connectionId, out Client client)
		{
			return m_Clients.TryGetValue(connectionId, out client);
		}

		public bool GetClient(string token, out Client client)
		{
			return m_ClientFromTokens.TryGetValue(token, out client);
		}
		
		private string GetToken(string tokenData)
		{
			tokenData = tokenData.Replace("DISCORD_", string.Empty).Substring(0, 8);

			var length     = 15;
			var privateStr = default(string);
			using (var rng = new RNGCryptoServiceProvider())
			{
				var bytes = new byte[(length * 6 + 7) / 8];
				rng.GetBytes(bytes);
				privateStr = Convert.ToBase64String(bytes);
			}

			privateStr = privateStr.Replace(':', 'c');

			return $"{tokenData}:{privateStr}:{m_UniqueConnectionId % 100}";
		}
	}
}