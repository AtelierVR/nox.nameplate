using Nox.Nameplate;

namespace Nox.CCK.Nameplate {
	/// <summary>
	/// Object owning a nameplate <b>handle</b>: a controller, a physical, a session...
	/// <para>
	/// It exposes its plate so any other mod can feed it — typically <c>Set(Keys.USER, user)</c> for
	/// an owner that does not know the user itself (a controller's plate, for instance, only carries
	/// the client-wide visibility until a session provides the profile).
	/// </para>
	/// </summary>
	public interface INameplateHolder {
		/// <summary>The plate owned by this object. May be dead: test it with <c>IsAlive()</c> first.</summary>
		INameplate Nameplate { get; }
	}
}
