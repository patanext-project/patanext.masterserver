#pragma once

using namespace P4TLBMasterServer;

namespace P4TLBMasterServerRequests
{
	class GetUserLoginData
	{
	public:
		int connectionId;
		int userGuid;
	};

	class GetUserLogin : public Request<GetUserLoginData>
	{
		string Request::getPath()
		{
			return "GET/UserLogin";
		}

		virtual void Read(int connectionId, GetUserLoginData & output) override;

		// Hérité via Request
		virtual void Process(ListeningServer * server) override;
	};
}