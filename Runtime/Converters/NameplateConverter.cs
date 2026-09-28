using Nox.CCK.Scripting;
using Nox.Scripting;

namespace Nox.Nameplate.Runtime.Converters {
	/// <summary>
	/// Type converter for <see cref="INameplateEntity"/>.
	/// <para>
	/// Lives here (and not in <c>nox.sessions</c>) because <see cref="INameplateEntity"/> belongs to
	/// this package: any value that implements it also gets the bindings of its other compatible
	/// converters (e.g. the <c>IPlayer</c> one), the registry merging them by name.
	/// </para>
	/// </summary>
	public static class NameplateConverter {
		private static bool Bool(IScriptingContext ctx, object value)
			=> (bool)ctx.FromScript(value, typeof(bool));

		public static readonly IScriptingTypeConverter Entity =
			ScriptingTypeConverterBuilder<INameplateEntity>.Create()
				.AddProperty("nameplateVisible",
					getter: (ctx, e) => (object)e.NameplateVisible,
					setter: (ctx, e, val) => e.NameplateVisible = Bool(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("healthbarVisible",
					getter: (ctx, e) => (object)e.HealthbarVisible,
					setter: (ctx, e, val) => e.HealthbarVisible = Bool(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.SetDefault((INameplateEntity)null)
				.Build();
	}
}
