# Releasing DUI

The [Desktop builds](../.github/workflows/desktop.yml) workflow prepares the game
for Windows x64, Linux x64, and macOS Intel and Apple Silicon. Packages include
.NET 10, MonoGame/Gum libraries, and assets. Players do not need the SDK or MGCB.
This does not cover mobile ports or a browser version.

## First CI run

1. Add the configuration and scripts to the `main` branch on GitHub.
2. Open the repository → **Actions → Desktop builds**. A push to `main`
   starts the process automatically; **Run workflow** lets you start it manually.
3. After a successful run, download the **DUI-dev-<SHA>-all-platforms** artifact
   at the bottom of the run page. Extract GitHub's outer ZIP — it contains the
   four game archives and `SHA256SUMS.txt`. Upload these inner archives to itch.io.

No PAT or itch.io key is required. Jobs have `contents: read`; only the job
preparing the draft Release gets `contents: write` through `GITHUB_TOKEN`.
If an organization policy blocks GitHub Actions or writing to Releases,
allow this workflow in the repository/organization settings.

## Workflow triggers

| Event | Result |
|---|---|
| Pull request to `main` | 64 game tests and release tooling tests on four runners |
| Push to `main` | Tests, publishing, four archives, and SHA-256 checksums |
| Manual **Run workflow** | The same set of packages; `sign_macos` enables signing and notarization; no Release is created |
| Push of a `vA.B.C.D` tag | Tests, packages, and a draft GitHub Release; Mac signing when `MACOS_SIGNING_ENABLED` is enabled |

Tests run on Windows Server 2022, Ubuntu 22.04, and macOS 15 on both
architectures. They do not need a GPU. Windows publishes `win-x64`,
`linux-x64`, `osx-x64`, and `osx-arm64` in sequence with the same DesktopGL content;
shader compilation on Windows does not require Wine.

The [release script](../scripts/release.py) uses Release, self-contained,
without trimming, AOT, single-file, ReadyToRun, or tiered compilation.
Packaging takes place on the target operating system. The script checks the
assets listed in MGCB, streamed OGG files, native libraries, the bundled runtime,
and executable architecture, then extracts the archive and repeats the checks.
Linux preserves the executable bit in TAR.GZ; macOS uses `ditto` for ZIP.

Mac packages contain `DUI.app`: the executable and native libraries in
`Contents/MacOS`, managed DLLs and .NET configuration in `Contents/Resources`,
game assets in `Contents/Resources/Content`, and metadata in `Contents/Info.plist`.
Relative symlinks preserve .NET lookup paths inside the app;
packaging also checks them after extraction. The `allow-jit` entitlement
allows .NET code to run without AOT.
The default ad hoc signature is also checked after extraction. It does not
verify the publisher's identity. After [Developer ID setup](MacSigning.md),
releases can use a signature with a secure timestamp, hardened runtime,
Apple notarization, and a stapled ticket. The ZIP is recreated after stapling;
the script checks the ticket and Gatekeeper assessment after extraction as well.
The distribution mode of each package is recorded in `BUILD.txt`.
A signing failure, rejected notarization, or timeout stops the job and prevents
creation of the draft Release; there is no fallback to ad hoc.
The declared minimum macOS version is 15; Linux requires desktop system libraries
and an OpenGL driver, which a self-contained publish does not replace.

## Versioned releases

The tag must follow the exact `vA.B.C.D` format, for example `v1.0.0.0`.
Invalid tags starting with `v` stop package builds.
The workflow does not create tags or change versions in project files.
The tag version identifies archives and `BUILD.txt`; macOS metadata uses
the first three components and retains the full version in `CFBundleGetInfoString`.
Branch packages use `dev-` and the first 12 characters of the SHA.

After choosing the version and commit, create the tag and push it to GitHub
according to the project rules. Once the workflow finishes, open **Releases**:
a draft will appear with four archives and `SHA256SUMS.txt`.
Add the change description and check the game before publishing the draft.
A rerun can replace files in an existing draft; the workflow does not overwrite
a published release. In that case, prepare a new tag.

Actions packages are retained for 30 days; raw directories used to transfer
files between runners are retained for 1 day. Release attachments are not
subject to this Actions retention period. [GitHub: artifacts](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts)

## Manual upload to itch.io

1. On itch.io, select **Create new game**, the game classification, and a downloadable project.
2. Upload the four archives from Actions or the Release. Select the appropriate OS
   for each file; both Mac packages use macOS, with **Intel** and **Apple Silicon** in their names.
3. Add a description, controls, cover image, and screenshots. In the installation
   instructions, state the Mac signing mode from `BUILD.txt`. For an ad hoc package,
   include the first-launch instructions from the [player README](PlayerReadme.txt).
4. Keep the page as a draft and check its preview and package downloads.
   When the release is ready, set its visibility to **Public**.

For updates, upload the new release's archives to the same page.
Automatic uploads through butler are not currently configured.
[itch.io guide](https://itch.io/docs/creators/getting-started)

## Pre-publication checks

- Download and extract the package outside the repository; also test a path with spaces.
- Launch it without `dotnet run`. On Mac, open `DUI.app` from Finder.
- Check audio, vodka shaking, the mushroom rainbow, and visual effect suppression
  after powder; the UI should remain unaffected by shaders.
- Check arrow keys/WASD, pausing, focus loss, RESUME, and RETRY.
- Perform these checks on Windows, Linux, and both Mac architectures.
  Automated model and package structure tests do not check graphics or audio.
- Compare SHA-256 against the manifest: `shasum -a 256` on Mac, `sha256sum`
  on Linux, or `Get-FileHash -Algorithm SHA256` in PowerShell.

For ad hoc packages, macOS may block the first launch. After attempting to open
the app, use **System Settings → Privacy & Security → Open Anyway**.
Do not disable Gatekeeper globally. [Apple guide](https://support.apple.com/en-gb/102445)
Check Developer ID packages after downloading them through a browser to test
the normal Gatekeeper checks for downloaded files.

## Local builds

Requirements: .NET SDK 10 and Python 3.9+. From the repository directory:

```sh
python3 scripts/release.py publish
python3 scripts/release.py package --rid osx-arm64 --version dev-0123456789ab --source artifacts/publish/osx-arm64
python3 -m unittest discover -s scripts/tests -v
```

Replace the sample identifier with your own SHA. On Windows, use `python`
instead of `python3`. Local publishing on macOS/Linux requires Wine/MGFXC
configuration, just like a regular game build. Create macOS packages on a Mac.
Linux TAR.GZ archives can be prepared on a Unix system, including a Mac,
but the game must be tested on Linux; CI packages them on Ubuntu.
Output goes into the ignored `artifacts/` directory.
