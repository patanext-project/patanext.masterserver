#include "GetUserLogin.h"

#include <string>
#include <list>

#include "../../ListeningServer.h";

using namespace P4TLBMasterServer;
using namespace std;

namespace P4TLBMasterServerRequests
{
	void GetUserLogin::Read(int connectionId, GetUserLoginData& output)
	{
		output.connectionId = connectionId;
		output.userGuid = reader.get<int>("UserGuid");
	}

	void GetUserLogin::Process(ListeningServer* server)
	{
		if (requests.empty())
			return;


		for (GetUserLoginData data : requests)
		{
			bool exists;
			auto client = server->GetClient(data.connectionId, exists);
			if (!exists)
				return;
		}
	}
}