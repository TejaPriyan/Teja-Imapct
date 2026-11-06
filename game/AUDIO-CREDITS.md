# Teja's Impact v0.6 — original music and sound effects

All score and sound effects in `Assets/Resources/Audio` are original procedural compositions and synthesis authored for this game. No downloaded recording, third-party sample, paid sound pack or external music-generation service was used.

- **Skybound Morning:** a 40-second exploration loop in D major with flute melody, harp-like arpeggios, pads and light percussion.
- **Aether Pursuit:** a 20-second combat loop with B minor harmony and stronger plucked and percussion rhythms.
- **Crown of the Rift:** a 20-second boss loop with low drums, bell figures and B minor/F-sharp dominant harmony.
- **Twenty-one sound effects:** three sword swings, two guns, recharge/reload, normal and boss impacts, hurt, pickups/rewards, healing, level-up, victory, interface feedback, jump, dodge, footsteps, frost burst and attack warnings.

The Python score and synthesizer are supplied in `Assets/AudioSource/generate_audio.py`. They produce the local PCM WAV files at 22,050 Hz. `AUDIO-MANIFEST.json` records source asset hashes and measurements, and `Assets/AudioSource/validate_audio.py` reproduces the waveform checks.

The music shares a 96 BPM pulse and crossfades between exploration, combat and boss play. Settings provide separate music and effects volumes. Cached playback uses three music sources and ten effect voices; decoded sample data totals approximately 14.47 MiB.

These original audio files may be reused and modified with the game. No third-party audio attribution is required. The licenses of the character, weapon assets, fonts, engine and networking helper are unchanged.
