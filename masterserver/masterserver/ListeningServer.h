#include <winsock2.h>
#include <list> 
#include <map>
#include <iostream> 
#include "ClientServerEvent.h"
#include "Client.h"

#pragma once

using namespace std;

namespace P4TLBMasterServer
{
#pragma once
	class ListeningServer
	{
	public:
		ListeningServer(int port);
		~ListeningServer();

		void Bind(int& errorCode);
		void Listen(int& errorCode);

		void Update();
		bool PopEvent(ClientServerEvent& event);
		/// <summary>
		/// Get the current clients from the server
		/// </summary>
		/// <param name="wantedStatus">Used for filtering</param>
		/// <return>
		/// Read-only list of clients
		/// </return>
		const list<Client>& GetClients(Client::ClientStatus wantedStatus = Client::ClientStatus::NoFilter);

		void Accept(int connectionId);

		sockaddr_in GetAddress();

	private:
		int socketId;
		sockaddr_in serverAddress;

		int connectionId;

		list<ClientServerEvent> events;
		list<Client> clients;
		map<int, Client> clientMap;
	};
}