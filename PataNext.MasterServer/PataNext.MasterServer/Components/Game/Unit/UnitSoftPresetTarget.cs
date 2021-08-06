using PataNext.MasterServer.Entities;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.Components.Game.Unit
{
	/// <summary>
	/// A soft preset is a redirection to which preset has been applied.
	/// </summary>
	/// <remarks>
	///	If the user modify an unit, the changes will be done on the <see cref="UnitHardPresetTarget"/>.
	/// </remarks>
	public struct UnitSoftPresetTarget : IEntityComponent
	{
		public DbEntityRepresentation<UnitPresetEntity> Value;

		public UnitSoftPresetTarget(DbEntityRepresentation<UnitPresetEntity> preset) => Value = preset;
	}

	/// <summary>
	/// A hard preset is the original preset of this unit. The target cannot be switched once created.
	/// </summary>
	/// <remarks>
	///	When a new preset is applied, the values will be copied to the hard target; and <see cref="UnitSoftPresetTarget"/> will be modified to that new preset.
	/// </remarks>
	public struct UnitHardPresetTarget : IEntityComponent
	{
		public DbEntityRepresentation<UnitPresetEntity> Value;

		public UnitHardPresetTarget(DbEntityRepresentation<UnitPresetEntity> preset) => Value = preset;
	}
}