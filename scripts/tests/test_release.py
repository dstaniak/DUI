import hashlib
import os
from pathlib import Path
import struct
import sys
import tempfile
import unittest
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import release


class ReleaseTests(unittest.TestCase):
    def test_versions_distinguish_tags_and_commits(self):
        sha = "0123456789abcdef" * 2 + "01234567"
        self.assertEqual("v1.2.3.4", release.version_for_ref("tag", "v1.2.3.4", sha))
        self.assertEqual("dev-0123456789ab", release.version_for_ref("branch", "main", sha))
        for tag in ("v1.2", "v1.2.3.4-beta", "v01.2.3.4", "v1.2.3.4/../escape"):
            with self.subTest(tag=tag), self.assertRaises(ValueError):
                release.version_for_ref("tag", tag, sha)
        with self.assertRaises(ValueError):
            release.archive_name("win-x64", "../../escape")

    def test_content_includes_buffered_music_and_raw_font(self):
        required = set(release.required_content())
        for path in ("Audio/theme.xnb", "Audio/credits.txt", "Fonts/c64.fnt",
                     "Fonts/c64-license.txt", "Images/tilemap-definition.xml",
                     "Effects/rainbowEffect.xnb", "Effects/doubleVisionEffect.xnb"):
            self.assertIn(Path(path), required)
        self.assertNotIn(Path("Audio/theme.ogg"), required)

    def test_only_streamed_songs_require_a_separate_audio_file(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            content = root / "DUI.Game" / "Content"
            content.mkdir(parents=True)
            (content / "Content.mgcb").write_text(
                "#begin Audio/song.ogg\n/processor:SongProcessor\n"
                "/build:Audio/song.ogg\n"
                "#begin Audio/theme.ogg\n/processor:SoundEffectProcessor\n"
                "/build:Audio/theme.ogg\n"
                "#begin Audio/credits.txt\n/copy:Audio/credits.txt\n",
                encoding="utf-8")
            with mock.patch.object(release, "ROOT", root):
                self.assertEqual(set(release.required_content()), {
                    Path("Audio/song.xnb"), Path("Audio/song.ogg"),
                    Path("Audio/theme.xnb"), Path("Audio/credits.txt")})

    def test_missing_or_empty_resources_fail_validation(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            with self.assertRaises(ValueError):
                release.require_files(root, ["shader.xnb"])
            (root / "shader.xnb").touch()
            with self.assertRaises(ValueError):
                release.require_files(root, ["shader.xnb"])

    def test_wrong_mac_architecture_is_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            executable = Path(temporary) / "DUI.Desktop"
            executable.write_bytes(struct.pack("<II", 0xFEEDFACF, 0x0100000C) + bytes(56))
            release.check_architecture(executable, "osx-arm64")
            with self.assertRaises(ValueError):
                release.check_architecture(executable, "osx-x64")

    def test_checksums_require_exactly_the_four_matching_packages(self):
        version = "v1.2.3.4"
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            names = [release.archive_name(rid, version) for rid in release.TARGETS]
            for name in names[:-1]:
                (root / name).write_bytes(b"package")
            with self.assertRaises(ValueError):
                release.checksums(root, version)
            (root / names[-1]).write_bytes(b"package")
            release.checksums(root, version)
            lines = (root / "SHA256SUMS.txt").read_text().splitlines()
            digest = hashlib.sha256(b"package").hexdigest()
            self.assertEqual([f"{digest}  {name}" for name in sorted(names)], lines)
            release.checksums(root, version)  # Re-running the same collection is safe.
            (root / "DUI-v9.0.0.0-windows-x64.zip").write_bytes(b"stale")
            with self.assertRaises(ValueError):
                release.checksums(root, version)

    @unittest.skipIf(os.name == "nt", "Creating symlinks needs Windows developer mode.")
    def test_bundle_links_allow_aliased_parent_but_not_external_files(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            app = root / "real" / "DUI.app"
            app.mkdir(parents=True)
            (root / "alias").symlink_to(root / "real", target_is_directory=True)
            (app / "assembly.dll").write_bytes(b"assembly")
            (app / "linked.dll").symlink_to("assembly.dll")
            release.check_bundle_links(root / "alias" / "DUI.app")
            (root / "outside.dll").write_bytes(b"external")
            (app / "external.dll").symlink_to(root / "outside.dll")
            with self.assertRaises(ValueError):
                release.check_bundle_links(app)


if __name__ == "__main__":
    unittest.main()
