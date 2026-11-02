"""Rebuild the game's audio with FFmpeg; no downloads or Python packages needed."""

import array
import math
from pathlib import Path
import subprocess
import sys
import wave


SOURCE = Path(__file__).resolve().parent
OUTPUT = SOURCE.parents[1] / "DUI.Game/Content/Audio"
SAMPLE_RATE = 44100

# Keep one action per pickup and remove recording clicks and long tails.
# Times are seconds in the original source. The last value removes low rumble.
EFFECTS = (
    ("burp.wav", "beer-burp.wav", 0.0, 0.344, 70),
    # Keep the voiced attack of the short "ahh", without its quiet tail.
    ("relief-exhale.mp3", "vodka-exhale.wav", 0.03, 0.58, 70),
    ("sniff.mp3", "powder-sniff.wav", 0.10, 0.65, 100),
    ("crunch.1.ogg", "eat.wav", 0.50, 1.02, 80),
    ("crash.mp3", "crash.wav", 0.32, 1.65, 60),
)


def build_effect(source, destination, start, end, highpass):
    duration = end - start
    filters = (
        f"atrim=start={start}:end={end},asetpts=PTS-STARTPTS,"
        f"highpass=f={highpass},lowpass=f=9000,"
        "afade=t=in:d=0.005,"
        f"afade=t=out:st={duration - 0.04}:d=0.04"
    )
    decoded = subprocess.run(
        ["ffmpeg", "-v", "error", "-i", str(SOURCE / source),
         "-af", filters, "-ar", str(SAMPLE_RATE), "-ac", "1",
         "-f", "f32le", "-"],
        check=True, capture_output=True,
    )
    samples = array.array("f", decoded.stdout)
    if sys.byteorder != "little":
        samples.byteswap()

    peak = max(abs(sample) for sample in samples)
    if not math.isfinite(peak) or peak == 0:
        raise ValueError(f"Invalid or silent source: {source}")

    # Peak-normalize to -3 dBFS, leaving headroom for overlapping effects.
    gain = 32767 * 10 ** (-3 / 20) / peak
    pcm = array.array("h", (round(sample * gain) for sample in samples))
    if sys.byteorder != "little":
        pcm.byteswap()
    with wave.open(str(OUTPUT / destination), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(pcm.tobytes())
    print(f"{destination}: {len(samples) / SAMPLE_RATE:.3f} s")


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for effect in EFFECTS:
        build_effect(*effect)

    subprocess.run(
        ["ffmpeg", "-v", "error", "-y",
         "-i", str(SOURCE.parent / "pydim-byte-hook-620646.mp3"),
         "-map_metadata", "-1", "-ar", str(SAMPLE_RATE), "-ac", "2",
         "-c:a", "libvorbis", "-q:a", "5", str(OUTPUT / "theme.ogg")],
        check=True,
    )
    print("theme.ogg: converted the supplied track to stereo Vorbis")


if __name__ == "__main__":
    main()
