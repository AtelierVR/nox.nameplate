using System.Collections.Generic;
using Nox.CCK;
using Nox.CCK.Language;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using Nox.Users;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;
using Keys = Nox.CCK.Nameplate.Constants;

namespace Nox.Nameplate.Runtime {
	public class Plate : MonoBehaviour, INameplate {
		private const string Tag = "Nameplate";

		// ── Scene references ──────────────────────────────────────────────────
		public GameObject Head;
		public GameObject Body;
		public TextLanguage Display;
		public TextLanguage Slug;
		public Image Thumbnail;
		public Image PresenceDot;
		public Image Voice;
		public IconRow Status;
		public IconRow Badges;
		public GameObject HeartContainer;
		public Slider HeartSlider;
		public TextLanguage HeartText;

		// ── State ─────────────────────────────────────────────────────────────
		private Main _main;
		private Transform _anchor;

		private bool  _visible = true;
		private bool  _destroyed;
		private float _heightOffset;
		private bool  _heartVisible;
		private float _heartValue;
		private float _heartMax = 100f;
		private string _display;

		/// <summary>Colour of the display name and of the voice image (null = white).</summary>
		private Color? _color;

		/// <summary>Voice level (0-1), used as the alpha of the voice image.</summary>
		private float _voice;

		[SerializeField]
		[Tooltip("Only apply the camera yaw to the plate (keeps the plate horizontal).")]
		private bool _yawOnly = true;

		[SerializeField]
		[Tooltip("Invert direction if the UI prefab faces backward (towards -Z).")]
		private bool _reverseFacing = false;

		// ── INameplate ────────────────────────────────────────────────────────
		public Transform Anchor => _anchor;
		public GameObject GameObject => gameObject;
		public Identifier Identifier { get; private set; } = Identifier.Invalid;
		public IUser User { get; private set; }

		internal bool Visible {
			get => _visible;
			set {
				if (_visible == value) return;
				_visible = value;
				RefreshVisibility();
			}
		}

		internal void Bind(Main main, Transform anchor) {
			_main   = main;
			_anchor = anchor;
			_visible = true;

			if (Head != null)
				Head.SetActive(false);

			// No voice level pushed yet: the voice image starts hidden (alpha 0).
			RefreshVoice();
			RefreshHeart();
			RefreshVisibility();
		}

		// ── Values & Language ─────────────────────────────────────────────────
		public void Set<T>(string key, T value) {
			switch (key) {
				case Keys.VISIBLE: 
					Visible = value.ToBool(); 
					break;
				
				case Keys.HEIGHT_OFFSET: 
					_heightOffset = value.ToFloat(); 
					break;

				case Keys.USER: 
					SetUser(value is IUser user ? user : null); 
					break;

				case Keys.DISPLAY: 
					SetDisplay(value as string); 
					break;
					
				case Keys.STATUS when value is Dictionary<string, string> v0: 
					Status.Set(v0); 
					break;

				case Keys.BADGES when value is Dictionary<string, string> v1: 
					Badges.Set(v1); 
					break;

				case Keys.VOICE:
					_voice = Mathf.Clamp01(value.ToFloat());
					RefreshVoice();
					break;

				case Keys.COLOR:
					_color = ParseColor(value);
					RefreshVoice();
					RefreshDisplay();
					break;

				case Keys.HEARTS_VISIBLE: 
					_heartVisible = value.ToBool(); RefreshHeart(); 
					break;

				case Keys.HEARTS_VALUE: 
					_heartValue = value.ToFloat(); 
					_heartVisible = true; 
					RefreshHeart(); 
					break;

				case Keys.HEARTS_MAX: 
					_heartMax = Mathf.Max(value.ToFloat(), 0.0001f); 
					_heartVisible = true; 
					RefreshHeart(); 
					break;

				default: 
					Logger.LogWarning($"Unknown nameplate key '{key}'.", tag: Tag); 
					break;
			}
		}

		public T Get<T>(string key, T @default = default) {
			var value = key switch {
				Keys.VISIBLE		=> (object) _visible,
				Keys.HEIGHT_OFFSET  => _heightOffset,
				Keys.USER		   => User,
				Keys.DISPLAY		=> _display,
				Keys.STATUS		 => Status.Values,
				Keys.BADGES		 => Badges.Values,
				Keys.VOICE          => _voice,
				Keys.COLOR          => _color,
				Keys.HEARTS_VISIBLE => _heartVisible,
				Keys.HEARTS_VALUE   => _heartValue,
				Keys.HEARTS_MAX	 => _heartMax,
				_				   => null
			};
			return value == null ? @default : value.To<T>();
		}

		private const string LangDisplay = "nameplate.display";
		private const string LangSlug	= "nameplate.slug";
		private const string LangHeart   = "nameplate.heart";

		private static void SetLanguage(TextLanguage text, string key, params string[] values) {
			if (text != null) text.UpdateText(key, values);
		}

		/// <summary>
		/// Sets (or clears, with an empty value) a custom display name overriding the <c>User</c>
		/// profile's own <c>Display</c> (<c>Keys.DISPLAY</c>).
		/// </summary>
		private void SetDisplay(string display) {
			_display = string.IsNullOrEmpty(display) ? null : display;
			RefreshHead();
			RefreshDisplay();
			RefreshLayout();
		}

		private void SetUser(IUser user) {
			User	   = user;
			Identifier = user?.Identifier ?? Identifier.Invalid;

			RefreshHead();
			RefreshDisplay();
			SetLanguage(Slug, LangSlug, string.IsNullOrEmpty(user?.Username) ? string.Empty : $"@{user.Username}");
			SetThumbnail(user?.Thumbnail);

			var presence = user?.Presence;
			if (PresenceDot != null) {
				PresenceDot.gameObject.SetActive(presence != null);
				if (presence != null) PresenceDot.color = presence.Status.ToColor();
			}

			RefreshBody();
			RefreshLayout();
		}

		private void SetThumbnail(string url) {
			if (Thumbnail == null) return;
			var net = Thumbnail.gameObject.GetOrAddComponent<NetworkImage>();
			if (string.IsNullOrEmpty(url)) {
				net.Url = null;
				Thumbnail.enabled = false;
				return;
			}
			Thumbnail.enabled = true;
			net.Url = url;
		}

		private void RefreshHeart() {
			if (HeartContainer != null) HeartContainer.SetActive(_heartVisible);
			if (HeartSlider != null) HeartSlider.value = Mathf.Clamp01(Mathf.Clamp(_heartValue, 0f, _heartMax) / _heartMax);
			SetLanguage(HeartText, LangHeart, Mathf.RoundToInt(_heartValue).ToString(), Mathf.RoundToInt(_heartMax).ToString());
			RefreshBody();
			RefreshLayout();
		}

		/// <summary>Anything to identify: a user profile, or only a custom display name.</summary>
		private bool HasIdentity
			=> User != null || !string.IsNullOrEmpty(_display);

		private void RefreshHead() {
			if (Head != null) Head.SetActive(HasIdentity);
		}

		/// <summary>
		/// Display text: the custom display name when set, else the profile's own one, wrapped in a
		/// RichText colour tag when a colour is set (<see cref="Keys.COLOR"/>).
		/// </summary>
		private void RefreshDisplay()
			=> SetLanguage(Display, LangDisplay, Colorize(_display ?? User?.Display ?? string.Empty));

		/// <summary>
		/// Applies the colour (<see cref="Keys.COLOR"/>, white by default) and the voice level
		/// (<see cref="Keys.VOICE"/>, used as alpha) to the voice image.
		/// </summary>
		private void RefreshVoice() {
			if (Voice == null) return;

			var color = _color ?? Color.white;
			color.a = _voice;
			Voice.color = color;
		}

		/// <summary>
		/// Wraps the text in a RichText <c>&lt;color&gt;</c> tag when a colour is set
		/// (<see cref="Keys.COLOR"/>), so the display component keeps its own material and font.
		/// </summary>
		private string Colorize(string text)
			=> _color is { } color && !string.IsNullOrEmpty(text)
				? $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>"
				: text;

		/// <summary>
		/// Reads a colour pushed through <see cref="Keys.COLOR"/>: a <c>UnityEngine.Color</c> or an
		/// HTML string (<c>#RRGGBB</c>, <c>#RRGGBBAA</c>). Anything else (including <c>null</c>)
		/// clears it back to the default (white).
		/// </summary>
		private static Color? ParseColor(object value)
			=> value switch {
				Color color => color,
				string html => ParseHtmlColor(html),
				_           => null
			};

		private static Color? ParseHtmlColor(string html)
			=> !string.IsNullOrEmpty(html) && ColorUtility.TryParseHtmlString(html, out var parsed)
				? parsed
				: (Color?)null;

		private void RefreshBody() {
			if (Body != null) Body.SetActive(HasIdentity || _heartVisible);
		}

		private void RefreshLayout() => UpdateLayout.UpdateImmediate(gameObject);

		internal void RefreshVisibility() {
			if (_destroyed) return;
			var show = _visible && _anchor != null && _anchor.gameObject.activeInHierarchy;
			if (gameObject.activeSelf != show) {
				gameObject.SetActive(show);
				if (show) RefreshLayout();
			}
		}

		private void LateUpdate() => UpdateTransform();

		private void UpdateTransform() {
			if (_anchor == null) return;
			transform.position = _anchor.position + Vector3.up * _heightOffset;

			var cam = Camera.main;
			if (cam != null) FaceCamera(cam);
		}

		// ── Camera facing ─────────────────────────────────────────────────────

		private void OnEnable() {
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
			Camera.onPreCull += OnPreCullCamera; // Support pour le Built-in Pipeline
		}

		private void OnDisable() {
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
			Camera.onPreCull -= OnPreCullCamera;
		}

		private void OnPreCullCamera(Camera cam) {
			// Ignorer si URP/HDRP est actif (déjà géré par OnBeginCameraRendering)
			if (GraphicsSettings.currentRenderPipeline != null) return;
			ProcessCameraFacing(cam);
		}

		private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam) {
			ProcessCameraFacing(cam);
		}

		private void ProcessCameraFacing(Camera cam) {
			if (_destroyed || _anchor == null || cam == null) return;

			// Filtrer les caméras d'aperçu / de réflexion / inutiles
			if (cam.cameraType == CameraType.Preview) return;

			FaceCamera(cam);
		}

		private void FaceCamera(Camera cam) {
			Vector3 forward = cam.transform.forward;

			if (_yawOnly) {
				forward.y = 0f;
				if (forward.sqrMagnitude < 0.0001f) return;
				forward.Normalize();
			}

			if (_reverseFacing) {
				forward = -forward;
			}

			transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
		}

		// ── Lifetime ──────────────────────────────────────────────────────────

		public void Dispose() {
			if (_main != null) _main.Release(this);
			else Destroy();
		}

		internal void Destroy() {
			if (_destroyed) return;
			_destroyed = true;
			gameObject.Destroy();
		}

		private void OnDestroy() {
			_destroyed = true;
			User = null;
			Identifier = Identifier.Invalid;
			_anchor = null;
			_main = null;
		}
	}
}