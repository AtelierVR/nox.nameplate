using System;
using Nox.CCK.Utils;
using Nox.Users;
using UnityEngine;

namespace Nox.Nameplate {
	/// <summary>
	/// A single nameplate instance, attached to a dedicated anchor transform
	/// (usually provided by a player physical or a controller).
	/// <para>
	/// The plate is driven by its owner through <see cref="Set{T}"/>/<see cref="Get{T}"/> with the keys
	/// of <c>Nox.CCK.Nameplate.Constants</c>. The plate only handles the rendering and the
	/// billboarding.
	/// </para>
	/// </summary>
	public interface INameplate : IDisposable {
		/// <summary>Anchor followed by the plate (its owner decides where the plate must be).</summary>
		Transform Anchor { get; }

		/// <summary>The instantiated plate root (a world space canvas).</summary>
		GameObject GameObject { get; }

		/// <summary>Identifier of the currently displayed user (<see cref="Identifier.Invalid"/> if none).</summary>
		Identifier Identifier { get; }

		/// <summary>The user currently bound to the plate (may be null while unresolved).</summary>
		IUser User { get; }

		/// <summary>
		/// Set a value of the plate. Use the keys of <c>Nox.CCK.Nameplate.Constants</c>:
		/// <c>User</c>, <c>Visible</c> (always local: it drives every nameplate of this client),
		/// <c>HeightOffset</c>, <c>Status</c> and <c>Badges</c> (value arrays rendered as icons),
		/// <c>Hearts_Visible</c>, <c>Hearts_Value</c>, <c>Hearts_Max</c>.
		/// </summary>
		void Set<T>(string key, T value);

		/// <summary>
		/// Read a value of the plate, or <paramref name="@default"/> when it is unset or of another
		/// type. Use the keys of <c>Nox.CCK.Nameplate.Constants</c>.
		/// </summary>
		T Get<T>(string key, T @default = default);
	}
}
