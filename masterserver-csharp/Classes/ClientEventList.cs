using System.Collections;
using System.Collections.Generic;
using P4TLB.MasterServer;

namespace P4TLBMasterServer
{
	public class ClientEventList : IEnumerable<CheckEventResponse.Types.EventData>
	{
		private List<CheckEventResponse.Types.EventData> list = new List<CheckEventResponse.Types.EventData>();

		public void Add(string ev)
		{
			list.Add(new CheckEventResponse.Types.EventData {Name = ev});
		}

		public void Clear()
		{
			list.Clear();
		}
		
		public IEnumerator<CheckEventResponse.Types.EventData> GetEnumerator()
		{
			return list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}
	}
}