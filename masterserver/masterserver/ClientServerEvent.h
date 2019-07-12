#include "Client.h"

#pragma once

namespace P4TLBMasterServer
{
	struct ClientServerEvent
	{
		enum EventType
		{
			Connection,
			Connected,
			Stream,
			Disconnection
		};

	public:
		EventType Type;
		int ClientConnectionId;
	};
}