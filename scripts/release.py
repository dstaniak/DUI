#!/usr/bin/env python3
"""Publish and package DesktopGL builds. Requires Python 3.9+; no pip packages."""

import argparse
from dataclasses import dataclass
import hashlib
import json
import os
from pathlib import Path
import plistlib
import re
import shutil
import struct
import subprocess
import sys
import tarfile
import tempfile
import zipfile


ROOT = Path(__file__).resolve().parents[1]
TARGETS = {
    "win-x64": ("windows-x64", ".zip"),
    "linux-x64": ("linux-x64", ".tar.gz"),
    "osx-x64": ("macos-x64", ".zip"),
    "osx-arm64": ("macos-arm64", ".zip"),
}
RELEASE_VERSION = r"v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)"


@dataclass(frozen=True)
class MacSigning:
    identity: str
    notary_profile: str
    keychain: Path = None

    def __post_init__(self):
        if not re.fullmatch(r"[0-9a-fA-F]{40}", self.identity):
            raise ValueError("Use the SHA-1 identity of a Developer ID Application certificate.")
        if not self.notary_profile.strip():
            raise ValueError("Developer ID packaging requires a notarytool keychain profile.")

    def keychain_arguments(self):
        return ["--keychain", str(self.keychain)] if self.keychain else []


def run(*args):
    subprocess.run([str(arg) for arg in args], check=True, cwd=ROOT)


def version_for_ref(ref_type, ref_name, sha):
    if ref_type == "tag":
        if not re.fullmatch(RELEASE_VERSION, ref_name):
            raise ValueError("Release tags must have the format vA.B.C.D.")
        return ref_name
    if not re.fullmatch(r"[0-9a-fA-F]{40}", sha):
        raise ValueError("A full Git commit SHA is required for development builds.")
    return "dev-" + sha[:12].lower()


def validate_version(version):
    if not (re.fullmatch(RELEASE_VERSION, version)
            or re.fullmatch(r"dev-[0-9a-f]{12}", version)):
        raise ValueError("Expected vA.B.C.D or dev-<12 lowercase hex digits>.")


def archive_name(rid, version):
    validate_version(version)
    platform, extension = TARGETS[rid]
    return f"DUI-{version}-{platform}{extension}"


def publish():
    # One Windows job compiles the shared DesktopGL shaders without Wine.
    # The same command also works locally when MGFXC/Wine is configured.
    run("dotnet", "tool", "restore")
    for rid in TARGETS:
        output = ROOT / "artifacts" / "publish" / rid
        if output.exists():
            shutil.rmtree(output)
        run("dotnet", "publish", "DUI.Desktop/DUI.Desktop.csproj",
            "-c", "Release", "-r", rid, "--self-contained", "true",
            "-p:PublishTrimmed=false", "-p:PublishAot=false",
            "-p:PublishSingleFile=false", "-p:PublishReadyToRun=false",
            "-p:TieredCompilation=false", "--nologo", "-o", output)


def required_content():
    """Check every MGCB entry, including raw files used outside Content.Load."""
    manifest = ROOT / "DUI.Game" / "Content" / "Content.mgcb"
    processor = None
    for line in manifest.read_text(encoding="utf-8").splitlines():
        if line.startswith("#begin "):
            processor = None
        elif line.startswith("/processor:"):
            processor = line.split(":", 1)[1]
        if line.startswith(("/build:", "/copy:")):
            command, value = line.split(":", 1)
            source, _, destination = value.partition(";")
            path = Path(destination or source)
            yield path.with_suffix(".xnb") if command == "/build" else path
            if command == "/build" and processor == "SongProcessor":
                yield path  # SongProcessor also ships the streamed audio file.


def require_files(directory, names):
    for name in names:
        path = directory / name
        if not path.is_file() or path.stat().st_size == 0:
            raise ValueError(f"Missing or empty package file: {path}")


def check_architecture(executable, rid):
    with executable.open("rb") as stream:
        header = stream.read(64)
        if rid == "win-x64":
            if header[:2] != b"MZ":
                raise ValueError(f"Not a Windows executable: {executable}")
            stream.seek(struct.unpack_from("<I", header, 60)[0])
            pe = stream.read(6)
            valid = pe == b"PE\0\0\x64\x86"
        elif rid == "linux-x64":
            valid = (header[:6] == b"\x7fELF\x02\x01"
                     and struct.unpack_from("<H", header, 18)[0] == 62)
        else:
            cpu = 0x0100000C if rid == "osx-arm64" else 0x01000007
            valid = (header[:4] == b"\xcf\xfa\xed\xfe"
                     and struct.unpack_from("<I", header, 4)[0] == cpu)
    if not valid:
        raise ValueError(f"Executable does not match {rid}: {executable}")


def check_payload(binary_dir, content_dir, rid):
    executable = "DUI.Desktop.exe" if rid == "win-x64" else "DUI.Desktop"
    require_files(binary_dir, [executable, "DUI.Desktop.dll",
                              "DUI.Desktop.deps.json", "DUI.Desktop.runtimeconfig.json",
                              "System.Private.CoreLib.dll", "MonoGame.Framework.dll",
                              "MonoGameGum.dll"])
    native_files = {
        "win-x64": ["coreclr.dll", "hostfxr.dll", "hostpolicy.dll", "SDL2.dll", "openal.dll"],
        "linux-x64": ["libcoreclr.so", "libhostfxr.so", "libhostpolicy.so", "libSDL2-2.0.so.0", "libopenal.so"],
        "osx-x64": ["libcoreclr.dylib", "libhostfxr.dylib", "libhostpolicy.dylib", "libSDL2-2.0.0.dylib", "libopenal.dylib"],
        "osx-arm64": ["libcoreclr.dylib", "libhostfxr.dylib", "libhostpolicy.dylib", "libSDL2-2.0.0.dylib", "libopenal.dylib"],
    }
    require_files(binary_dir, native_files[rid])
    require_files(content_dir, required_content())
    config = json.loads((binary_dir / "DUI.Desktop.runtimeconfig.json").read_text())
    if "includedFrameworks" not in config["runtimeOptions"]:
        raise ValueError("Package must contain its own .NET runtime.")
    check_architecture(binary_dir / executable, rid)


def sign_mac_code(path, signing=None, entitlements=None):
    args = ["codesign", "--force", "--sign", signing.identity if signing else "-"]
    if signing:
        args += ["--timestamp", "--options", "runtime", *signing.keychain_arguments()]
    else:
        args += ["--timestamp=none"]
    if entitlements:
        args += ["--entitlements", entitlements]
    run(*args, path)


def notarize_mac_app(app, signing):
    credentials = ["--keychain-profile", signing.notary_profile,
                   *signing.keychain_arguments()]
    # Stapling changes the bundle, so the submitted ZIP is never distributed.
    with tempfile.TemporaryDirectory(prefix="dui-notary-") as temporary:
        submission = Path(temporary) / "DUI.zip"
        run("ditto", "-c", "-k", "--keepParent", "--norsrc", app, submission)
        result = subprocess.run(
            ["xcrun", "notarytool", "submit", str(submission), *credentials,
             "--wait", "--timeout", "20m", "--output-format", "json"],
            stdout=subprocess.PIPE, text=True, cwd=ROOT)
        print(result.stdout)
        try:
            response = json.loads(result.stdout)
        except json.JSONDecodeError:
            result.check_returncode()
            raise ValueError("Apple returned an invalid notarization response.")
        if response["status"] in ("Accepted", "Invalid"):
            run("xcrun", "notarytool", "log", response["id"], *credentials)
        result.check_returncode()
        if response["status"] != "Accepted":
            raise ValueError(f"Apple notarization was not accepted: {response['status']}")
    run("xcrun", "stapler", "staple", app)
    verify_notarization(app)


def verify_notarization(app):
    run("xcrun", "stapler", "validate", app)
    run("spctl", "--assess", "--type", "execute", "--verbose=2", app)


def make_mac_app(payload, version, signing=None):
    app = payload / "DUI.app"
    binaries = app / "Contents" / "MacOS"
    resources = app / "Contents" / "Resources"
    binaries.mkdir(parents=True)
    resources.mkdir()
    for path in list(payload.iterdir()):
        if path != app:
            shutil.move(str(path), resources if path.name == "Content" else binaries)

    # CFBundle versions contain at most three components. Keep the full release
    # label (including D or development SHA) in the visible info and build record.
    bundle_version = ".".join(version[1:].split(".")[:3]) if version.startswith("v") else "0.0.0"
    metadata = {
        "CFBundleName": "DUI",
        "CFBundleDisplayName": "DUI",
        "CFBundleExecutable": "DUI.Desktop",
        "CFBundleIdentifier": "io.github.dstaniak.dui",
        "CFBundlePackageType": "APPL",
        "CFBundleInfoDictionaryVersion": "6.0",
        "CFBundleShortVersionString": bundle_version,
        "CFBundleVersion": bundle_version,
        "CFBundleGetInfoString": f"DUI {version}",
        "LSMinimumSystemVersion": "15.0",
        "LSApplicationCategoryType": "public.app-category.games",
        "NSHighResolutionCapable": True,
    }
    with (app / "Contents" / "Info.plist").open("wb") as stream:
        plistlib.dump(metadata, stream)

    # Apple treats files in MacOS as native code. Managed assemblies/configs
    # belong in Resources; relative links keep .NET's adjacent-file lookup.
    for path in list(binaries.iterdir()):
        if path.suffix != ".dylib" and path.name not in ("DUI.Desktop", "createdump"):
            destination = resources / path.name
            shutil.move(str(path), destination)
            if destination.is_file():
                destination.chmod(0o644)
            path.symlink_to(Path("../Resources") / path.name)

    # Sign nested native code first, then seal the bundle. Development builds
    # use ad hoc signatures; Developer ID builds also require notarization.
    native = [*binaries.glob("*.dylib"), binaries / "createdump"]
    for path in native:
        if path.is_file():
            path.chmod(0o755)
            sign_mac_code(path, signing)
            # .NET resolves the main managed assembly's link to Resources and
            # searches for its runtime/native dependencies beside that target.
            (resources / path.name).symlink_to(Path("../MacOS") / path.name)
    (binaries / "DUI.Desktop").chmod(0o755)
    sign_mac_code(app, signing, ROOT / "scripts" / "macos.entitlements")
    run("codesign", "--verify", "--deep", "--strict", app)
    if signing:
        notarize_mac_app(app, signing)
    return app


def check_bundle_links(app):
    bundle_root = app.resolve()
    for path in app.rglob("*"):
        if path.is_symlink() and not path.resolve(strict=True).is_relative_to(bundle_root):
            raise ValueError(f"Bundle link points outside the app: {path}")


def verify_archive(archive, rid, notarized=False):
    # Only extract archives produced by this script, in a disposable directory.
    with tempfile.TemporaryDirectory(prefix="dui-verify-") as temporary:
        extracted = Path(temporary)
        if rid.startswith("osx-"):
            run("ditto", "-x", "-k", archive, extracted)
            app = extracted / "DUI.app"
            binary_dir = app / "Contents" / "MacOS"
            content_dir = app / "Contents" / "Resources" / "Content"
            run("plutil", "-lint", app / "Contents" / "Info.plist")
            run("codesign", "--verify", "--deep", "--strict", app)
            if notarized:
                verify_notarization(app)
            # The host resolves the managed assembly symlink before loading
            # the runtime. Check that location too, not just the launcher dir.
            managed_dir = (binary_dir / "DUI.Desktop.dll").resolve().parent
            require_files(managed_dir, ["DUI.Desktop.runtimeconfig.json",
                                       "libhostfxr.dylib", "libhostpolicy.dylib",
                                       "libcoreclr.dylib", "libSDL2-2.0.0.dylib",
                                       "libopenal.dylib"])
            check_bundle_links(app)
        elif rid == "linux-x64":
            with tarfile.open(archive) as bundle:
                bundle.extractall(extracted)
            binary_dir = extracted / "DUI"
            content_dir = binary_dir / "Content"
        else:
            with zipfile.ZipFile(archive) as bundle:
                bundle.extractall(extracted)
            binary_dir = extracted / "DUI"
            content_dir = binary_dir / "Content"
        check_payload(binary_dir, content_dir, rid)
        if rid != "win-x64" and not os.access(binary_dir / "DUI.Desktop", os.X_OK):
            raise ValueError("Archive lost the executable permission.")


def package(rid, version, source, output, signing=None):
    validate_version(version)
    if signing and not rid.startswith("osx-"):
        raise ValueError("Developer ID signing is only supported for macOS packages.")
    if rid.startswith("osx-") and sys.platform != "darwin":
        raise ValueError("macOS packages must be created on macOS.")
    if rid == "linux-x64" and os.name != "posix":
        raise ValueError("Linux TAR archives require a Unix host to preserve permissions.")
    check_payload(source, source / "Content", rid)
    output.mkdir(parents=True, exist_ok=True)
    archive = output / archive_name(rid, version)
    with tempfile.TemporaryDirectory(prefix="dui-package-") as temporary:
        payload = Path(temporary) / "DUI"
        shutil.copytree(source, payload)
        build_info = f"DUI {version}\nTarget: {rid}\n"
        if rid.startswith("osx-"):
            mode = "Developer ID + Apple notarization" if signing else "ad hoc (not notarized)"
            build_info += f"macOS distribution: {mode}\n"
        (payload / "BUILD.txt").write_text(build_info, encoding="utf-8")
        shutil.copy2(ROOT / "Docs" / "PlayerReadme.txt", payload / "README.txt")
        if rid.startswith("osx-"):
            app = make_mac_app(payload, version, signing)
            # ditto preserves Unix permissions and bundle metadata in ZIPs.
            run("ditto", "-c", "-k", "--keepParent", "--norsrc", app, archive)
        elif rid == "linux-x64":
            (payload / "DUI.Desktop").chmod(0o755)
            if (payload / "createdump").exists():
                (payload / "createdump").chmod(0o755)
            with tarfile.open(archive, "w:gz") as bundle:
                bundle.add(payload, arcname="DUI")
        else:
            with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as bundle:
                for path in sorted(payload.rglob("*")):
                    if path.is_file():
                        bundle.write(path, Path("DUI") / path.relative_to(payload))
    verify_archive(archive, rid, notarized=signing is not None)
    print(f"Verified {archive}")


def checksums(directory, version):
    expected = sorted(archive_name(rid, version) for rid in TARGETS)
    actual = sorted(path.name for path in directory.iterdir() if path.name != "SHA256SUMS.txt")
    if actual != expected:
        raise ValueError(f"Expected exactly four packages: {expected}; found: {actual}")
    lines = []
    for name in expected:
        digest = hashlib.sha256()
        with (directory / name).open("rb") as stream:
            for block in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(block)
        lines.append(f"{digest.hexdigest()}  {name}\n")
    (directory / "SHA256SUMS.txt").write_text("".join(lines), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    metadata = commands.add_parser("metadata")
    metadata.add_argument("--ref-type", required=True)
    metadata.add_argument("--ref-name", required=True)
    metadata.add_argument("--sha", required=True)
    commands.add_parser("publish")
    packaging = commands.add_parser("package")
    packaging.add_argument("--rid", choices=TARGETS, required=True)
    packaging.add_argument("--version", required=True)
    packaging.add_argument("--source", type=Path, required=True)
    packaging.add_argument("--output", type=Path, default=ROOT / "artifacts" / "packages")
    packaging.add_argument("--signing-identity", help="Developer ID Application certificate SHA-1")
    packaging.add_argument("--notary-profile", help="Stored notarytool keychain profile")
    packaging.add_argument("--keychain", type=Path, help="Optional dedicated signing keychain")
    hashing = commands.add_parser("checksums")
    hashing.add_argument("--directory", type=Path, required=True)
    hashing.add_argument("--version", required=True)
    args = parser.parse_args()
    if args.command == "metadata":
        version = version_for_ref(args.ref_type, args.ref_name, args.sha)
        print(f"version={version}")
        if os.environ.get("GITHUB_OUTPUT"):
            with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as stream:
                stream.write(f"version={version}\n")
    elif args.command == "publish":
        publish()
    elif args.command == "package":
        signing = None
        if args.signing_identity or args.notary_profile or args.keychain:
            if not args.signing_identity or not args.notary_profile:
                parser.error("Both --signing-identity and --notary-profile are required.")
            signing = MacSigning(args.signing_identity, args.notary_profile, args.keychain)
        package(args.rid, args.version, args.source.resolve(), args.output.resolve(), signing)
    else:
        checksums(args.directory, args.version)


if __name__ == "__main__":
    main()
