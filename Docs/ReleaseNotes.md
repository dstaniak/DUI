Paczki DUI zawierają środowisko .NET oraz zasoby gry.

- Windows x64: rozpakuj ZIP i uruchom `DUI/DUI.Desktop.exe`.
- Linux x64: rozpakuj TAR.GZ i uruchom `DUI/DUI.Desktop`.
- macOS: wybierz Intel (`macos-x64`) albo Apple Silicon (`macos-arm64`),
  rozpakuj ZIP i uruchom `DUI.app`.

macOS: aplikacja nie ma certyfikatu Developer ID ani notaryzacji Apple.
Pierwsze uruchomienie może wymagać zatwierdzenia w ustawieniach
**Prywatność i ochrona → Otwórz mimo to**.

`SHA256SUMS.txt` zawiera sumy kontrolne czterech archiwów.
Sterowanie: strzałki/WASD lub pad, Escape pauzuje grę.

Przed opublikowaniem tego szkicu:

- [ ] Sprawdź uruchomienie pobranych paczek na Windowsie, Linuxie i obu Macach.
- [ ] Sprawdź dźwięk, shadery powerupów, sterowanie, pauzę i RETRY.
- [ ] Dopisz opis zmian w tym wydaniu i usuń tę listę kontrolną.
