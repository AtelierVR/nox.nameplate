using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Nameplate.Runtime.Converters;
using Nox.Scripting;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Nameplate.Runtime {
	/// <summary>
	/// Mod entry point for <c>nox.nameplate</c>.
	/// <para>
	/// The mod is a pure service: it owns the plate prefab and instantiates one plate per
	/// <b>anchor</b> given by a caller. It never discovers players by itself — <c>nox.relay</c>
	/// asks for a plate for every remote player physical, and <c>nox.desktop</c>/<c>nox.xr</c>
	/// ask for one for their controller.
	/// </para>
	/// <para>
	/// Everything else lives on the plate itself (<c>INameplate.Set/Get</c> with the keys of
	/// <c>Nox.CCK.Nameplate.Constants</c>).
	/// </para>
	/// </summary>
	public class Main : IMainModInitializer, INameplateAPI {

		private const string Tag = "Nameplate";

		internal static Main Instance { get; private set; }

		internal static IMainModCoreAPI CoreAPI { get; private set; }

		internal static IScriptingAPI ScriptingAPI
			=> CoreAPI?.ModAPI
				?.GetMod("scripting")
				?.GetInstance<IScriptingAPI>();

		/// <summary>Plates, keyed by anchor instance id.</summary>
		private readonly Dictionary<int, Plate> _plates = new();

		/// <summary>Localization of the plate texts (loaded from the mod bundle).</summary>
		private LanguagePack _lang;

		/// <summary>
		/// Visibility of the nameplates, always <b>local</b>. The value belongs to each plate
		/// (<c>Plate.Visible</c>): the local client's plate is driven by the controller (its menu
		/// provider), the remote players' plates by the scripts.
		/// </summary>
		internal bool Visible
			=> _plates.Values.FirstOrDefault(plate => plate != null)?.Visible ?? true;

		/// <summary>Apply the (always local) visibility to every plate.</summary>
		internal void SetVisible(bool visible) {
			foreach (var plate in _plates.Values.ToArray())
				plate.Visible = visible;
		}

		// ── Lifecycle ─────────────────────────────────────────────────────────

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;

			_lang = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);

			ScriptingAPI?.RegisterConverter(NameplateConverter.Entity);
		}

		public void OnDisposeMain() {
			ScriptingAPI?.UnregisterConverter(NameplateConverter.Entity);

			LanguageManager.RemovePack(_lang);
			_lang = null;

			foreach (var plate in _plates.Values.ToArray())
				plate.gameObject.Destroy();
			_plates.Clear();

			Instance     = null;
			CoreAPI      = null;
		}

		// ── INameplateAPI ─────────────────────────────────────────────────────

		public async UniTask<INameplate> Instantiate(Transform anchor, CancellationToken cancellationToken = default) {
			if (anchor == null)
				return null;

			var key = anchor.GetId();
			if (_plates.TryGetValue(key, out var existing) && existing != null)
				return existing;

			var prefab = await CoreAPI.AssetAPI.GetAssetAsync<GameObject>("plate.prefab", cancellationToken);
			if (prefab == null) {
				Logger.LogError("Plate prefab is missing, cannot create a nameplate.", tag: Tag);
				return null;
			}

			if (_plates.TryGetValue(anchor.GetId(), out existing) && existing != null)
				return existing;

			var plate = await prefab.InstantiateAsync<Plate>(anchor, cancellationToken: cancellationToken);
			var go = plate as MonoBehaviour;
			go.gameObject.name = $"[{nameof(Plate)}] {anchor.GetId()}";

			plate.Bind(this, anchor);
			_plates[anchor.GetId()] = plate;

			return plate;
		}

		// ── Plates management ─────────────────────────────────────────────────

		/// <summary>Destroy the given plate (called by <c>INameplate.Dispose</c>).</summary>
		internal void Release(Plate plate) {
			if (plate == null)
				return;

			var anchor = plate.Anchor;
			if (anchor != null)
				_plates.Remove(anchor.GetId());

			plate.gameObject.Destroy();
		}
	}
}
