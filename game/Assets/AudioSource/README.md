# Teja's Impact — original audio

This soundtrack and these effects are synthesized from the included Python score. They use no downloaded recordings, third-party samples, paid sound packs or external music services.

## Music

- **Skybound Morning** (`music-exploration.wav`): 40-second, 16-bar exploration loop at 96 BPM in D major, with an original flute melody, harp-like arpeggios, warm pads and soft percussion.
- **Aether Pursuit** (`music-combat.wav`): 20-second combat loop with B minor harmony, plucked figures and a stronger percussion pulse.
- **Crown of the Rift** (`music-boss.wav`): 20-second boss loop with B minor/F-sharp dominant harmony, low drums and rising bell phrases.

The loops use the same tempo and align every eight bars. Reverb/delay tails wrap into the beginning. The runtime crossfades between exploration, nearby combat and boss encounters, and lowers the score while menus are open.

## Effects

Twenty-one short cached effects cover three sword swings, sidearm/rifle shots, reload/recharge completion, regular and boss impacts, hurt, coins, treasure, healing, level-up, victory, UI, jump, dodge, footsteps, frost burst and attack warnings. Synthesis includes oscillator sweeps, filtered noise, pitch envelopes and original chime sequences.

## Reproduce and validate

Run `python generate_audio.py` with Python 3 and NumPy. The output is 22,050 Hz PCM16 WAV in `Assets/Resources/Audio`. The generated `AUDIO-MANIFEST.json` records durations, channels, sample rates, peaks, RMS, decoded sample sizes and SHA-256 hashes.

The 24 clips total **14.47 MiB** of decoded 32-bit sample data. This excludes small Unity object/voice overhead. Runtime playback caches clips once and uses three music sources and a pool of ten effect voices; it does not generate clips on each attack. Volumes are separate for music and effects, with a master sound toggle and debounced preference writes. Smoke and isolated input-check modes do not write audio preferences.

Run `python validate_audio.py` to verify the manifest hashes, WAV format, duration, peak and RMS levels, DC offset, zero-ended effects, music boundary transitions and sample-memory budget. `AUDIO-VALIDATION.json` records those checks. These waveform checks do not replace listening to the final game mix.

These original project audio files may be reused and modified with the game. No third-party audio attribution is required. Other character, weapon, engine and font licenses are unaffected.

## Integration hooks

1. Call `InitializeAudio()` once after `BuildCamera()` and the existing audio source setup in `Awake`.
2. Call `TickAudio(dt)` once per `Update`, including while menus are open; call it before any early return for preview camera overrides.
3. Replace the old `Tone` implementation with `PlayLegacySfx(frequency,duration)`. This maps existing events to cached effects. Prefer `PlaySfx("name")` for new code; `PlaySfxAt("name",world)` supports distance attenuation and stereo positioning.
4. Add `PlaySfx("jump")` to a successful grounded jump; `PlaySfxAt("hit",e.root.position)` to enemy hit reactions; `PlaySfxAt("warning",e.root.position,.6f)` when a boss/charge attack is telegraphed; and `PlaySfx("ui",.45f)` to accepted UI button clicks.
5. Expose `MusicVolume` / `SfxVolume` getters in sliders calling `SetMusicVolume(value)` / `SetSfxVolume(value)`. `SetAudioEnabled(value)` is available; legacy direct `soundOn` changes are also detected. Call `SaveAudioSettings()` on application quit. Invalid numeric values cannot reach audio sources. `PlayLegacyTone` is also available as an alias for `PlayLegacySfx`.
6. Call `ValidateAudio()` from the smoke test after initialization. It checks imported clips, volume clamps, pool size and the decoded sample budget.

Code and the generator were reviewed locally; full Unity compilation and audible playtesting belong to the integrated game validation pass.
