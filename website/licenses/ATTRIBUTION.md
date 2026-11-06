# Teja's Impact v0.7 — asset attribution

This noncommercial prototype combines original project code and procedural scenery with the third-party assets listed below. The in-game name **Teja** does not change the identity or ownership of the supplied Eula character artwork.

## User-supplied Eula character model

- **Title embedded in the supplied GLB:** Genshin Impact - Eula
- **Author/uploader recorded in the metadata:** [Meta Headz](https://sketchfab.com/MetaHeadz)
- **Source recorded in the metadata:** [Genshin Impact - Eula on Sketchfab](https://sketchfab.com/3d-models/genshin-impact-eula-932cc401fc2a456ba424aa102f339f4d)
- **License recorded in the metadata:** [Creative Commons Attribution-NonCommercial 4.0 International — CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/)
- **Original file supplied by the user:** `genshin_impact_-_eula.glb`

Converted source and metadata are retained in `Assets/CharacterSource`. That folder's `ATTRIBUTION.txt` preserves the source credit. Generated Unity meshes, materials, prefab, and clips are stored in `Assets/Resources/Eula`.

**Changes for this prototype:** converted coordinates and scale; extracted mesh and texture data into Unity-readable source files; created a 20-bone skeleton and heuristic skin weights; generated a skinned prefab; added a right-hand weapon socket; authored 15 motion clips and transitions, including sprinting, three sword strikes and reloading; revised leg IK and locomotion; applied the project's character shader; and attached separately sourced or original weapons. These modifications were not supplied or endorsed by Meta Headz or HoYoverse.

The recorded CC BY-NC 4.0 license requires attribution, a license link, indication of changes, and noncommercial use. This document records the supplied file's metadata; it does not independently establish that the uploader controls all underlying character rights. The live Sketchfab page could not be fetched during this update, so the creator and license entry above are specifically attributed to the embedded metadata. [CC BY-NC 4.0 license summary](https://creativecommons.org/licenses/by-nc/4.0/).

Eula and Genshin Impact are associated with HoYoverse. This prototype has no official affiliation or endorsement. Before public or commercial distribution, replace the character with an original or appropriately cleared asset, or establish the necessary permissions. A license statement attached by a model uploader alone is not a clearance of every underlying right.

## Kenney Blaster Kit

- **Creator:** [Kenney](https://kenney.nl/)
- **Asset pack:** [Blaster Kit, version 2.1](https://kenney.nl/assets/blaster-kit)
- **License:** [Creative Commons Zero 1.0 Universal — CC0](https://creativecommons.org/publicdomain/zero/1.0/)
- **Included assets:** `blaster-a`, `blaster-k`, and the `variation-a` texture, used for the Dawn Sidearm and Aurora Rifle.
- **Retained license:** `Assets/Resources/Weapons/KENNEY-LICENSE.txt`

The models are rescaled, reoriented, attached to the generated hand socket, and rendered with the project's character shader. Gameplay names, prices, firing behavior, ammunition rules, and effects are original to this prototype. Kenney's asset page and included license identify the pack as CC0; CC0 permits reuse and modification, including commercial use, subject to rights outside its scope. [Blaster Kit](https://kenney.nl/assets/blaster-kit), [CC0 summary](https://creativecommons.org/publicdomain/zero/1.0/).

## Original project content and other components

The Aetherfall layout, procedural scenery, enemy geometry, Frostglass sword geometry, game logic, generated rig and motion curves, interface layouts and drawn graphics, and synthesized sound effects were created for this prototype. This does not claim ownership of the underlying Eula model, textures, design, or Kenney weapon meshes.

The project uses Unity and its included runtime components under their applicable terms. The interface bundles Rubik Regular and Rajdhani SemiBold under SIL Open Font License 1.1. A static Rubik instance was generated for Unity’s legacy font renderer; copyright notices and full licenses are retained in Assets/Resources/Fonts. See FONT-CREDITS.md for sources and modifications. System fonts are only a fallback if a packaged font cannot load. No paid stock character, animation, or environment pack was added.

Keep this attribution file and the source license notices with copies of the prototype and its source. This file does not relicense third-party content or imply endorsement by its creators.


## v0.6 original audio and internet helper

The score and sound effects were authored for this prototype by its included deterministic synthesis script. See AUDIO-CREDITS.md. The internet helper is Cloudflare cloudflared 2026.10.4, distributed with its Apache 2.0 license in Assets/StreamingAssets/Networking/CLOUDFLARED-LICENSE.txt. See NETWORKING.md.
