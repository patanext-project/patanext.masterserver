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

	void ListeningServer::Update()
	{
		// Check for client connection...
		sockaddr address;
		int addrLen = sizeof(address);
		auto result = accept(socketId, &address, &addrLen);
		if (result != INVALID_SOCKET)
		{
			// Create client
			Client client;
			client.ConnectionId = connectionId++;
			client.Status = Client::ClientStatus::Connecting;
			clients.push_back(client);
			clientMap.insert(pair<int, Client>(client.ConnectionId, client));

			// We got a connection...
			ClientServerEvent event;
			event.Type = ClientServerEvent::EventType::Connection;
			event.ClientConnectionId = client.ConnectionId;

			events.push_back(event);
		}
	}

	bool ListeningServer::PopEvent(ClientServerEvent& event)
	{
		if (!events.empty())
		{
			event = events.front();
			events.pop_front();

			return true;
		}

		return false;
	}

	const list<Client>& ListeningServer::GetClients(Client::ClientStatus status)
	{
		return clients;
	}

	void ListeningServer::Accept(int connectionId)
	{
		auto it = clientMap.find(connectionId);
		if (it == clientMap.end())
		{
			std::cout << "No client found with connectionId: " << connectionId;
			return;
		}

		Client client = clientMap.find(connectionId)->second;
		if (client.Status == Client::ClientStatus::Connected)
			return; // ???

		client.Status = Client::ClientStatus::Connected;
		clientMap.insert_or_assign(connectionId, client);

		ClientServerEvent event;
		event.Type = ClientServerEvent::EventType::Connected;
		event.ClientConnectionId = connectionId;

		events.push_back(event);
	}

	ListeningServer::~ListeningServer()
	{
	}
}