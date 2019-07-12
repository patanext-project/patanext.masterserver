#pragma once

namespace P4TLBMasterServer
{
	enum ClientStatus
	{
		/// Used for filtering
		NoFilter,
		/// The client is not connected to the server (he maybe was connected was before)
		Disconnected,
		/// The client is currently connecting (this is the phase where we see if it's a gameserver or an user
		Connecting,
		/// The client is connected
		Connected
	};

    struct Client
	{
	public:
		ClientStatus Status;
		int ConnectionId;
	};
}