# Nox - Nameplate

Displays a nameplate (identity, presence, badges and heartbar) above remote players' heads.

The mod is a **pure service**: it never discovers players by itself. An owner exposes a
**dedicated anchor** transform and instantiates a plate on it, then drives it and disposes it.

## API

```csharp
public interface INameplateAPI {
    UniTask<INameplate> Instantiate(Transform anchor);   // that's all
}
```

Everything else lives on the plate:

```csharp
public interface INameplate : IDisposable {
    Transform  Anchor     { get; }
    GameObject GameObject { get; }
    Identifier Identifier { get; }
    IUser      User       { get; }
    void Set<T>(string key, T value);
    T    Get<T>(string key, T @default = default);
}
```

## Usage

```csharp
using Nox.CCK.Utils;
using Nox.Nameplate;
using Nox.CCK.Nameplate;                    // NameplateExtensions.IsAlive
using Keys = Nox.CCK.Nameplate.Constants;   // alias: `Constants` also exists in Nox.CCK.Utils

var api = CoreAPI.ModAPI.GetMod("nameplate")?.GetInstance<INameplateAPI>();
if (api == null) return; // the mod is optional

// 1. The owner exposes a dedicated anchor (and keeps it up to date).
var anchor = new GameObject("Nameplate Anchor").transform;
anchor.SetParent(myTransform, false);      // hidden with the owner
anchor.position = headPosition + Vector3.up * 0.35f;

// 2. Instantiate the plate on that anchor.
var plate = await api.Instantiate(anchor);
if (!plate.IsAlive()) return;

// 3. Drive it.
plate.Set(Keys.User, user);                // identity, presence, thumbnail
plate.Set(Keys.Status, new[] { "mute", "staff" });   // status icons
plate.Set(Keys.Badges, new[] { "platform", "engine" }); // badge icons
plate.Set(Keys.HeightOffset, 0.1f);        // extra offset above the anchor
plate.Set(Keys.Hearts_Max, 100f);          // setting a heart value/max shows the bar
plate.Set(Keys.Hearts_Value, 75f);
plate.Set(Keys.Hearts_Visible, false);     // hides it
var current = plate.Get(Keys.Hearts_Value, 0f);   // 75

// 4. Release it when the owner goes away.
plate.Dispose();
```

> A destroyed plate is still a non-null managed reference: always use `plate.IsAlive()`
> instead of `plate != null`.

## Keys (`Nox.CCK.Nameplate.Constants`)

| Key | Type | Meaning |
| --- | --- | --- |
| `Visible` | `bool` | visibility of the nameplates — always **local** (client-wide, see below) |
| `HeightOffset` | `float` | vertical offset above the anchor |
| `User` | `IUser` | profile shown by the plate (display name, slug, thumbnail, presence) |
| `Display` | `string` | custom display name overriding the user profile's own `Display` (empty clears it) |
| `Status` | `string`[] | status values rendered as icons (`mute`, `deaf`, `friend`, `staff`, ...) |
| `Badges` | `string`[] | badge values rendered as icons (`group`, `platform`, `engine`, ...) |
| `Hearts_Visible` | `bool` | show/hide the heartbar |
| `Hearts_Value` | `float` | current heartbar value |
| `Hearts_Max` | `float` | heartbar maximum (default 100) |

Values are converted defensively (numbers, strings, ...) since they may come from another mod,
a scripting bridge or a JSON payload; `Get` returns the given default when the stored value
is unset or of another type.

### Visibility

`Visible` is always **local**: it is a local value owned by the plate (client-side, never networked),
but it has no per-plate meaning — the plate that receives it applies the same value to **every**
nameplate of this client. That is how `nox.desktop` / `nox.xr` make nameplates *menu-only*.

```csharp
// controller — what the menu providers do:
var plate = await Client.NameplateAPI.Instantiate(anchor);   // handle: displays nothing
plate.Set(Keys.Visible, menuIsOpen);                        // → every nameplate follows
```

## Plates driven by the runtime

| Owner | Behaviour |
| --- | --- |
| `nox.relay` — one plate per remote player physical | anchor follows the avatar head bone (fallback: `PlayerRig.Head` part, then the base), user fetched from `IUserAPI`, heartbar from the player data (`heart` / `heart.max`) |
| `nox.desktop` / `nox.xr` — one plate per controller | anchor above the local head camera; the plate displays nothing, it only carries the client-wide visibility driven by the menu providers |

## Prefab

`Assets/nameplate/plate.prefab` (world space canvas). Every part is a **public field** assigned on
the prefab (no lookup by name), so the prefab can be re-skinned without touching the code:

| Field | Filled with |
| --- | --- |
| `Head` | header container (hidden while no user is bound) |
| `Body` | body container (hidden when there is neither user nor heartbar) |
| `Display` | user display name |
| `Slug` | `@username` |
| `Thumbnail` | user thumbnail (loaded with `NetworkImage`) |
| `PresenceDot` | coloured from `IUserPresence.Status` |
| `Status` | `IconRow` component on the status container |
| `Badges` | `IconRow` component on the badges container |
| `HeartContainer` / `HeartSlider` / `HeartText` | heartbar |

The `status` and `badges` rows are **`IconRow` components** placed on their container. Each row
instantiates one `icon.prefab` entry per value, filled with the sprite of `icons/<value>.png`
through `CoreAPI.AssetAPI`. The row hides itself when its list is empty, so a status/badge is just a
value and the prefab stays a plain list of icons.

```csharp
plate.Set(Keys.Status, new[] { "mute", "staff" });   // → icons/mute.png, icons/staff.png
```
