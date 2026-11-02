DUI - Snake

Rozpakuj całe archiwum. Gra zawiera .NET i swoje zasoby; nie wymaga SDK.
Windows: uruchom DUI.Desktop.exe w katalogu DUI.
Linux x64: uruchom ./DUI.Desktop w katalogu DUI.
macOS: uruchom DUI.app. Wybierz paczkę Intel albo Apple Silicon.

Sterowanie: strzałki / WASD, D-pad lub lewy drążek pada.
Pauza: Escape / Start / Back lub przycisk PAUSE.
RESUME wznawia grę; RETRY zaczyna od nowa. Zamknij okno, aby wyjść.
Wynik to liczba aut wraz z głową, pokazywana przez pięć gwiazdek.

macOS: tryb podpisu tej paczki znajdziesz w BUILD.txt, w wierszu
"macOS distribution". "Developer ID + Apple notarization" oznacza podpis
wydawcy i dołączony ticket Apple. "ad hoc (not notarized)" oznacza paczkę
testową bez notaryzacji. W tym drugim przypadku system może po pierwszej
próbie otwarcia wymagać zatwierdzenia aplikacji
w Ustawienia systemowe > Prywatność i ochrona > Otwórz mimo to.
Dotyczy to tylko tej aplikacji; nie trzeba wyłączać ochrony systemu.
https://support.apple.com/en-gb/102445

Wspierane paczki: Windows x64, Linux x64, macOS 15+ Intel / Apple Silicon.
Wymagany jest system zgodny z .NET 10 oraz działający sterownik OpenGL.
Linux wymaga zwyklych bibliotek systemowych desktopu (m.in. glibc, ICU,
X11/Wayland i obsługi dźwięku); self-contained dołącza .NET, nie cały system.

Zgłoszenia problemów: https://github.com/dstaniak/DUI/issues
Podaj system, nazwę paczki, kroki odtworzenia i komunikat błędu.
Identyfikator paczki znajduje się w BUILD.txt (na Macu: wewnątrz
DUI.app/Contents/MacOS). Licencja fontu: Content/Fonts/c64-license.txt
(na Macu Content jest w DUI.app/Contents/Resources).
