# C64 UI font

`c64.ttf` is the C64 character font from [zeiche/c64font](https://github.com/zeiche/c64font).
It is distributed by its author under CC0 1.0 Universal; see `LICENSE.txt`.
Downloaded on 2026-11-01 from the repository's `main` branch.

Run `python3 SourceAssets/Fonts/build_font.py` with Pillow installed to rebuild
`DUI.Game/Content/Fonts/c64.fnt` and `c64.png`. The game needs neither Pillow
nor an installed system font. Glyphs use an 8×8 grid without antialiasing.
The atlas includes ASCII 32–95 (uppercase text, numbers and punctuation),
plus a custom 12×8 per-mille symbol drawn on the same grid.
