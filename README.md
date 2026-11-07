# Teja's Impact

**v0.7 — Aetherfall**, a free, noncommercial action-RPG prototype built with Unity 2022.3.20f1. Explore a bounded island, complete quests, earn upgrades and face the Rift Warden as Teja.

## Play or download

- [Game website and browser edition](https://tejapriyan.github.io/Teja-Imapct/) — published by the repository's GitHub Pages workflow; use a laptop or desktop for browser play.
- [Windows x64 ZIP](https://github.com/TejaPriyan/Teja-Imapct/releases/download/v0.7.0/Tejas-Impact-Windows-v0.7.zip) — extract the entire ZIP, then open `TejasImpact.exe`. Includes solo play and two-player internet rooms.
- [Android APK v0.7.1](https://github.com/TejaPriyan/Teja-Imapct/releases/download/v0.7.1/Tejas-Impact-Android-v0.7.1.apk) — Android 8.0+, ARM64, OpenGL ES 3 and Android API 36 target. Review the phone's installation prompt, launch and turn the phone horizontally. Solo play; directly distributed test build, not a Play Store release.
- [Complete Unity source ZIP](https://github.com/TejaPriyan/Teja-Imapct/releases/download/v0.7.0/Tejas-Impact-Unity-Source-v0.7.zip) — includes the pinned Windows networking helper and retained licenses.
- [Latest Android compatibility release](https://github.com/TejaPriyan/Teja-Imapct/releases/tag/v0.7.1) and [v0.7 release notes/source files](https://github.com/TejaPriyan/Teja-Imapct/releases/tag/v0.7.0).

The website in [`website/`](website/) includes the desktop browser game, requirements, controls, installation help and credits. Browser play requires a laptop/desktop browser with WebGL 2, WebAssembly and hardware acceleration. Run it over HTTP/HTTPS, not by opening `index.html` directly from disk. Browser and Android editions are solo. Each edition keeps separate local saves; there are no accounts or cloud saves.

**No iPhone/iPad app is included.** An installable iOS build still requires a Mac, Xcode, Apple signing and device testing.

## What's included

- One Aetherfall island with eleven regions, six chests, three NPC quests, a grotto puzzle and waystone checkpoints.
- Frostglass Sword, Dawn Sidearm and Aurora Rifle; coins, an in-game armory, weapon/character upgrades, ammunition, healing and target lock.
- 47 enemies including a phased boss, original synthesized music and sound effects, and local save/backup recovery.
- Landscape touch controls with independent movement, camera and attack gestures, safe-area layout, enlarged pause/settings buttons and a portrait rotation prompt.
- Two-player Windows rooms with invitation codes and shared enemies, rewards, quests and victory. The host's PC and game must remain running. Rooms use a temporary Cloudflare relay; there is no uptime guarantee.

This is a prototype, not a finished AAA or streamed open-world game. The supplied character is an Eula skin named Teja in-game; it is not an original character asset or an official HoYoverse product. See [asset attribution](game/ATTRIBUTION.md) and [license notices](LICENSE-NOTICE.md).

## Controls

| Action | Keyboard / mouse |
| --- | --- |
| Move / sprint | WASD or arrows / Left Ctrl |
| Look / zoom / recenter | Mouse / wheel / T |
| Attack or fire | Left click or J; hold to repeat |
| Aim / target lock | Right click or V / Tab |
| Jump / dodge / Frost Nova | Space / Left Shift / E |
| Interact / reload / heal | F / R / H |
| Owned weapons | 1 / 2 / 3 |
| Journal / upgrades / loadout / map | Q / C / I / M |
| Shop / pause / HUD cursor | B near trader / Escape / hold Alt |

On Android, use the left stick to move and drag empty space on the right to look. Hold Strike/Fire and use the dedicated action buttons. Tap Menu, Map, Loadout, the health card or the quest card for the corresponding panels. Play horizontally.

## Run the website locally

From the repository root, with Node.js installed:

```text
node scripts/serve-website.cjs
```

Open **http://127.0.0.1:4177/**. Stop the server with Ctrl+C. Alternatively, with Python 3 installed:

```text
python -m http.server 4177 --bind 127.0.0.1 --directory website
```

Neither option requires third-party packages. The website downloads released builds from GitHub and contains the complete WebGL player under `website/play/`.

## Open and build the Unity project

1. Install **Unity 2022.3.20f1** through Unity Hub and add the [`game/`](game/) folder as a project.
2. For Windows co-op source builds, run `scripts/fetch-network-helper.ps1` from PowerShell. It downloads the pinned official Cloudflare helper and verifies its SHA-256 before placing it in the project. The helper executable is excluded from Git; its Apache 2.0 license is retained. The complete release source ZIP already contains it.
3. Open `Assets/Scenes/Aetherfall.unity`. The world is generated at runtime; Unity recreates its `Library` cache.
4. Use the **Teja's Impact** editor menu or the methods below. Install the corresponding Unity build modules first.

| Edition | Editor method | Additional build support |
| --- | --- | --- |
| Windows x64 | `TejaBuilder.BuildWindows` | Windows build support |
| Browser | `TejaWebBuilder.BuildWebGL` | WebGL Build Support |
| Android ARM64 | `TejaMobileBuilder.BuildAndroid` | Android Build Support, matching SDK/NDK and OpenJDK |

Builders write sibling `Tejas-Impact-*-v0.7` outputs next to `game/`. Android uses IL2CPP, OpenGL ES 3 and local debug signing. The browser loader uses gzip with decompression fallback. The Windows helper is temporarily stashed during other platform builds and restored afterward.

See [`game/README.md`](game/README.md), [`game/NETWORKING.md`](game/NETWORKING.md) and [`game/RELEASE-NOTES-v0.7.md`](game/RELEASE-NOTES-v0.7.md) for details. The networking document retains the version-6 protocol and its earlier transport test history; v0.7 release checks are recorded separately below.

## Validation and limits

The delivered Windows, WebGL and Android builds compiled successfully. The Windows runtime suite passed combat, progression, quests, economy, save/backup recovery, input rules, terrain traversal and boss outcomes. Two separate Windows processes passed both local and public-HTTPS co-op checks. The desktop browser started, played and restored its journey after save/reload. The Android APK passed manifest and signature checks, and mobile landscape/portrait layouts were inspected in desktop preview mode.

**No physical Android phone was tested.** Phone frame rate, battery use, temperature, rendering and physical multitouch still need device testing. The two-player checks ran on one PC, not two separate home networks. Start with Low graphics on an Intel Iris Xe / 8 GB PC; the 30 FPS cap is a target, not a performance guarantee. No claim is made that every possible bug is fixed.

Full evidence and limitations: [`game/VALIDATION-v0.7.md`](game/VALIDATION-v0.7.md) and [`game/TEST-RESULT.txt`](game/TEST-RESULT.txt). Development-workspace paths in that report identify the original test logs; they are not bundled into this repository.

## Repository layout

```text
game/       Unity Assets, Packages, ProjectSettings and game documentation
website/    Static website and browser player
scripts/    Local website server and verified networking-helper download
```

Unity caches, local saves, credentials and generated native builds are excluded from Git. Downloadable native builds are release assets.
