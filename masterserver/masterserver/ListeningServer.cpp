#include "pch.h"
#include "ListeningServer.h"
#include <sys/types.h> 
#include <winsock2.h>

#pragma comment(lib, "ws2_32.lib")

namespace P4TLBMasterServer
{
	ListeningServer::ListeningServer(int port)
	{
		WSADATA WSAData;
		WSAStartup(MAKEWORD(2, 2), &WSAData);

		// be sure that the memory is blank...
		memset(&serverAddress, 0, sizeof(serverAddress));

		serverAddress.sin_family = AF_INET; // ipv4
		serverAddress.sin_addr.s_addr = htonl(INADDR_ANY);
		serverAddress.sin_port = htons(port);
	}

	void ListeningServer::Bind(int& errorCode)
	{
		u_long nonBlockingLong = 1;

		socketId = socket(AF_INET, SOCK_STREAM, 0);

		errorCode = bind(socketId, (struct sockaddr*) &serverAddress, sizeof(serverAddress));

		ioctlsocket(socketId, FIONBIO, &nonBlockingLong); // non blocking
	}

	void ListeningServer::Listen(int& errorCode)
	{
		errorCode = listen(socketId, 5);
	}

	bool ListeningServer::PopEvent(ClientServerEvent& event)
	{
		sockaddr address;
		int addrLen = sizeof(address);
		auto result = accept(socketId, &address, &addrLen);
		if (result != INVALID_SOCKET)
		{
			// Create client
			Client client;
			client.ConnectionId = connectionId++;
			client.Status = ClientStatus::Connecting;
			clients.push_back(client);
			clientMap.insert(pair<int, Client>(client.ConnectionId, client));

			// We got a connection...
			event.Type = EventType::Connection;
			event.Client = client;
			return true;
		}

		return false;
	}

	const list<Client>& ListeningServer::GetClients(ClientStatus status)
	{
		return clients;
	}

	void ListeningServer::Accept(Client client)
	{
		auto it = clientMap.find(client.ConnectionId);
		if (it == clientMap.end())
		{
			std::cout << "No client found with connectionId: " << client.ConnectionId;
		}

		client = clientMap.find(client.ConnectionId)->second;
	}

	ListeningServer::~ListeningServer()
	{
	}
}