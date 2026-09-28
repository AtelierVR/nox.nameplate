using Nox.Entities;

namespace Nox.Nameplate {
	/// <summary>
	/// Entity whose nameplate can be shown or hidden.
	/// <para>
	/// The flag is authoritative data (kept out of the avatar pipeline), so an avatar cannot change it.
	/// </para>
	/// </summary>
	public interface INameplateEntity : IEntity {
		/// <summary>Whether the entity's nameplate is visible.</summary>
		bool NameplateVisible { get; set; }

		/// <summary>Whether the entity's nameplate health bar (heartbar) is shown.</summary>
		bool HealthbarVisible { get; set; }
	}
}
