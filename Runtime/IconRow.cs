using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Nameplate.Runtime {
	/// <summary>
	/// A row of icons placed on a container: one entry per value, each an <c>icon.prefab</c>
	/// instance filled with the sprite of the given <see cref="ResourceIdentifier"/> (e.g.
	/// <c>ui:platforms/windows.png</c>, <c>ui:engines/unity.png</c>, <c>ui:status/mute.png</c>, ...).
	/// <para>
	/// The row owns its <see cref="GameObject"/> (hidden while there is no value). <see cref="Set"/> is
	/// incremental: an entry already rendered for a value is kept as-is, so pushing a row whose
	/// values only grew or shrank never reloads the icons that did not move.
	/// </para>
	/// </summary>
	[DisallowMultipleComponent]
	public class IconRow : MonoBehaviour {
		/// <summary>Entry prefab instantiated for every value.</summary>
		private const string IconPrefab = "icon.prefab";

		/// <summary>Values currently rendered by the row.</summary>
		public Dictionary<string, string> Values { get; private set; } = new Dictionary<string, string>();

		/// <summary>Live entry of every rendered value, keyed like <see cref="Values"/>.</summary>
		private readonly Dictionary<string, GameObject> _entries = new();

		/// <summary>
		/// Cancels the instantiations started for a previous state: a value that left (or a row that was
		/// wiped) must not see its entry appear afterwards, so the work is cancelled instead of checked.
		/// </summary>
		private CancellationTokenSource _creations = new();

		/// <summary>True while <see cref="Sync"/> runs: its own activation must not wipe the row.</summary>
		private bool _syncing;

		/// <summary>Renders <paramref name="values"/>, adding, removing and reordering only what changed.</summary>
		public void Set(Dictionary<string, string> values) {
			values ??= new Dictionary<string, string>();

			// Owners push their row every frame: an identical row is left untouched.
			if (Same(values, Values))
				return;

			Values = values;
			Sync();
		}

		/// <summary>
		/// A row that comes back up (pooled plate, plate hidden then shown, prefab re-enabled) may still
		/// hold the entries of its previous life: the container is cleared, then rebuilt from the values
		/// the row already holds (an owner may push them once only, e.g. the platform/engine badges).
		/// </summary>
		private void OnEnable() {
			// The row was enabled by its own Sync (it went from empty to filled): the entries being
			// created there are the ones it must show, wiping them would restart every icon.
			if (_syncing)
				return;

			for (var i = transform.childCount - 1; i >= 0; i--) {
				var child = transform.GetChild(i).gameObject;

				// Hidden right away: Destroy() is deferred, and stale icons must not show for a frame.
				child.SetActive(false);
				child.Destroy();
			}

			_entries.Clear();

			if (Values.Count > 0)
				Sync();
		}

		private void OnDestroy() {
			_creations.Cancel();
			_creations.Dispose();
		}

		/// <summary>True when both rows render the same values (same keys, same labels).</summary>
		private static bool Same(Dictionary<string, string> incoming, Dictionary<string, string> current) {
			if (ReferenceEquals(incoming, current))
				return true;

			var count = incoming?.Count ?? 0;
			if (count != (current?.Count ?? 0))
				return false;

			if (count == 0)
				return true;

			foreach (var (key, label) in incoming)
				if (!current.TryGetValue(key, out var other) || other != label)
					return false;

			return true;
		}

		/// <summary>
		/// Aligns the rendered entries on <see cref="Values"/>: the dropped ones are destroyed, the
		/// missing ones instantiated, the kept ones are not touched (their sprite stays loaded).
		/// </summary>
		private void Sync() {
			if (_syncing)
				return;

			_syncing = true;

			// Whatever was being instantiated for the previous values is now obsolete.
			var token = RenewCreations();

			try {
				foreach (var key in _entries.Keys.ToArray()) {
					if (Values.ContainsKey(key))
						continue;

					_entries[key].Destroy();
					_entries.Remove(key);
				}

				foreach (var (key, label) in Values) {
					if (_entries.ContainsKey(key))
						continue;

					Create(key, label, token).Forget();
				}

				gameObject.SetActive(Values.Count > 0);
				ApplyOrder();
			} finally {
				_syncing = false;
			}
		}

		/// <summary>Replaces the creation token, cancelling the instantiations of the previous state.</summary>
		private CancellationToken RenewCreations() {
			_creations.Cancel();
			_creations.Dispose();
			_creations = new CancellationTokenSource();
			return _creations.Token;
		}

		/// <summary>
		/// Instantiates and fills the entry of one value. Returns silently when the row changed while the
		/// instantiation was in flight: the token is cancelled, and so are the awaits above.
		/// </summary>
		private async UniTaskVoid Create(string key, string label, CancellationToken token) {
			try {
				var api = Main.CoreAPI;
				if (api == null)
					return;

				var prefab = await api.AssetAPI.GetAssetAsync<GameObject>(IconPrefab, token);

				// The value left while the prefab was loading.
				if (prefab == null || !Values.ContainsKey(key))
					return;

				// Not cancellable: an interrupted instantiation would leave an untracked child behind.
				var entry = await prefab.InstantiateAsync(transform);

				if (entry == null)
					return;

				// The row moved on while instantiating: this instance is a leftover.
				if (token.IsCancellationRequested || !Values.ContainsKey(key) || _entries.ContainsKey(key)) {
					entry.Destroy();
					return;
				}

				_entries[key] = entry;

				Fill(entry, key, label);
				ApplyOrder();
			} catch (OperationCanceledException) {
				// The row changed before the entry was ready: nothing to render, nothing to clean up.
			}
		}

		/// <summary>Keeps the rendered icons in the order the values are pushed with.</summary>
		private void ApplyOrder() {
			var index = 0;
			foreach (var key in Values.Keys) {
				if (!_entries.TryGetValue(key, out var entry) || entry == null)
					continue;

				if (entry.transform.GetSiblingIndex() != index)
					entry.transform.SetSiblingIndex(index);
				index++;
			}
		}

		private void Fill(GameObject entry, ResourceIdentifier value, string label) {
			if (entry == null) {
				Logger.LogWarning("entry is null.", this, nameof(IconRow));
				return;
			}

			entry.name = label;

			var icon = entry.GetComponentInChildren<Image>(true);
			if (icon == null) {
				Logger.LogWarning($"icon has no {nameof(Image)}.", entry, nameof(IconRow));
				return;
			}

			icon.enabled = false;

			if (string.IsNullOrEmpty(value)) {
				Logger.LogWarning("Value is empty.", entry, nameof(IconRow));
				return;
			}

			Load(icon, value).Forget();
		}

		private async UniTask Load(Image icon, ResourceIdentifier value) {
			if (Main.CoreAPI == null)
				return;

			var sprite = await Main.CoreAPI.AssetAPI.GetAssetAsync<Sprite>(value);

			// The entry may have been removed (or the whole row wiped) while the sprite loaded: a
			// cancelled load is not a missing icon.
			if (icon == null)
				return;

			if (sprite == null) {
				Logger.LogWarning($"Icon '{value}' not found.", icon, nameof(IconRow));
				return;
			}

			icon.sprite = sprite;
			icon.enabled = true;
		}
	}
}
