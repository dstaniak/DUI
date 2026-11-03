using System;
using Microsoft.Xna.Framework;

namespace DUI.Graphics;

/// <summary>The 16 VIC-II colors, using the Pepto PAL RGB approximation shipped by VICE.</summary>
public static class C64Palette
{
    // Source: VICE-Team/svn-mirror, vice/data/C64/pepto-pal.vpl.
    private static readonly Color[] s_colors =
    [
        new(0x00, 0x00, 0x00), new(0xff, 0xff, 0xff),
        new(0x68, 0x37, 0x2b), new(0x70, 0xa4, 0xb2),
        new(0x6f, 0x3d, 0x86), new(0x58, 0x8d, 0x43),
        new(0x35, 0x28, 0x79), new(0xb8, 0xc7, 0x6f),
        new(0x6f, 0x4f, 0x25), new(0x43, 0x39, 0x00),
        new(0x9a, 0x67, 0x59), new(0x44, 0x44, 0x44),
        new(0x6c, 0x6c, 0x6c), new(0x9a, 0xd2, 0x84),
        new(0x6c, 0x5e, 0xb5), new(0x95, 0x95, 0x95)
    ];

    public static ReadOnlySpan<Color> Colors => s_colors;
    public static Color Black => s_colors[0];
    public static Color White => s_colors[1];
    public static Color Red => s_colors[2];
    public static Color Cyan => s_colors[3];
    public static Color Blue => s_colors[6];
    public static Color Yellow => s_colors[7];
    public static Color Orange => s_colors[8];
    public static Color LightRed => s_colors[10];
    public static Color LightBlue => s_colors[14];
    public static Color LightGray => s_colors[15];
}
