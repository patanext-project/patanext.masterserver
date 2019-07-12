#include "Client.h"

#pragma once

namespace P4TLBMasterServer
{
	enum EventType
	{
		Connection,
		Stream,
		Disconnection
	};

	struct ClientServerEvent
	{
	public:
		EventType Type;
		Client Client;
	};
}