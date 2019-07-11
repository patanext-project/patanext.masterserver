#include "pch.h"
#include "ListeningServer.h"
#include <iostream>
#include <sys/types.h> 
#include <winsock2.h>

#pragma comment(lib, "ws2_32.lib")

ListeningServer::ListeningServer(int port)
{
	WSADATA WSAData;
	WSAStartup(MAKEWORD(2, 2), &WSAData);

	// be sure that the memory is blank...
	memset(&serverAddress, 0, sizeof(serverAddress));

	serverAddress.sin_family      = AF_INET; // ipv4
	serverAddress.sin_addr.s_addr = htonl(INADDR_ANY);
	serverAddress.sin_port        = htons(port);
}

void ListeningServer::Bind(int& errorCode)
{
	socketId = socket(AF_INET, SOCK_STREAM, 0);

	errorCode = bind(socketId, (struct sockaddr*) &serverAddress, sizeof(serverAddress));
}

void ListeningServer::Listen(int& errorCode)
{
	errorCode = listen(socketId, 5);
}

ListeningServer::~ListeningServer()
{
}
