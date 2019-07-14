namespace P4TLBMasterServer
{
	public enum ClientStatus
	{
		Invalid,
		Connecting,
		Connected,
		Disconnected
	}
	
	public struct Client
	{
		public ClientStatus Status;
	}
}