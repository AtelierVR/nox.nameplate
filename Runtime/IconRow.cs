using System.Collections.Generic;
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
	/// The row owns its <see cref="GameObject"/> (hidden while there is no value) and rebuilds its
	/// entries on every <see cref="Set"/>, so it only needs to sit on the container.
	/// </para>
	/// </summary>
	[DisallowMultipleComponent]
	public class IconRow : MonoBehaviour {
		/// <summary>Entry prefab instantiated for every value.</summary>
		private const string IconPrefab = "icon.prefab";

		/// <summary>Values currently rendered by the row.</summary>
		public Dictionary<string, string> Values { get; private set; } = new Dictionary<string, string>();

		/// <summary>Rebuild the row, or hide it when there is no value.</summary>
		public void Set(Dictionary<string, string> values) {
			Values = values ?? new Dictionary<string, string>();

			Clear();

			gameObject.SetActive(Values.Count > 0);

			if (Values.Count > 0)
				Build(Values).Forget();
		}

		private async UniTask Build(Dictionary<string, string> values) {
			if (Main.CoreAPI == null)
				return;

			var prefab = await Main.CoreAPI.AssetAPI.GetAssetAsync<GameObject>(IconPrefab);
			if (prefab == null || values != Values)
				return;

			var instantiations = new UniTask<GameObject>[values.Count];

            int i;
			for (i = 0; i < values.Count; i++)
				instantiations[i] = prefab.InstantiateAsync(transform);

			var entries = await UniTask.WhenAll(instantiations);

			if (values != Values) {
				foreach (var entry in entries)
					if (entry != null)
						entry.Destroy();
				return;
			}

            i = 0;
			foreach (var value in values)
				Fill(entries[i++], value.Key, value.Value);
		}

		private void Clear() {
			for (var i = transform.childCount - 1; i >= 0; i--)
				transform.GetChild(i).gameObject.Destroy();
		}

		private void Fill(GameObject entry, ResourceIdentifier value, string label) {
			if (entry == null) {
                Logger.LogWarning($"entry is null.", this, nameof(IconRow));
				return;
            }

            entry.name = label;

			var icon = entry.GetComponentInChildren<Image>(true);
			icon.enabled = false;
			if (icon == null) {
                Logger.LogWarning($"icon has no {nameof(Image)}.", entry, nameof(IconRow));
				return;
            }

			if (string.IsNullOrEmpty(value)) {
				icon.enabled = false;
                Logger.LogWarning($"Value is empty.", entry, nameof(IconRow));
				return;
			}

			Load(icon, value).Forget();
		}

		private async UniTask Load(Image icon, ResourceIdentifier value) {
			if (Main.CoreAPI == null)
				return;

			var sprite = await Main.CoreAPI.AssetAPI.GetAssetAsync<Sprite>(value);
			if (icon == null)  {
                Logger.LogWarning($"Icon '{value}' not found.", icon, nameof(IconRow));
                return;
            }

			icon.sprite = sprite;
			icon.enabled = true;
		}
	}
}
