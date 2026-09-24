# nox.nameplate

Displays a nameplate above the head of **remote** players.

The plate is the world space canvas `Assets/nameplate/plate.prefab`, positioned on the
`PlayerRig.Head` part (the part the session synchronizes) and billboarded toward the local
camera.

## How it works

* One plate is created per remote player of the current session
  (`session_player_joined`, `session_player_left`, `session_current_changed`).
* The plate is hidden as soon as the player's physical is hidden
  (`session_player_visibility_changed`).
* Every UI part is resolved through `Nox.CCK.Utils.Reference` keys, so the prefab can be
  re-skinned without touching the code:

  | Key                  | Usage                                        |
  | -------------------- | -------------------------------------------- |
  | `head`               | header container (hidden while no user data) |
  | `body`               | body container                               |
  | `display`            | `TMP_Text` — display name                    |
  | `slug`               | `TMP_Text` — `@username`                     |
  | `thumbnail`          | `Image` — user thumbnail (via `NetworkImage`) |
  | `presence.dot`       | `Image` — coloured from the user presence    |
  | `status`             | container — presence text                    |
  | `badges`             | container — one entry per user tag           |
  | `heartbar.container` | container, hidden while `heart` is unset     |
  | `heartbar.slider`    | `Slider` — `heart / heart.max`               |
  | `heartbar.text`      | `TMP_Text` — `value/max`                     |

## Data

The session owns the data: push the resolved user with `SetUser`, the mod only fills the
plate.

```csharp
var nameplates = coreAPI.ModAPI.GetMod("nameplate").GetInstance<INameplateAPI>();

nameplates.SetUser(identifier, user);   // display, slug, thumbnail, presence, badges
```

If a plate has no user yet and `AutoFetchUser` (default `true`) is enabled, the mod falls
back on `IUserAPI.Fetch`.

## Heartbar (competitive modes)

The heartbar is bound to the player data (`IPlayerData`), so a world/session can drive it:

```csharp
// through the API
nameplates.Get(identifier)?.SetHeart(37f, 100f);

// or directly on the player data (same storage, synced by nox.table)
playerData.Set("heart", 37f);
playerData.Set("heart.max", 100f);
```

The heartbar stays hidden while `heart` is not set on the player data.

## Visibility

```csharp
nameplates.Visibility = NameplateVisibility.Always;   // default
nameplates.Visibility = NameplateVisibility.MenuOnly; // only while a menu is open
nameplates.Visibility = NameplateVisibility.Never;

nameplates.Visible = false;      // master switch
nameplates.SetMenuOpen(true);    // driven by the controller
nameplates.HeightOffset = 0.35f; // metres above the head
```

`MenuOnly` is also released automatically `MenuHold` seconds (default `5`) after the last
`menu_display` / `radial_display` event, since the UI stack has no "menu closed" event.

## Build

The package builds through `AtelierVR/mod.builder` (see `.github/workflows/build.yml`),
like the other `nox.*` packages.
