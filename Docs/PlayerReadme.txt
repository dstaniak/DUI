DUI - Snake

Extract the entire archive. The game includes .NET and its assets; no SDK is required.
Windows: run DUI.Desktop.exe in the DUI directory.
Linux x64: run ./DUI.Desktop in the DUI directory.
macOS: launch DUI.app. Choose the Intel or Apple Silicon package.

Controls: arrow keys / WASD, D-pad, or the controller's left stick.
Pause: Escape / Start / Back or the PAUSE button.
RESUME continues the game; RETRY starts over. Close the window to exit.
The score counts all cars, including the head, and is shown by five stars.

macOS: the signing mode for this package is listed in BUILD.txt on the
"macOS distribution" line. "Developer ID + Apple notarization" means it has
a publisher signature and a stapled Apple ticket. "ad hoc (not notarized)"
means it is a test package without notarization. In the latter case, after
the first attempt to open it, the system may require you to approve the app
in System Settings > Privacy & Security > Open Anyway.
This applies only to this app; you do not need to disable system protection.
https://support.apple.com/en-gb/102445

Supported packages: Windows x64, Linux x64, macOS 15+ Intel / Apple Silicon.
A .NET 10-compatible operating system and a working OpenGL driver are required.
Linux requires standard desktop system libraries (including glibc, ICU,
X11/Wayland, and audio support); self-contained includes .NET, not the entire OS.

Report issues: https://github.com/dstaniak/DUI/issues
Include your OS, package name, steps to reproduce, and the error message.
The package identifier is in BUILD.txt (on Mac: inside
DUI.app/Contents/MacOS). Font license: Content/Fonts/c64-license.txt
(on Mac, Content is in DUI.app/Contents/Resources).
