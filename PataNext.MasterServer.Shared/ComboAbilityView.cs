using MessagePack;

namespace PataNext.MasterServerShared
{
	[MessagePackObject]
	public struct MessageComboAbilityView
	{
		[Key(0)]
		public string Top;

		[Key(1)]
		public string Mid;

		[Key(2)]
		public string Bot;
	}
}