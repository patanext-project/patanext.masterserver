using System.Collections.Generic;

namespace P4TLBMasterServer
{
	public class ClientManager : ManagerBase
	{
		private Queue<int> m_RecycledIds;
		private int        m_ConnectionUniqueCounter;

		public override void OnCreate()
		{
			m_RecycledIds             = new Queue<int>();
			m_ConnectionUniqueCounter = 0;
		}

		public int CreateClient()
		{
			var id = -1;
			if (!m_RecycledIds.TryDequeue(out id))
			{
				id = m_ConnectionUniqueCounter++;
			}

			return id;
		}

		public void RemoveClient(int id)
		{
			m_RecycledIds.Enqueue(id);
		}
	}
}