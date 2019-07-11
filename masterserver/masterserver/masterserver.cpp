// masterserver.cpp : Ce fichier contient la fonction 'main'. L'exécution du programme commence et se termine à cet endroit.
//

#include "pch.h"
#include <iostream>

#include "ListeningServer.h"

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

	getchar();

	return EXIT_SUCCESS;
}
