namespace Nox.Nameplate {
	/// <summary>
	/// Controls when the nameplates built by this mod are rendered.
	/// The mode is owned by the session (or the controller), so a world can decide
	/// whether plates are always visible, only when a menu is open, or never.
	/// </summary>
	public enum NameplateVisibility {
		/// <summary>Plates are always visible.</summary>
		Always = 0,

		/// <summary>
		/// Plates are only visible while a menu is open.
		/// <see cref="INameplateAPI.SetMenuOpen"/> drives the menu state; the controller
		/// is expected to call it when the radial menu is opened/closed.
		/// </summary>
		MenuOnly = 1,

		/// <summary>Plates are never visible.</summary>
		Never = 2,
	}
}
