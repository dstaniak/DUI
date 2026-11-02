# Wydawanie DUI

Workflow [Desktop builds](../.github/workflows/desktop.yml) przygotowuje grę
na Windows x64, Linux x64 oraz macOS Intel i Apple Silicon. Paczki zawierają
.NET 10, biblioteki MonoGame/Gum i zasoby. Gracz nie potrzebuje SDK ani MGCB.
Nie obejmuje to portów mobilnych ani wersji przeglądarkowej.

## Pierwsze uruchomienie CI

1. Umieść konfigurację i skrypty na GitHubie w gałęzi `main`.
2. Otwórz repozytorium → **Actions → Desktop builds**. Push do `main`
   uruchamia proces automatycznie; **Run workflow** pozwala uruchomić go ręcznie.
3. Po sukcesie pobierz artefakt **DUI-dev-<SHA>-all-platforms** z dołu strony
   wykonania. Rozpakuj zewnętrzny ZIP GitHuba — w środku są cztery właściwe
   archiwa gry i `SHA256SUMS.txt`. To wewnętrzne archiwa wgrywa się na itch.io.

Nie potrzeba PAT ani klucza itch.io. Jobs mają `contents: read`; tylko
przygotowanie szkicu Release dostaje `contents: write` przez `GITHUB_TOKEN`.
Jeżeli polityka organizacji blokuje GitHub Actions lub zapis do Releases,
trzeba dopuścić ten workflow w ustawieniach repozytorium/organizacji.

## Co uruchamia workflow

| Zdarzenie | Wynik |
|---|---|
| Pull request do `main` | 64 testy gry i testy narzędzi wydawania na czterech runnerach |
| Push do `main` | Testy, publikacja, cztery archiwa i sumy SHA-256 |
| Ręczne **Run workflow** | Ten sam zestaw paczek; bez tworzenia Release |
| Push tagu `vA.B.C.D` | Testy, paczki i szkic GitHub Release |

Testy działają na Windows Server 2022, Ubuntu 22.04 oraz macOS 15 w obu
architekturach. Nie potrzebują GPU. Windows publikuje kolejno `win-x64`,
`linux-x64`, `osx-x64` i `osx-arm64` z tym samym contentem DesktopGL;
kompilacja shaderów na Windowsie nie wymaga Wine.

[Skrypt wydawania](../scripts/release.py) używa Release, self-contained,
bez trimming, AOT, single-file, ReadyToRun i tiered compilation.
Pakowanie odbywa się na systemie docelowym. Skrypt sprawdza zasoby wpisane
w MGCB, strumieniowane OGG, biblioteki natywne, dołączony runtime i architekturę
pliku wykonywalnego, a potem rozpakowuje archiwum i ponawia kontrolę.
Linux zachowuje bit wykonywalności w TAR.GZ; macOS używa `ditto` do ZIP.

Mac otrzymuje `DUI.app`: program i biblioteki natywne w `Contents/MacOS`,
zarządzane DLL i konfigurację .NET w `Contents/Resources`, zasoby gry
w `Contents/Resources/Content`, metadane w `Contents/Info.plist`.
Względne dowiązania zachowują ścieżki wyszukiwania .NET wewnątrz aplikacji;
pakowanie sprawdza je także po rozpakowaniu. Uprawnienie `allow-jit`
pozwala uruchomić kod .NET bez AOT.
Podpis ad hoc jest sprawdzany również po rozpakowaniu. Nie potwierdza to
tożsamości wydawcy i nie zastępuje Developer ID ani notaryzacji Apple.
Minimalna deklarowana wersja macOS to 15; Linux wymaga bibliotek systemowych
desktopu i sterownika OpenGL, których publikacja self-contained nie zastępuje.

## Wydanie z numerem

Tag musi mieć dokładnie format `vA.B.C.D`, np. `v1.0.0.0`.
Niepoprawne tagi zaczynające się od `v` zatrzymują budowanie paczek.
Workflow nie tworzy tagów i nie zmienia wersji w plikach projektu.
Numer tagu identyfikuje archiwa i `BUILD.txt`; metadane macOS używają
trzech pierwszych członów, a pełny numer zachowują w `CFBundleGetInfoString`.
Paczki z gałęzi używają `dev-` i pierwszych 12 znaków SHA.

Po wybraniu numeru i commita utwórz tag oraz wyślij go na GitHub zgodnie
z regułami projektu. Po zakończeniu workflow przejdź do **Releases**:
pojawi się szkic z czterema archiwami i `SHA256SUMS.txt`.
Uzupełnij opis zmian, wykonaj kontrolę gry i dopiero wtedy opublikuj szkic.
Ponowne wykonanie może zastąpić pliki istniejącego szkicu; opublikowanego
wydania workflow nie nadpisuje. Wtedy należy przygotować nowy tag.

Paczki w Actions są przechowywane 30 dni; surowe katalogi służące do
przekazania między runnerami — 1 dzień. Załączniki Releases nie podlegają
tej retencji Actions. [GitHub: artefakty](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts)

## Ręczne wgranie na itch.io

1. W itch.io wybierz **Create new game**, klasyfikację gry i projekt do pobrania.
2. Wgraj cztery archiwa z Actions albo Release. Zaznacz właściwy system przy
   każdym pliku; oba Maci to macOS, z nazwami **Intel** i **Apple Silicon**.
3. Dodaj opis, sterowanie, okładkę i zrzuty ekranu. W instrukcji instalacji
   umieść informację o braku notaryzacji na Macu z [README gracza](PlayerReadme.txt).
4. Zachowaj stronę jako szkic, sprawdź podgląd i pobieranie paczek.
   Gdy wydanie jest gotowe, ustaw widoczność **Public**.

Przy aktualizacji wgrywaj archiwa nowego wydania na tę samą stronę.
Automatyczna wysyłka przez butler nie jest obecnie skonfigurowana.
[Instrukcja itch.io](https://itch.io/docs/creators/getting-started)

## Kontrola przed publikacją

- Pobierz i rozpakuj paczkę poza repozytorium; sprawdź także ścieżkę ze spacjami.
- Uruchom ją bez korzystania z `dotnet run`. Na Macu otwórz `DUI.app` z Findera.
- Sprawdź dźwięk, drżenie po wódce, tęczę po grzybku i wyciszanie efektów
  po proszku; UI powinno pozostawać poza shaderami.
- Sprawdź strzałki/WASD, pauzę, utratę aktywności, RESUME oraz RETRY.
- Wykonaj tę kontrolę na Windowsie, Linuxie i obu architekturach Maca.
  Automatyczne testy modeli i struktury paczek nie sprawdzają grafiki ani audio.
- Porównaj SHA-256 z manifestem: `shasum -a 256` na Macu, `sha256sum`
  na Linuxie lub `Get-FileHash -Algorithm SHA256` w PowerShell.

macOS może zablokować pierwsze uruchomienie pobranej aplikacji. Po próbie
otwarcia użyj **Ustawienia systemowe → Prywatność i ochrona → Otwórz mimo to**.
Nie wyłączaj globalnie Gatekeepera. [Instrukcja Apple](https://support.apple.com/en-gb/102445)

## Budowanie lokalne

Wymagane: .NET SDK 10 i Python 3.9+. Z katalogu repozytorium:

```sh
python3 scripts/release.py publish
python3 scripts/release.py package --rid osx-arm64 --version dev-0123456789ab --source artifacts/publish/osx-arm64
python3 -m unittest discover -s scripts/tests -v
```

Zastąp przykładowy identyfikator własnym SHA. Na Windowsie użyj `python`
zamiast `python3`. Publikacja lokalna na macOS/Linux wymaga konfiguracji
Wine/MGFXC, tak samo jak zwykły build gry. Paczki macOS twórz na Macu.
Linuxowe TAR.GZ można przygotować na systemie Unix, również na Macu,
ale uruchomienie gry trzeba sprawdzić na Linuxie; CI pakuje je na Ubuntu.
Wyniki są w ignorowanym katalogu `artifacts/`.
