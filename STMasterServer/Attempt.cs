using System.Diagnostics.Contracts;

namespace project
{
	public struct Attempt<T>
	{
		public T    Result    { get; }
		public bool HasResult { get; }

		public Attempt(T value)
		{
			HasResult = true;
			Result    = value;
		}

		[Pure]
		public bool TryGet(out T value)
		{
			value = Result;
			return HasResult;
		}
	}
}