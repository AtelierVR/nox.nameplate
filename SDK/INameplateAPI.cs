using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.Nameplate {
	/// <summary>
	/// Public API of the <c>nox.nameplate</c> mod.
	/// Exposed as the mod instance: <c>GetMod("nameplate")?.GetInstance&lt;INameplateAPI&gt;()</c>.
	/// <para>
	/// The mod is purely demand driven: it never discovers players by itself. An owner
	/// (a player physical, a controller, a session...) exposes a <b>dedicated anchor</b>
	/// transform and instantiates a plate on it, then drives it through
	/// <c>INameplate.Set/Get</c> and disposes it when done.
	/// </para>
	/// </summary>
	public interface INameplateAPI {
		/// <summary>
		/// Create the plate bound to the given anchor (or return the existing one).
		/// The plate is parented under the anchor (it follows the anchor and is destroyed with it)
		/// and is disposed when no longer needed.
		/// </summary>
		/// <param name="anchor">Dedicated anchor transform owned by the caller.</param>
		/// <param name="cancellationToken">Owns the creation: cancelling it drops the instantiation.</param>
		UniTask<INameplate> Instantiate(Transform anchor, CancellationToken cancellationToken = default);
	}
}
