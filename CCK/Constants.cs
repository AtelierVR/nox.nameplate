namespace Nox.CCK.Nameplate {
	/// <summary>
	/// Well-known keys accepted by <c>INameplate.Set/Get</c>.
	/// </summary>
	public static class Constants {
		/// <summary><see cref="bool"/> — visibility, always <b>local</b> (client-wide):
		/// there is no per-plate visibility, a plate that receives it is the client's handle on the
		/// nameplate system and drives every nameplate of this client (the plate itself displays
		/// nothing).</summary>
		public const string VISIBLE = "visible";

		/// <summary><see cref="float"/> — vertical offset (metres) applied above the anchor.</summary>
		public const string HEIGHT_OFFSET = "height_offset";

		/// <summary><c>Nox.Users.IUser</c> — user profile displayed by the plate
		/// (display name, slug, thumbnail, presence).</summary>
		public const string USER = "user";

		/// <summary><see cref="string"/> — display name overriding the <c>User</c> profile's own
		/// <c>Display</c>, so a mod that knows the player can show a custom name (a relay alias, a
		/// localised name, ...). An empty or null value clears the override and falls back to
		/// <c>IUser.Display</c>.</summary>
		public const string DISPLAY = "display";

		/// <summary><see cref="string"/>[] — status values rendered as icons
		/// (<c>mute</c>, <c>deaf</c>, <c>friend</c>, <c>staff</c>, ...), loaded from
		/// <c>icons/&lt;value&gt;.png</c>. An empty list hides the row.</summary>
		public const string STATUS = "status";

		/// <summary><c>Nox.CCK.Utils.ResourceIdentifier</c>[] — badge values rendered as icons
		/// (<c>group</c>, <c>platform</c>, <c>engine</c>, ...). Each value is the full sprite path:
		/// the client platform/engine badges pushed by the clients are
		/// <c>ui:platforms/&lt;name&gt;.png</c> and <c>ui:engines/&lt;name&gt;.png</c>
		/// (<c>ui:platforms/windows.png</c>, <c>ui:engines/unity.png</c>, ...).
		/// An empty list hides the row.</summary>
		public const string BADGES = "badges";

		/// <summary><see cref="bool"/> — show or hide the heartbar.</summary>
		public const string HEARTS_VISIBLE = "hearts.visible";

		/// <summary><see cref="float"/> — current heartbar value.</summary>
		public const string HEARTS_VALUE = "hearts.value";

		/// <summary><see cref="float"/> — heartbar maximum (defaults to 100).</summary>
		public const string HEARTS_MAX = "hearts.max";
	}
}
