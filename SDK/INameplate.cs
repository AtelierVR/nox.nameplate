using Nox.CCK.Utils;
using Nox.Users;
using UnityEngine;

namespace Nox.Nameplate {
	/// <summary>
	/// A single nameplate bound to a remote player.
	/// The plate is positioned above the player's head (<c>PlayerRig.Head</c> part) and
	/// billboarded toward the local camera.
	/// </summary>
	public interface INameplate {
		/// <summary>Identifier of the user this plate represents.</summary>
		Identifier Identifier { get; }

		/// <summary>The instantiated plate root (a world space canvas).</summary>
		GameObject GameObject { get; }

		/// <summary>The user currently bound to the plate (may be null while unresolved).</summary>
		IUser User { get; }

		/// <summary>
		/// Fill the plate with the given user data (display name, slug, thumbnail,
		/// presence, status, badges). The session is responsible for providing it.
		/// </summary>
		void SetUser(IUser user);

		/// <summary>
		/// Set the heartbar value. Values are stored in the player data
		/// (<c>heart</c> / <c>heart.max</c>) so worlds can drive competitive modes.
		/// </summary>
		void SetHeart(float value, float max = 100f);

		/// <summary>Show or hide this plate (independent of the global visibility mode).</summary>
		void SetVisible(bool visible);
	}
}
