using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Players;
using Nox.Sessions;
using Nox.Users;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using ResourceIdentifier = Nox.CCK.Utils.ResourceIdentifier;

namespace Nox.Nameplate.Runtime {
	/// <summary>
	/// Mod entry point for <c>nox.nameplate</c>.
	/// <para>
	/// One plate is created per remote player of the current session and positioned above
	/// the player's head (<see cref="PlayerRig.Head"/> part, the same part the relay
	/// synchronizes). Plates are billboarded toward the local camera.
	/// </para>
	/// <para>
	/// The mod fills the plate from the <see cref="IUser"/> pushed by the session through
	/// <see cref="SetUser"/> (an automatic <c>IUserAPI</c> fetch is used as a fallback when
	/// <see cref="AutoFetchUser"/> is enabled).
	/// </para>
	/// </summary>
	public class Main : IMainModInitializer, INameplateAPI {
		/// <summary>Path of the plate prefab inside this mod assets (<c>Assets/nameplate/plate.prefab</c>).</summary>
		public static readonly ResourceIdentifier PlatePrefabPath = "nameplate:plate.prefab";

		private const string Tag = "Nameplate";

		public static Main Instance { get; private set; }

		internal static IMainModCoreAPI CoreAPI { get; private set; }

		private readonly Dictionary<string, Plate> _plates = new();

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		private bool _menuOpen;
		private float _menuHoldUntil;

		// ── INameplateAPI ─────────────────────────────────────────────────────

		public bool Visible { get; set; } = true;

		public NameplateVisibility Visibility { get; set; } = NameplateVisibility.Always;

		public bool AutoFetchUser { get; set; } = true;

		public float HeightOffset { get; set; } = 0.35f;

		/// <summary>
		/// How long (in seconds) a plate stays visible after a menu event.
		/// The UI stack has no "menu closed" event, so this acts as the release timer
		/// for <see cref="NameplateVisibility.MenuOnly"/>.
		/// </summary>
		public float MenuHold { get; set; } = 5f;

		public INameplate[] All
			=> _plates.Values.Cast<INameplate>()
                .ToArray();

		// ── Internal accessors ────────────────────────────────────────────────

		internal GameObject PlatePrefab { get; private set; }

		internal static ISessionAPI SessionAPI
			=> CoreAPI?.ModAPI?.GetMod("session")
                ?.GetInstance<ISessionAPI>();

		internal static IPlayerAPI PlayerAPI
			=> CoreAPI?.ModAPI?.GetMod("players")
                ?.GetInstance<IPlayerAPI>();

		internal static IUserAPI UserAPI
			=> CoreAPI?.ModAPI?.GetMod("users")
                ?.GetInstance<IUserAPI>();

		// ── Lifecycle ─────────────────────────────────────────────────────────

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;

			PlatePrefab = api.AssetAPI.GetAsset<GameObject>(PlatePrefabPath);
			if (PlatePrefab == null)
				Logger.LogError($"Plate prefab '{PlatePrefabPath}' not found in the mod assets.", tag: Tag);

			_events = new[] {
				api.EventAPI.Subscribe("session_current_changed", OnSessionChanged),
				api.EventAPI.Subscribe("session_player_joined", OnPlayerJoined),
				api.EventAPI.Subscribe("session_player_left", OnPlayerLeft),
				api.EventAPI.Subscribe("session_player_visibility_changed", OnPlayerVisibilityChanged),
				api.EventAPI.Subscribe("user_update", OnUserUpdated),
				api.EventAPI.Subscribe("user_fetch", OnUserUpdated),
				api.EventAPI.Subscribe("menu_display", OnMenuEvent),
				api.EventAPI.Subscribe("radial_display", OnMenuEvent),
			};

			Logger.LogDebug("Initialized.", tag: Tag);
		}

		public void OnPostInitializeMain()
			=> Rebuild();

		public void OnLateUpdateMain() {
			foreach (var plate in _plates.Values)
				plate.RefreshVisibility();
		}

		public void OnDisposeMain() {
			foreach (var plate in _plates.Values.ToArray())
				plate.Destroy();
			_plates.Clear();

			foreach (var ev in _events)
				CoreAPI.EventAPI.Unsubscribe(ev);
			_events = Array.Empty<EventSubscription>();

			PlatePrefab = null;
			Instance    = null;
			CoreAPI     = null;
		}

		// ── Events ────────────────────────────────────────────────────────────

		private void OnSessionChanged(EventData context)
			=> Rebuild();

		private void OnPlayerJoined(EventData context) {
			if (!context.TryGet(1, out IPlayer player))
				return;
			if (player.IsLocal)
				return;
			Add(player);
		}

		private void OnPlayerLeft(EventData context) {
			if (!context.TryGet(1, out IPlayer player))
				return;
			Remove(player.Identifier);
		}

		private void OnPlayerVisibilityChanged(EventData context) {
			if (!context.TryGet(1, out IPlayer player))
				return;
			if (player == null || player.IsLocal)
				return;

			// The physical presence drives the plate lifetime through RefreshVisibility.
			if (_plates.TryGetValue(Key(player.Identifier), out var plate))
				plate.RefreshVisibility();
		}

		private void OnUserUpdated(EventData context) {
			if (!context.TryGet(0, out IUser user))
				return;
			SetUser(user.Identifier, user);
		}

		private void OnMenuEvent(EventData context)
			=> _menuHoldUntil = Time.unscaledTime + Mathf.Max(0f, MenuHold);

		// ── Plates management ─────────────────────────────────────────────────

		/// <summary>Rebuild every plate from the players of the current session.</summary>
		private void Rebuild() {
			var sessionAPI = SessionAPI;
			if (sessionAPI == null || !sessionAPI.TryGet(sessionAPI.Current, out var session) || session == null) {
				// No session: drop everything.
				foreach (var key in _plates.Keys.ToArray())
					Remove(key);
				return;
			}

			var alive = new HashSet<string>();
			foreach (var player in session.Entities.GetEntities<IPlayer>()) {
				if (player == null || player.IsLocal)
					continue;
				var key = Key(player.Identifier);
				alive.Add(key);
				Add(player);
			}

			foreach (var key in _plates.Keys.ToArray()) {
				if (!alive.Contains(key))
					Remove(key);
			}
		}

		public void SetMenuOpen(bool open) {
			_menuOpen      = open;
			_menuHoldUntil = open ? Time.unscaledTime + Mathf.Max(0f, MenuHold) : 0f;
		}

		public INameplate Get(Identifier identifier)
			=> identifier.IsValid() && _plates.TryGetValue(Key(identifier), out var plate) ? plate : null;

		public INameplate Get(IPlayer player)
			=> player == null ? null : Get(player.Identifier);

		public INameplate Add(IPlayer player, IUser user = null) {
			if (player == null || player.IsLocal || !player.Identifier.IsValid())
				return null;

			var key = Key(player.Identifier);
			if (_plates.TryGetValue(key, out var existing)) {
				if (user != null)
					existing.SetUser(user);
				return existing;
			}

			if (PlatePrefab == null) {
				Logger.LogError("Plate prefab is missing, cannot create a nameplate.", tag: Tag);
				return null;
			}

			var go = PlatePrefab.Instantiate();
			go.name = $"Nameplate_{player.Identifier.ToShortString()}";

			var plate = go.GetComponent<Plate>();
			if (plate == null)
				plate = go.AddComponent<Plate>();

			plate.Bind(this, player);
			_plates[key] = plate;

			plate.RefreshVisibility();

			if (user != null)
				plate.SetUser(user);
			else if (AutoFetchUser)
				FetchUser(player.Identifier);

			return plate;
		}

		public void SetUser(Identifier identifier, IUser user) {
			if (user == null || !identifier.IsValid())
				return;

			var key = Key(identifier);
			if (_plates.TryGetValue(key, out var plate)) {
				plate.SetUser(user);
				return;
			}

			// The plate may not exist yet: create it if the player is currently visible.
			var sessionAPI = SessionAPI;
			if (sessionAPI == null || !sessionAPI.TryGet(sessionAPI.Current, out var session) || session == null)
				return;

			var player = session.Entities.GetEntities<IPlayer>()
				.FirstOrDefault(p => p != null && !p.IsLocal && Key(p.Identifier) == key);
			if (player != null)
				Add(player, user);
		}

		public void Remove(IPlayer player) {
			if (player != null)
				Remove(player.Identifier);
		}

		public void Remove(Identifier identifier) {
			if (identifier.IsValid())
				Remove(Key(identifier));
		}

		private void Remove(string key) {
			if (!_plates.Remove(key, out var plate))
				return;
			plate.Destroy();
		}

		// ── Helpers ───────────────────────────────────────────────────────────

		internal static string Key(Identifier identifier)
			=> identifier.ToString();

		/// <summary>Whether plates are allowed to render right now (global gate).</summary>
		internal bool IsRenderable() {
			if (!Visible)
				return false;

			return Visibility switch {
				NameplateVisibility.Always   => true,
				NameplateVisibility.MenuOnly => _menuOpen || Time.unscaledTime < _menuHoldUntil,
				_                            => false
			};
		}

		private void FetchUser(Identifier identifier) {
			var api = UserAPI;
			if (api == null)
				return;

			FetchUserAsync(api, identifier).Forget();
		}

		private async UniTaskVoid FetchUserAsync(IUserAPI api, Identifier identifier) {
			try {
				var user = await api.Fetch(identifier);
				if (user != null)
					SetUser(identifier, user);
			} catch (Exception e) {
				Logger.LogDebug($"Failed to fetch user '{identifier.ToString()}': {e.Message}", tag: Tag);
			}
		}
	}
}
