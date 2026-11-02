# Audio sources

The original music is `../pydim-byte-hook-620646.mp3`, supplied by the
project owner with permission to use it in this type of game. It is not
part of the CC0 effect collection below. The full track is converted to
`DUI.Game/Content/Audio/theme.ogg`, played on repeat at 35% music volume.

These source effects were downloaded on 2026-11-02. Each source page
identifies its sound as [CC0 1.0][cc0]. Copies kept here are unmodified;
the game uses the processed WAV files in `DUI.Game/Content/Audio/`.

| Source file | Author and original | Game use |
|---|---|---|
| `burp.wav` | [Joseph SARDIN — Burp #4][burp] | Beer, short burp |
| `drink-exhale.mp3` | [craigglenday — Drinking.wav][exhale] | Vodka, human "ahh" exhale after a drink |
| `sniff.mp3` | [OwlStorm / Ashe Kirk — Sniff][sniff] | Powder, nasal sniff |
| `crunch.1.ogg` | [Tito and StarNinjas — 7 Eating Crunches][eat] | Mushroom and sausage, eating |
| `crash.mp3` | [squareal — Car Crash][crash] | Car hitting a boundary or its tail |

Freesound MP3s are the public high-quality previews. The eating source is
`crunch.1.ogg` from the author's `crunch_-_tito.zip`. The burp is the
original WAV. Download URLs:

- <https://bigsoundbank.com/UPLOAD/bwf-en/1710.wav>
- <https://cdn.freesound.org/previews/517/517173_2290066-hq.mp3>
- <https://cdn.freesound.org/previews/151/151218_140737-hq.mp3>
- <https://opengameart.org/sites/default/files/crunch_-_tito.zip>
- <https://cdn.freesound.org/previews/237/237375_1502374-hq.mp3>

## Rebuilding

Run `python3 SourceAssets/Audio/build_audio.py` from the repository root.
Requires FFmpeg with libvorbis and Python 3; uses no Python packages or
network access. This is an asset-authoring step, not a game dependency.
The normal MonoGame build consumes the checked-in OGG and WAV files.

The vodka effect uses seconds 2.78–4.15 of the drinking recording: the
final human exhale, with the sip and gulp omitted. Its natural pitch and
speed are preserved, with a gentle 70 Hz high-pass to remove low rumble.

The script keeps one short action per effect, filters low rumble and high
noise, applies edge fades, and normalizes peaks to -3 dBFS. Effects are
mono 44.1 kHz, 16-bit PCM. The music retains its full length and stereo
channels, encoded as Vorbis quality 5. The original source MP3 is retained.

`Content.mgcb` ships `Audio/credits.txt` alongside the compiled audio.
UI and pickup-wall bounce effects remain from the tutorial.

[cc0]: https://creativecommons.org/publicdomain/zero/1.0/
[burp]: https://bigsoundbank.com/burp-4-s1710.html
[exhale]: https://freesound.org/people/craigglenday/sounds/517173/
[sniff]: https://freesound.org/people/OwlStorm/sounds/151218/
[eat]: https://opengameart.org/content/7-eating-crunches
[crash]: https://freesound.org/people/squareal/sounds/237375/
