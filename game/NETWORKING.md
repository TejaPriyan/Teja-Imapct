# Teja's Impact v0.6 — internet co-op

This Windows prototype supports a host and one guest. Both players need the complete v0.6 Windows download, extracted before launching.

1. The host chooses **Online co-op**, then **Host**.
2. Wait for the room to open, then copy the complete `TEJA6` invitation.
3. Share it privately with your friend. The guest pastes it into **Online co-op** and joins.
4. Keep the host's game, PC and internet connection running throughout the session. Closing the room stops its connection helper. A newly hosted room has a new code.

The host owns shared enemies, rewards and journey progress. The guest's solo save stays separate. Opening a gameplay menu pauses the shared world. This prototype uses temporary Cloudflare Quick Tunnels rather than a permanent game server. No account, domain or paid subscription is required for this connection method; availability and latency depend on the network and Cloudflare's development service. It has no uptime guarantee. See [Cloudflare Quick Tunnels](https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/).

## Connection behavior

The host listens on a random loopback port. The bundled helper forwards the temporary public HTTPS address to that session-only listener. The listener accepts only the two game POST endpoints, with protocol version 6, a random 128-bit room secret and a guest identifier. It serves no files. Incoming payloads, headers, queues and concurrent connections are bounded; only one guest is admitted. Invitations are limited to the expected `trycloudflare.com` host format. Redirects are disabled.

The guest normally uses the system resolver and HTTP client. If a home router cannot resolve a new temporary tunnel hostname, the game can query Cloudflare's HTTPS DNS API for that validated room hostname. No invitation secret is sent to the DNS API. This fallback changes no Windows DNS or hosts settings. It connects to the resolved public address while TLS authenticates the **original room hostname** using default certificate validation; certificate validation is never bypassed. The HTTP fallback accepts legitimate repeated metadata headers while rejecting ambiguous response framing and enforcing the snapshot size limit. See [Cloudflare DNS over HTTPS](https://developers.cloudflare.com/1.1.1.1/encryption/dns-over-https/make-api-requests/).

Requests have cancellation deadlines and retry backoff. The host reserves the disconnected guest's slot briefly before admitting a replacement. The gameplay layer resets acknowledgement state when the transport's `PeerSessionId` changes. `LastDiagnostic` exposes bounded error categories and connection stages without the room hostname, invitation or secret.

## Bundled helper

- Upstream: [Cloudflare cloudflared 2026.10.4](https://github.com/cloudflare/cloudflared/releases/tag/2026.10.4).
- Asset: `cloudflared-windows-amd64.exe`, bundled as `Assets/StreamingAssets/Networking/cloudflared.exe` in the Windows and complete source release ZIPs. In a Git checkout, run `scripts/fetch-network-helper.ps1` from the repository root before building Windows co-op; the script downloads this pinned helper and checks its SHA-256. The helper executable is excluded from Git.
- Size: 55,344,848 bytes.
- SHA-256: `411b16d40c8d872243efeb52c550053d00af0a8a97fbb32e730fb8aba429eaa8`.
- License: Apache License 2.0, included alongside the binary as `CLOUDFLARED-LICENSE.txt`.
- The download hash was checked against the official release's published checksum.

## Transport validation

Validation completed on 29 October 2026:

- **25 loopback checks passed** with real HTTP peers, including both payload directions, invalid invitations/secrets/headers/endpoints, room capacity, packet size limits, guest replacement after the grace period, peer generation changes, missing helper handling and socket cleanup. The loopback suite also passed using Unity's bundled Mono runtime.
- **7 public TLS checks passed** using a temporary Cloudflare tunnel and a **20,517-byte** snapshot. The test deliberately selected the secure-address TLS path while retaining the original hostname's certificate checks. Guest input and host snapshots crossed the public HTTPS endpoint. The owned helper exited on disposal, and rapid host cancellation cleared owned helper state.
- The earlier small-packet public test also passed. Large-packet testing exposed and fixed rejection of repeated HTTP metadata headers from the proxy.
- Unity's standalone `mono.exe` cannot supply HTTPS TLS support in this installation; its public harness result is not an integrated game result. Actual UnityPlayer public-session validation is recorded separately in the release validation document.

Harness and raw result files are in the development workspace under `work/v06-network/`. These transport tests exercise the same transport source used by the game; they do not by themselves validate all gameplay synchronization or a friend's separate device/network.

## Current scope

The bundled internet helper is Windows x64. Android and iPhone internet co-op builds have not been packaged or verified. This is a two-player prototype, with host authority and temporary rooms, rather than a production matchmaking service. There is no server-side account, cloud save, voice chat or permanent room address in this implementation.

