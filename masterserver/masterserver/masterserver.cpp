// masterserver.cpp : Ce fichier contient la fonction 'main'. L'exécution du programme commence et se termine à cet endroit.
//

#include "pch.h"
#include <iostream>

#include "ListeningServer.h"
#include "ClientServerEvent.h"
#include "msg/Request.h"
#include "msg/GET/GetUserLogin.h"

#include <list>

using namespace P4TLBMasterServer;
using namespace P4TLBMasterServerRequests;
using namespace std;

void addRequest(list<RequestBase*>& list, RequestBase* request)
{
	list.push_back(request);
}

void addRequests(list<RequestBase*>& list)
{
	addRequest(list, new GetUserLogin());
}

int main()
{
	list<RequestBase*> requests;
	addRequests(requests);
	
	for (RequestBase* request : requests)
	{
		std::cout << "request path: " << request->getPath() << "\n";
	}

    std::cout << "Starting the server!\n";

	int port = 4242;

	auto server = new ListeningServer(port);

	int bindErrorCode;
	server->Bind(bindErrorCode);
	if (bindErrorCode != 0)
	{
		std::cout << "Couldn't start the server :( " << bindErrorCode;
	}
	int listenErrorCode;
	server->Listen(listenErrorCode);
	if (listenErrorCode != 0)
	{
		std::cout << "Couldn't start listening... " << listenErrorCode;
	}

	std::cout << "Currently listening...\n";

	// loop until we are requested to close the application with the 'escape' key
	while (GetAsyncKeyState(VK_ESCAPE) == 0)
	{
		ClientServerEvent ev;

		server->Update();
		while (server->PopEvent(ev))
		{
			switch (ev.Type)
			{
			case ClientServerEvent::EventType::Connection:
				std::cout << "Client being connected!\n";
				
				server->Accept(ev.ClientConnectionId);

				break;

			case ClientServerEvent::EventType::Connected:
				std::cout << "Client " << ev.ClientConnectionId << " connected.\n";
				break;

			case ClientServerEvent::EventType::Stream:
				std::cout << "Message received!\n";
				break;
			}
		}
	}

	return EXIT_SUCCESS;
}