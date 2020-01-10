using P4TLB.MasterServer;

namespace P4TLBMasterServer.Events
{
	public struct OnUserConnection
	{
		public DataUserAccount User;
		public Client Client;
	}
}