#pragma once

#include <string>
#include <list>

using namespace std;
using namespace P4TLBMasterServer;

namespace P4TLBMasterServerRequests
{
	class RequestBase
	{
	public:
		virtual string getPath() = 0;

		virtual void InternalRead() = 0;
		virtual void Process(ListeningServer* server) = 0;
	};

	template <typename  TOutput>
	class Request : public RequestBase
	{
	public:
		virtual void InternalRead() override;

		virtual void Read(int connectionId, TOutput& output) = 0;

		list<TOutput> requests;
	};
}