// masterserver.cpp : Ce fichier contient la fonction 'main'. L'exécution du programme commence et se termine à cet endroit.
//

#include "pch.h"
#include <iostream>

#include "ListeningServer.h"
#include "ClientServerEvent.h"

using namespace P4TLBMasterServer;

int main()
{
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

		while (server->PopEvent(ev))
		{
			switch (ev.Type)
			{
			case EventType::Connection:
				std::cout << "Client connected!";
				
				server->Accept(ev.Client);

				break;

			case EventType::Stream:
				std::cout << "Message received!";
				break;
			}
		}
	}

	return EXIT_SUCCESS;
}