DUI packages include the .NET runtime and game assets.

- Windows x64: extract the ZIP and run `DUI/DUI.Desktop.exe`.
- Linux x64: extract the TAR.GZ and run `DUI/DUI.Desktop`.
- macOS: choose Intel (`macos-x64`) or Apple Silicon (`macos-arm64`),
  extract the ZIP, and launch `DUI.app`.

macOS: the signing and notarization mode is recorded in `BUILD.txt` inside the app.
An ad hoc package may require approval on first launch in
**Privacy & Security → Open Anyway**. A Developer ID package has a stapled
Apple notarization ticket.

`SHA256SUMS.txt` contains checksums for the four archives.
Controls: arrow keys/WASD or a controller; Escape pauses the game.

Before publishing this draft:

- [ ] Check that the downloaded packages launch on Windows, Linux, and both Mac architectures.
- [ ] Check audio, powerup shaders, controls, pausing, and RETRY.
- [ ] State the macOS signing mode above, matching this release's `BUILD.txt`.
- [ ] Add a description of the changes in this release and remove this checklist.
