# Teja's Impact v0.7

A noncommercial action-RPG prototype with one bounded Aetherfall island, eleven regions, a supplied Eula character skin named Teja, three weapons, three NPC quests, a grotto puzzle, waystone checkpoints, 47 enemies including the Rift Warden, six chests, progression, local saves, original music and sound effects.

## Editions

- **Windows x64:** extract the whole Windows ZIP and open TejasImpact.exe. Keep its Data folder, DLLs and MonoBleedingEdge beside it. Solo and two-player internet rooms.
- **Browser:** serve the complete WebGL folder over HTTP/HTTPS and open index.html; do not open it directly from disk. Click Enter Aetherfall to start. This is the same Unity adventure, with solo play and browser-local storage. Use a current desktop Chrome, Edge or Firefox with hardware acceleration, WebGL 2 and WebAssembly. Clearing site data removes progress. Mobile browsers are directed to the download information.
- **Android ARM64:** download the APK, open it on an Android 8.0+ phone, review the phone's installation prompt and launch. Landscape only, in either horizontal orientation. Solo play. This is a directly distributed test build, not a Play Store listing; physical phone performance is not yet verified.
- **iPhone/iPad:** no installable app is supplied. A Mac, Xcode, Apple signing and device validation are still required. The user has no Mac available for this release.

Start on Low graphics on Intel Iris Xe / 8 GB RAM. Low uses a 30 FPS cap with shadows disabled. Frame caps and device compatibility targets are not performance guarantees. Close other heavy apps. There is no account or cloud-save system, and saves do not sync between editions or devices.

## Desktop controls

| Action | Control |
| --- | --- |
| Move / sprint | WASD or arrows / Left Ctrl |
| Camera | Move mouse; wheel zoom; T recenter |
| Attack / fire | Left click or J; hold to repeat |
| Aim / target lock | Hold right click or toggle V / Tab |
| Jump / dodge / Frost Nova | Space / Left Shift / E |
| Interact / reload / heal | F / R / H |
| Switch owned weapon | 1 / 2 / 3 |
| Journal / upgrades / loadout / map | Q / C / I / M |
| Shop / pause | B near village trader / Escape |
| HUD cursor | Hold Alt |

## Mobile controls

Turn the phone horizontally. A left movement stick and an independent right-side drag region support moving, looking and attacking with separate fingers. Hold Strike/Fire to repeat attacks. Dedicated buttons provide jump, dodge, Frost Nova, heal, aim, reload, weapon switching, target lock, sprint and camera recentering. Tap the interaction prompt near people, rewards and the merchant. Tap the health card for upgrades, quest card for the journal, and Loadout, Map or Menu at the top.

The layout respects the safe area. Rotation, menus and focus loss clear active gestures to prevent stuck movement/fire. A portrait prompt pauses play. The pause menu uses enlarged touch buttons; music/effects and camera settings have tap controls.

## Windows internet co-op

Both players need the complete same-version Windows build. Choose Online co-op, host a room, and privately send the entire invitation code. Your friend pastes it to join. The host's PC, game and connection must remain running. Coins, unlocked weapons, upgrades, quests and rewards are shared; health, ammunition and equipped weapon are individual. Menus pause the shared world. The host saves shared progress, while guests keep their solo saves separate.

Rooms use the included Cloudflare Quick Tunnel helper and a random room secret. No paid hosting account is needed, but this development relay has no uptime guarantee. Rehosting creates a new code. Browser and Android builds omit the Windows helper and native socket transport. See NETWORKING.md for limits and attribution.

## Save and combat fixes

v0.7 can recover a damaged primary save from its good backup. A failed Continue returns to the title and preserves the files rather than silently creating a fresh save. Recovery does not overwrite the good backup with the corrupt primary. Point-blank ranged shots now detect a target containing the aim-ray origin. Falling below the map returns the character to the active waystone and clears downward velocity and dash state. New journeys clear stale hit/landing effects and autosave timing.

Windows progress normally lives at %USERPROFILE%/AppData/LocalLow/Teja Games/Teja's Impact/journey-v3.json, with a .bak copy. Existing v0.3–v0.6 saves remain compatible; v0.2 saves can be imported. Android stores saves in its app data. Browser saves use the site's browser storage. Uninstalling or clearing app/site data can remove progress.

## Build from source

Use Unity 2022.3.20f1. Add this source folder in Unity Hub and open Assets/Scenes/Aetherfall.unity. The world is generated at runtime. Unity regenerates Library on a clean checkout.

Editor methods: TejaBuilder.BuildWindows, TejaWebBuilder.BuildWebGL, TejaMobileBuilder.BuildAndroid. They regenerate the character and scene, and write sibling v0.7 outputs. WebGL needs WebGL Build Support. Android needs Android Build Support, the matching SDK/NDK and OpenJDK. Android uses ARM64 IL2CPP, OpenGL ES 3 and local debug signing. WebGL uses gzip with decompression fallback and a custom loading/error/fullscreen page. Builds temporarily stash the Windows networking executable and restore it afterward; the editor Restore native helper menu handles an interrupted build.

The repository website is in ../website. It includes gameplay information, system requirements, desktop/mobile control guides, installation help, credits and direct GitHub Release downloads. Published package checksums are available in PACKAGE-MANIFEST-v0.7.json on the release page.

## Validation and limits

See VALIDATION-v0.7.md and TEST-RESULT.txt for actual delivered-build checks. Smoke mode uses -batchmode -nographics -teja-smoke -tejaTestResult <absolute-file> -logFile <absolute-log>. The -teja-input-check mode is isolated from normal saves. Add -teja-mobile-preview to inspect phone controls in a desktop window; it is not a substitute for testing a physical phone.

This remains a prototype. Procedural scenery, heuristic character skinning and clothing intersections remain. It is not a streamed open world or a production matchmaking service. No voice chat, Google login, multiple playable heroes, cloud saves or iOS package is included. The supplied character metadata records noncommercial restrictions. Retain ATTRIBUTION.md, FONT-CREDITS.md, AUDIO-CREDITS.md and all bundled licenses.
