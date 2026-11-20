using System;
using System.Collections.Generic;
using Nox.CCK.Utils;
using Nox.Nameplate;

namespace Nox.CCK.Nameplate {
	/// <summary>
	/// Helpers for <see cref="INameplate"/> consumers.
	/// </summary>
	public static class NameplateExtensions {
		/// <summary>Namespace the shared UI icons are read from.</summary>
		public const string IconsNamespace = "ui";

		/// <summary>Icon folder of the platform sprites (<c>ui:platforms/&lt;name&gt;.png</c>).</summary>
		public const string PlatformsFolder = "platforms";

		/// <summary>Icon folder of the engine sprites (<c>ui:engines/&lt;name&gt;.png</c>).</summary>
		public const string EnginesFolder = "engines";

		/// <summary>Icon folder of the status sprites (<c>ui:icons/status/&lt;name&gt;.png</c>).</summary>
		public const string StatusFolder = "status";

		/// <summary>Icon folder of the voice-mode sprites (<c>ui:icons/audio/&lt;name&gt;.png</c>).</summary>
		public const string VoiceFolder = "audio";

		/// <summary>
		/// True when the plate is still usable.
		/// <para>
		/// A destroyed plate is still a non-null managed reference (comparing an interface to null
		/// ignores Unity's object lifetime), so the underlying <see cref="UnityEngine.Object"/> must
		/// be checked: always use this instead of <c>plate != null</c> before calling into a plate.
		/// </para>
		/// </summary>
		public static bool IsAlive(this INameplate plate) {
			if (plate == null)
				return false;
			return plate is not UnityEngine.Object o || o != null;
		}

		/// <summary>
		/// Pushes the client badges of a player onto the plate (<see cref="Constants.BADGES"/>): its
		/// platform then its engine, rendered from <c>ui:platforms/&lt;name&gt;.png</c> and
		/// <c>ui:engines/&lt;name&gt;.png</c> (<c>ui:platforms/windows.png</c>,
		/// <c>ui:engines/unity.png</c>, ...). Unknown values are skipped.
		/// </summary>
		public static void SetClientBadges(this INameplate plate, Platform platform, Engine engine) {
			if (!plate.IsAlive())
				return;

			var pn = platform.GetPlatformName();
			var en = engine.GetEngineName();

			var badges = new Dictionary<string, string>();
			if (IsKnown(pn))
				badges[PlatformIcon(pn)] = pn;
			if (IsKnown(en))
				badges[EngineIcon(en)] = en;

			plate.Set(Constants.BADGES, badges);
		}

		/// <summary>Pushes the badges of the local client (its own platform and engine).</summary>
		public static void SetClientBadges(this INameplate plate)
			=> plate.SetClientBadges(PlatformExtensions.CurrentPlatform, EngineExtensions.CurrentEngine);

		/// <summary>Sprite of a platform icon: <c>ui:platforms/&lt;name&gt;.png</c>.</summary>
		public static ResourceIdentifier PlatformIcon(string name)
			=> new(IconsNamespace, $"icons/{PlatformsFolder}/{name}.png");

		/// <summary>Sprite of an engine icon: <c>ui:engines/&lt;name&gt;.png</c>.</summary>
		public static ResourceIdentifier EngineIcon(string name)
			=> new(IconsNamespace, $"icons/{EnginesFolder}/{name}.png");

		/// <summary>Sprite of a status icon: <c>ui:icons/status/&lt;name&gt;.png</c>.</summary>
		public static ResourceIdentifier StatusIcon(string name)
			=> new(IconsNamespace, $"icons/{StatusFolder}/{name}.png");

		/// <summary>
		/// Sprite of a voice-mode icon: <c>ui:icons/audio/&lt;name&gt;.png</c> (the sprites of the voice HUD:
		/// <c>mute</c>, <c>whisper</c>, <c>loud</c>, <c>broadcast</c>).
		/// </summary>
		public static ResourceIdentifier VoiceIcon(string name)
			=> new(IconsNamespace, $"icons/{VoiceFolder}/{name}.png");

		/// <summary>False for an empty or "none" value (platform/engine never announced).</summary>
		private static bool IsKnown(string name)
			=> !string.IsNullOrEmpty(name)
				&& !name.Equals(Platform.None.Name, StringComparison.OrdinalIgnoreCase);
	}
}
