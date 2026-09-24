using Nox.CCK.Utils;
using Nox.Players;
using Nox.Users;

namespace Nox.Nameplate {
	/// <summary>
	/// Public API of the <c>nox.nameplate</c> mod.
	/// Exposed as the mod instance: <c>GetMod("nameplate").GetInstance&lt;INameplateAPI&gt;()</c>.
	/// <para>
	/// The mod creates one plate per remote player automatically when a session is current.
	/// The session is expected to push the matching <see cref="IUser"/> through
	/// <see cref="SetUser"/> (or rely on the automatic fetch, see <see cref="AutoFetchUser"/>).
	/// </para>
	/// </summary>
	public interface INameplateAPI {
		/// <summary>Master switch. When false, no plate is rendered whatever the mode is.</summary>
		bool Visible { get; set; }

		/// <summary>When the plates are rendered (always, only with a menu open, never).</summary>
		NameplateVisibility Visibility { get; set; }

		/// <summary>
		/// When true (default), the mod fetches the user profile itself through
		/// <c>IUserAPI</c> for plates that received no explicit <see cref="SetUser"/>.
		/// Set it to false when the session is the only source of user data.
		/// </summary>
		bool AutoFetchUser { get; set; }

		/// <summary>Vertical offset (in metres) applied above the player's head.</summary>
		float HeightOffset { get; set; }

		/// <summary>
		/// Menu state used by <see cref="NameplateVisibility.MenuOnly"/>.
		/// The controller should call it when the menu opens/closes.
		/// </summary>
		void SetMenuOpen(bool open);

		/// <summary>
		/// Fill (or refresh) the plate of the given user.
		/// Creates the plate if the player is currently visible and has no plate yet.
		/// </summary>
		void SetUser(Identifier identifier, IUser user);

		/// <summary>Get the plate bound to the given identifier, or null.</summary>
		INameplate Get(Identifier identifier);

		/// <summary>Get the plate bound to the given player, or null.</summary>
		INameplate Get(IPlayer player);

		/// <summary>Create (or return) the plate of the given player. Usually called by the mod itself.</summary>
		INameplate Add(IPlayer player, IUser user = null);

		/// <summary>Destroy the plate of the given player.</summary>
		void Remove(IPlayer player);

		/// <summary>Destroy the plate bound to the given identifier.</summary>
		void Remove(Identifier identifier);

		/// <summary>All plates currently alive.</summary>
		INameplate[] All { get; }
	}
}
