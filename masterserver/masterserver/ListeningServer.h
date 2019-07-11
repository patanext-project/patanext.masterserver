#include <winsock2.h>

#pragma once
class ListeningServer
{
public:
	ListeningServer(int port);
	~ListeningServer();

	void Bind(int& errorCode);
	void Listen(int& errorCode);

	sockaddr_in GetAddress();

private:
	int socketId;
	sockaddr_in serverAddress;

	int connectedCount;
};

