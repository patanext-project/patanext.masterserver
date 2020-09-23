using GameHost.Injection;
using GameHost.Threading.Apps;
using GameHost.Worlds;

namespace project.Core
{
	public class MasterServerApplication : CommonApplicationThreadListener
	{
		public MasterServerApplication(GlobalWorld source, Context overrideContext) : base(source, overrideContext)
		{
		}
	}
}