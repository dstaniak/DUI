"""Build the C64 uppercase bitmap font. Requires Pillow; no runtime font install."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
OUTPUT = HERE.parents[1] / "DUI.Game" / "Content" / "Fonts"
FONT = ImageFont.truetype(str(HERE / "c64.ttf"), 8)
GLYPHS = list(range(32, 96)) + [0x2030]
ATLAS = Image.new("RGBA", (256, 64))
RECORDS = []

for index, code in enumerate(GLYPHS):
    width = 12 if code == 0x2030 else 8
    mask = Image.new("1", (width, 8))
    draw = ImageDraw.Draw(mask)
    if code == 0x2030:
        # PETSCII has no per-mille sign. Match the font's square pixel grid.
        for x, y in [(0, 0), (5, 5), (9, 5)]:
            draw.rectangle((x, y, x + 2, y + 2), outline=1)
        draw.line((1, 7, 8, 0), fill=1)
    else:
        # This font's uppercase 8x8 cells use a baseline at y=7.
        draw.text((0, 2), chr(code), font=FONT, fill=1)
    x = (index % 16) * 16
    y = (index // 16) * 12
    ATLAS.paste((255, 255, 255, 255), (x, y), mask)
    RECORDS.append(
        f"char id={code} x={x} y={y} width={width} height=8 "
        f"xoffset=0 yoffset=0 xadvance={width} page=0 chnl=15"
    )

OUTPUT.mkdir(parents=True, exist_ok=True)
ATLAS.save(OUTPUT / "c64.png")
HEADER = [
    'info face="C64 Uppercase" size=8 bold=0 italic=0 charset="" unicode=1 '
    'stretchH=100 smooth=0 aa=1 padding=0,0,0,0 spacing=0,0 outline=0',
    'common lineHeight=8 base=7 scaleW=256 scaleH=64 pages=1 packed=0 '
    'alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4',
    'page id=0 file="c64.png"',
    f'chars count={len(GLYPHS)}',
]
(OUTPUT / "c64.fnt").write_text("\n".join(HEADER + RECORDS) + "\n")
