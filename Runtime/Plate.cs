using System;
using System.Collections.Generic;
using Nox.CCK.Network;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Users;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Nameplate.Runtime {
	/// <summary>
	/// A single nameplate instance, bound to a remote player.
	/// <para>
	/// Every UI part is resolved from the prefab through <see cref="Reference"/> keys
	/// (<c>head</c>, <c>body</c>, <c>display</c>, <c>slug</c>, <c>thumbnail</c>,
	/// <c>presence.dot</c>, <c>status</c>, <c>badges</c>, <c>heartbar.container</c>,
	/// <c>heartbar.slider</c>, <c>heartbar.text</c>), so the prefab can be re-skinned
	/// without touching the code.
	/// </para>
	/// <para>
	/// The player data (<c>heart</c> / <c>heart.max</c>) drives the heartbar, letting a
	/// session/world run competitive modes.
	/// </para>
	/// </summary>
	public class Plate : MonoBehaviour, INameplate {
		private const string Tag = "Nameplate";

		// ── Scene references (resolved by Reference keys) ─────────────────────

		private GameObject _head;
		private GameObject _body;
		private TMP_Text _display;
		private TMP_Text _slug;
		private Image _thumbnail;
		private NetworkImage _networkImage;
		private Image _presenceDot;
		private GameObject _status;
		private TMP_Text _statusText;
		private GameObject _badges;
		private GameObject _heartContainer;
		private Slider _heartSlider;
		private TMP_Text _heartText;

		// ── State ─────────────────────────────────────────────────────────────

		private Main _main;
		private IPlayer _player;
		private IPlayerData _data;

		private bool _selfVisible = true;
		private bool _destroyed;

		[SerializeField]
		[Tooltip("Only apply the camera yaw to the plate (keeps the plate horizontal).")]
		private bool _yawOnly = true;

		private readonly List<GameObject> _badgePool = new();
		private GameObject _badgeTemplate;

		// ── INameplate ────────────────────────────────────────────────────────

		public Identifier Identifier
			=> _player?.Identifier ?? Identifier.Invalid;

		public GameObject GameObject
			=> gameObject;

		public IUser User { get; private set; }

		internal void Bind(Main main, IPlayer player) {
			_main   = main;
			_player = player;

			ResolveReferences();
			BindPlayerData();

			if (_head != null)
				_head.SetActive(false);

			RefreshHeart();
		}

		// ── References ────────────────────────────────────────────────────────

		private void ResolveReferences() {
			var root = gameObject;

			_head    = Reference.GetReference("head", root);
			_body    = Reference.GetReference("body", root);
			_display = Reference.GetComponent<TMP_Text>("display", root);
			_slug    = Reference.GetComponent<TMP_Text>("slug", root);
			_thumbnail = Reference.GetComponent<Image>("thumbnail", root);
			_presenceDot = Reference.GetComponent<Image>("presence.dot", root);
			_status  = Reference.GetReference("status", root);
			_badges  = Reference.GetReference("badges", root);
			_heartContainer = Reference.GetReference("heartbar.container", root);
			_heartSlider    = Reference.GetComponent<Slider>("heartbar.slider", root);
			_heartText      = Reference.GetComponent<TMP_Text>("heartbar.text", root);

			if (_status != null)
				_statusText = _status.GetComponentInChildren<TMP_Text>(true);

			if (_display == null) Logger.LogWarning("Reference 'display' not found on the plate prefab.", tag: Tag);
			if (_slug == null) Logger.LogWarning("Reference 'slug' not found on the plate prefab.", tag: Tag);
		}

		// ── Fill ──────────────────────────────────────────────────────────────

		public void SetUser(IUser user) {
			User = user;

			if (_head != null)
				_head.SetActive(user != null);

			// Identity
			if (_display != null)
				_display.text = user?.Display ?? _player?.Display ?? string.Empty;

			if (_slug != null)
				_slug.text = string.IsNullOrEmpty(user?.Username) ? string.Empty : $"@{user.Username}";

			// Thumbnail
			SetThumbnail(user?.Thumbnail);

			// Presence
			var presence = user?.Presence;
			if (_presenceDot != null) {
				_presenceDot.gameObject.SetActive(presence != null);
				if (presence != null)
					_presenceDot.color = StatusColor(presence.Status);
			}

			if (_status != null) {
				var text = presence?.Text;
				var hasStatus = !string.IsNullOrEmpty(text);
				_status.SetActive(hasStatus);
				if (hasStatus && _statusText != null)
					_statusText.text = text;
			}

			// Badges (from the user tags, `<attributor>:<value>`)
			RenderBadges(user?.Tags);

			RefreshBody();
		}

		private void RefreshBody() {
			if (_body == null)
				return;
			var hasHeart = _data != null && _data.Has("heart");
			_body.SetActive(User != null || hasHeart);
		}

		private void SetThumbnail(string url) {
			if (_thumbnail == null)
				return;

			if (string.IsNullOrEmpty(url)) {
				if (_networkImage != null)
					_networkImage.Url = null;
				_thumbnail.enabled = false;
				return;
			}

			_thumbnail.enabled  = true;
			_networkImage       ??= _thumbnail.gameObject.GetOrAddComponent<NetworkImage>();
			_networkImage.Url    = url;
		}

		private void RenderBadges(string[] tags) {
			if (_badges == null)
				return;

			if (tags == null || tags.Length == 0) {
				_badges.SetActive(false);
				return;
			}

			_badges.SetActive(true);

			if (_badgeTemplate == null && _badges.transform.childCount > 0) {
				// The first child of the badges container acts as the badge template and is
				// reused as the first badge (avoids leaking the placeholder entry).
				_badgeTemplate = _badges.transform.GetChild(0).gameObject;
				_badgePool.Add(_badgeTemplate);
			}

			if (_badgeTemplate == null)
				return;

			for (var i = 0; i < tags.Length; i++) {
				GameObject badge;
				if (i < _badgePool.Count) {
					badge = _badgePool[i];
					if (badge == null)
						continue;
				} else {
					badge = _badgeTemplate.Instantiate(_badges.transform);
					_badgePool.Add(badge);
				}

				badge.SetActive(true);
				var label = badge.GetComponentInChildren<TMP_Text>(true);
				if (label != null)
					label.text = CleanTag(tags[i]);
			}

			for (var i = tags.Length; i < _badgePool.Count; i++) {
				if (_badgePool[i] != null)
					_badgePool[i].SetActive(false);
			}
		}

		private static string CleanTag(string tag) {
			if (string.IsNullOrEmpty(tag))
				return string.Empty;
			var idx = tag.IndexOf(':');
			return idx >= 0 && idx < tag.Length - 1 ? tag[(idx + 1)..] : tag;
		}

		private static Color StatusColor(UserStatus status)
			=> status switch {
				UserStatus.OFFLINE                            => new Color(0.55f, 0.55f, 0.55f),
				UserStatus.BUSY                               => new Color(0.95f, 0.35f, 0.30f),
				UserStatus.DO_NOT_DISTURB                     => new Color(0.90f, 0.25f, 0.25f),
				UserStatus.STREAM                             => new Color(0.70f, 0.35f, 0.95f),
				UserStatus.EVENTS                             => new Color(1.00f, 0.75f, 0.25f),
				UserStatus.PUBLIC or UserStatus.ONLINE_JOIN   => new Color(0.35f, 0.90f, 0.45f),
				_                                             => new Color(0.35f, 0.80f, 0.45f),
			};

		// ── Heartbar ──────────────────────────────────────────────────────────

		public void SetHeart(float value, float max = 100f) {
			if (_data == null)
				return;

			_data.Set("heart", value);
			_data.Set("heart.max", max);
			RefreshHeart();
		}

		private void BindPlayerData() {
			var api = Main.PlayerAPI;
			if (api == null || _player == null || !_player.Identifier.IsValid())
				return;

			try {
				_data = api.Get(_player.Identifier);
				_data.OnChanged.AddListener(OnDataChanged);
			} catch (Exception e) {
				Logger.LogWarning($"Failed to bind player data for '{_player.Identifier.ToString()}': {e.Message}", tag: Tag);
			}
		}

		private void OnDataChanged(string[] key, object @new, object old)
			=> RefreshHeart();

		private void RefreshHeart() {
			if (_heartContainer == null)
				return;

			if (_data == null || !_data.Has("heart")) {
				_heartContainer.SetActive(false);
				RefreshBody();
				return;
			}

			var max   = Mathf.Max(_data.Get("heart.max", 100f), 0.0001f);
			var value = Mathf.Clamp(_data.Get("heart", 0f), 0f, max);

			_heartContainer.SetActive(true);
			if (_heartSlider != null)
				_heartSlider.value = Mathf.Clamp01(value / max);
			if (_heartText != null)
				_heartText.text = $"{Mathf.RoundToInt(value)}/{Mathf.RoundToInt(max)}";

			RefreshBody();
		}

		// ── Transform & visibility ────────────────────────────────────────────

		public void SetVisible(bool visible) {
			_selfVisible = visible;
			RefreshVisibility();
		}

		internal void RefreshVisibility() {
			if (_destroyed)
				return;

			var show = _selfVisible
				&& _main != null
				&& _main.IsRenderable()
				&& HasVisiblePhysical();

			if (gameObject.activeSelf != show)
				gameObject.SetActive(show);
		}

		/// <summary>
		/// A remote player's physical is hidden (not destroyed) for a while before being
		/// actually released: checking <c>activeInHierarchy</c> hides the plate immediately
		/// instead of leaving it floating at the last known position.
		/// </summary>
		private bool HasVisiblePhysical()
			=> _player != null
				&& _player.TryGetPhysical<Physical>(out var physical)
				&& physical != null
				&& physical.gameObject != null
				&& physical.gameObject.activeInHierarchy;

		private void LateUpdate()
			=> UpdateTransform();

		private void UpdateTransform() {
			if (_player == null)
				return;

			var anchor = _player.Position;
			if (_player.TryGetPart(PlayerRig.Head.ToIndex(), out IPart head)
				&& head != null
				&& head.Updated != default)
				anchor = head.Position;

			transform.position = anchor + Vector3.up * (_main?.HeightOffset ?? 0.35f);

			var cam = Camera.main;
			if (cam == null)
				return;

			var rotation = cam.transform.rotation;
			if (_yawOnly) {
				var forward = rotation * Vector3.forward;
				forward.y = 0f;
				if (forward.sqrMagnitude > 0.0001f)
					rotation = Quaternion.LookRotation(forward, Vector3.up);
			}

			transform.rotation = rotation;
		}

		// ── Lifetime ──────────────────────────────────────────────────────────

		internal void Destroy() {
			if (_destroyed)
				return;
			_destroyed = true;
			gameObject.Destroy();
		}

		private void OnDestroy() {
			_destroyed = true;

			if (_data != null) {
				_data.OnChanged.RemoveListener(OnDataChanged);
				_data.Dispose();
				_data = null;
			}

			_networkImage = null;
			User          = null;
			_player       = null;
			_main         = null;
		}
	}
}
