using System;

namespace P4TLBMasterServer
{
	public struct Client : IEquatable<Client>
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

		public bool Equals(Client other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is Client other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Id;
		}
	}
}