# Teja's Impact v0.7.1 — Android compatibility validation

Built on 6 November 2026 with Unity 2022.3.20f1 on Windows. This update replaces the v0.7 Android package that targeted API 32.

| Check | Result | Evidence |
| --- | --- | --- |
| Unity Android build | PASS | `work/v071-android-clean-build.log` contains `Build Finished, Result: Success` and `TEJA_ANDROID_BUILD_OK` |
| Target SDK | PASS | `aapt dump badging` reports `targetSdkVersion:'36'`, `compileSdkVersion:'36'` |
| Package version | PASS | package `com.tejagames.tejasimpact`, version `0.7.1`, version code `8` |
| Device scope | PASS | minimum API 26 (Android 8), ARM64 only, OpenGL ES 3 |
| APK signing | PASS | `apksigner verify --verbose` reports a valid APK Signature Scheme v2 signature |
| Hash | PASS | 24,777,302 bytes; SHA-256 `82ABC85BC54C625015C2B323175B824DE7711D45D2F4AEA9525B7B07386FE91E` |

## Build-tool fix

Unity 2022's bundled Android Gradle Plugin used an obsolete AAPT2 resource linker with Platform 36. The build now uses the API 36-compatible Build Tools 36.1 AAPT2 when it is available from the project-local SDK. The official Android Platform 36 archive was checksum-verified before this build.

## Limits

This is still a direct, debug-signed test APK and is not a Google Play release. It has not yet been installed and played on a physical Android device, so startup, touch responsiveness, device-specific graphics, performance and any remaining installation warning must be checked on the phone before claiming full device compatibility.
