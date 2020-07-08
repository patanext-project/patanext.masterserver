using System;
using System.Collections;
using System.Collections.Generic;
using P4TLB.MasterServer;

namespace P4TLBMasterServer
{
	public class ClientEventList : IEnumerable<CheckEventResponse.Types.EventData>
	{
		private HashSet<CheckEventResponse.Types.EventData> hashSet = new HashSet<CheckEventResponse.Types.EventData>();

		public void Add(string ev)
		{
			hashSet.Add(new CheckEventResponse.Types.EventData {Name = ev});
		}

		public void Clear()
		{
			hashSet.Clear();
		}
		
		public IEnumerator<CheckEventResponse.Types.EventData> GetEnumerator()
		{
			return hashSet.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return hashSet.GetEnumerator();
		}
	}
}