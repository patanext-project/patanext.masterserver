namespace P4TLBMasterServer
{
	public struct Client
	{
		/// <summary>
		/// ID of a client that was connected.
		/// No clients can have the same id.
		/// </summary>
		public int Id;
		/// <summary>
		/// The last token that the client used to connect as an user
		/// </summary>
		public string Token;
	}
}