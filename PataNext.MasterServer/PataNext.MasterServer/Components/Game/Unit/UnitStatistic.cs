using System;
using System.Numerics;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Game.Unit
{
	// Should it be separated into multiple components?
	public struct UnitStatistic : IEntityComponent
	{
		public int ExperiencePoints;

		#region Random Statistics

		/// <summary>
		/// How much meter this unit has walked?
		/// </summary>
		public int      MeterWalked;
		/// <summary>
		/// How much missions has been finished with this unit?
		/// </summary>
		public int      MissionsFinished;
		/// <summary>
		/// How much versus were won with this unit?
		/// </summary>
		/// <remarks>
		///	If you wish to know the score of an Unit, see <see cref="UnitRankingAlpha"/>
		/// </remarks>
		public int      VersusWon;
		/// <summary>
		/// The playtime with this unit
		/// </summary>
		public TimeSpan PlayTime;

		/// <summary>
		/// How much damage this unit received?
		/// </summary>
		public BigInteger DamageTaken;
		/// <summary>
		/// How much damage has been dealt with this unit?
		/// </summary>
		public BigInteger DamageDealt;
		
		#endregion
	}
}