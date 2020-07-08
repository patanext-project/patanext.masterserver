using System.Collections.Generic;

namespace P4TLBMasterServer
{
	public class UserServerLink
	{
		public ulong ServerId;
	}

	public class ServerUserList
	{
		public HashSet<ulong> UserIds = new HashSet<ulong>();
	}
}