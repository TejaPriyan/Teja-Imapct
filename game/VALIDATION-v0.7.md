# Teja's Impact v0.7 — release validation

Validated on 5 November 2026 using Unity 2022.3.20f1 on Windows with Intel Iris Xe. This report records the completed checks and device-testing limits. A successful build does not establish that gameplay works on every device.

## Results

| Check | Result | Evidence in the development workspace |
| --- | --- | --- |
| Initial Windows x64 build | PASS | `work/v07-windows-build-1.log`, `TEJA_WINDOWS_BUILD_OK` |
| Initial runtime regression suite | PASS | `work/v07-smoke-1/result.txt` |
| Final Windows build and regression after mobile menu changes | PASS | `work/v07-windows-build-2.log`, `TEJA_WINDOWS_BUILD_OK`; `work/v07-smoke-final/result.txt` |
| Browser save bridge | PASS | `work/v07-web-bridge-test.cjs`; checks startup population, one shared `FS.syncfs` queue, preservation of external callbacks, coalesced saves, error/throw recovery and a single visibility handler |
| Initial WebGL build | Build PASS; browser startup FAILED | `work/v07-webgl-build-1.log`; runtime reported a missing `CapsuleCollider` and a subsequent null reference |
| WebGL build 3 and interactive browser gameplay | PASS with a storage warning identified | `work/v07-webgl-build-3.log`; startup, movement, menus, save/return, reload and Continue restored progress in the browser. A concurrent `FS.syncfs` warning led to the serialized bridge fix |
| Final WebGL build 4 and storage retest | PASS | `work/v07-webgl-build-4.log`; title, Continue, pause, repeated save, return to title and page reload worked. No new console warnings or errors appeared in the final sessions at 14:43–14:44 UTC; earlier warnings belonged to build 3 |
| Initial Android ARM64 APK build | PASS | `work/v07-android-build-1.log`, `TEJA_ANDROID_BUILD_OK` |
| Initial Android APK metadata and signature inspection | PASS | `work/v07-android-package-1.txt`; ARM64, API 26 minimum, OpenGL ES 3, landscape, v2 signature, no Windows executable |
| Final Android APK build and package inspection | PASS | `work/v07-android-build-2.log`, `TEJA_ANDROID_BUILD_OK`; `work/v07-android-package-final.txt`; `work/v07-android-manifest.json` |
| Mobile landscape and portrait previews | PASS for observed desktop previews | At 844 × 390 the enlarged pause/settings panels fit and were readable. At 390 × 844 the rotation screen appeared and paused the journey; `work/v07-mobile-preview-final.log`, `work/v07-mobile-portrait-final.log` |
| Responsive website and Android download | PASS for checked flows | Desktop and 390 × 844 layouts, navigation and control tabs; real 23.6 MiB APK assembled, verified and saved through the browser. Downloaded APK SHA-256 matches the final build. Node tests cover corrupted chunks and cancellation; static audit checks 39 local references |
| v0.7 two-player Windows regression over local HTTP | Host and guest PASS | `work/v07-local-final/host-result.txt`, `work/v07-local-final/guest-result.txt` |
| v0.7 two-player Windows regression through public HTTPS | Host and guest PASS | `work/v07-public-final/host-result.txt`, `work/v07-public-final/guest-result.txt` |

## Regression scope

The final v0.7 runtime suite passed landscape touch-input rules, safe-area mapping, independent finger ownership, movement directions and modal blocking. New save tests cover recovery from a valid backup, keeping that backup during recovery, and preserving invalid files when Continue cannot load either copy. Combat tests cover a point-blank ranged hit with the ray starting inside a target. Fall recovery restores the selected checkpoint and clears downward velocity and dash state.

Existing coverage includes original audio assets, the grotto puzzle and checkpoints, guest save isolation, 47 enemies and the phased boss, 188 coins, six chests, all three NPC quests, duplicate reward prevention, progression caps and restoration, shop economy, ammunition, reload and healing, terrain collision and bridge traversal, scenery blocking shots, fifteen animation clips and skin weights, and victory/defeat/reset. Desktop input checks include A/D at four camera headings, left-click attacks, mouse look, aim, cursor release and modal blocking. Tests use isolated state rather than ordinary journey saves.

Headless runs use `-nographics`; unsupported-shader messages from the null graphics device are expected in those logs. They do not validate rendered graphics or performance. Desktop phone-preview mode validates layout and input rules but does not emulate an Android device, GPU, operating system or physical multitouch hardware.

Both v0.7 multiplayer runs used two separate Windows game processes on this PC. The local run used HTTP; the public run used a temporary public HTTPS relay. Both verified room joining, a shared purchase, duplicate purchase protection, guest movement, a guest ranged kill, a shared quest reward exactly once, and a guest boss kill producing shared victory. These automated tests use seeded conditions and are not a complete manual playthrough on separate physical devices.

## Android package inspection

The final APK identifies itself as `com.tejagames.tejasimpact`, version `0.7.0`, version code `7`. Android SDK tools reported minimum SDK 26, target SDK 32, OpenGL ES 3 and exclusively `arm64-v8a` native libraries. The activity uses `userLandscape` orientation. APK Signature Scheme v2 verified with the local Android Debug certificate. This is a directly distributed test build, not a Play Store release or a production signing setup. No `.exe` was present in the APK; the `cloudflared` license remained included.

The final APK is 24,777,298 bytes with SHA-256 `3858BB356BE6C4BE1FDCC4DBDBA14F373430A2008AA51B5F80837A2874D686F0`. The Android rebuild includes the primitive preservation fix applied after the first browser startup failure. Its successful build, signature and manifest checks do not establish physical-device startup or gameplay quality; no Android device was available for that verification.

## Reproduce

Run the extracted Windows player with absolute output paths:

```text
TejasImpact.exe -batchmode -nographics -teja-smoke -tejaTestResult <result-file> -logFile <log-file>
```

For multiplayer checks, start one process with `-teja-coop-host-test` and another with `-teja-coop-guest-test`, both using the same `-teja-coop-test-dir <absolute-directory>`. Add `-teja-coop-public` to both for a public HTTPS relay; otherwise the test uses local HTTP. Do not share `invite.private.txt`. These tests run two processes on one PC and are not equivalent to testing two physical PCs on different home networks.

For interactive isolated controls, use `-teja-input-check`; add `-teja-mobile-preview` to expose phone controls in a desktop window. For Android inspection, use the installed SDK's `aapt dump badging`, `aapt dump xmltree ... AndroidManifest.xml`, and `apksigner verify --verbose`. Check that no `.exe` is present in the APK archive.

Serve the complete browser output over HTTP or HTTPS. Test startup through the Enter Aetherfall button, movement, attacks, menus, local saving and reload in a desktop WebGL 2 browser. The JavaScript bridge test can be run with Node using `work/v07-web-bridge-test.cjs`.

## Release limits

- No physical Android phone or tablet was tested. Sustained frame rate, temperature, battery usage, multitouch feel and device-specific rendering remain unverified.
- No iPhone/iPad app is supplied. The user has no Mac available; Xcode, Apple signing and device validation are still required. The Unity 2022 browser edition targets desktop/laptop browsers.
- Browser and Android editions are solo. Windows retains two-player rooms using a temporary Cloudflare relay, with the host PC and game required to stay running. Progress remains local to each edition/device; there are no accounts, cloud saves, voice chat or production matchmaking.
- The world is one bounded prototype island. Procedural scenery, heuristic character rigging, clothing intersections and other prototype limitations remain.
- No complete manual playthrough across every platform, long-duration network soak, hostile-client penetration test, broad accessibility certification or performance guarantee is claimed.
- The supplied character retains the noncommercial restrictions and attribution in `ATTRIBUTION.md`.

`work/package-v07.ps1` verifies every Windows/source ZIP entry against the corresponding source SHA-256 and excludes Unity caches. Final archive sizes and hashes belong in `outputs/PACKAGE-MANIFEST-v0.7.json`. Packaging remains pending until the two remaining checks above are finalized.
