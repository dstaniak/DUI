# Developer ID i notaryzacja DUI

Podpisywanie jest opcjonalne. Zwykły push do `main` nadal tworzy paczki ad hoc.
Konfiguracja poniżej włącza podpis Developer ID oraz notaryzację Apple dla
obu architektur macOS. Gra pozostaje dystrybuowana przez GitHub/itch.io.

## 1. Utwórz certyfikat

Potrzebne jest aktywne członkostwo Apple Developer Program i uprawnienia
Account Holder do utworzenia certyfikatu Developer ID.

1. Otwórz **Xcode → Settings → Accounts** i dodaj swoje konto Apple.
2. Wybierz właściwy zespół, **Manage Certificates**, następnie **+ → Developer ID Application**.
3. Po utworzeniu kliknij certyfikat prawym przyciskiem i wybierz **Export Certificate**.
   Zapisz plik `.p12` poza repozytorium i zabezpiecz go hasłem.

Eksport musi zawierać certyfikat i jego klucz prywatny. Sam plik `.cer`
nie wystarczy. DUI potrzebuje certyfikatu **Developer ID Application**;
typy Apple Development, Apple Distribution i Developer ID Installer
służą innym zastosowaniom.

Jeżeli tworzysz certyfikat przez portal Apple, wygeneruj CSR w Dostępie do
pęku kluczy, wybierz Developer ID Application w portalu, wgraj CSR i zainstaluj
pobrany `.cer` na tym samym Macu. Następnie wyeksportuj tożsamość jako `.p12`.
[Tworzenie certyfikatu — Apple](https://developer.apple.com/help/account/certificates/create-developer-id-certificates/)
[Zarządzanie i eksport w Xcode — Apple](https://developer.apple.com/documentation/xcode/sharing-your-teams-signing-certificates)

## 2. Dodaj sekrety GitHuba

W repozytorium wybierz **Settings → Secrets and variables → Actions → Secrets**.
Dodaj pięć sekretów repozytorium:

| Nazwa | Zawartość |
|---|---|
| `APPLE_CERTIFICATE_P12_BASE64` | Plik `.p12` zakodowany Base64 |
| `APPLE_CERTIFICATE_PASSWORD` | Hasło ustawione przy eksporcie `.p12` |
| `APPLE_ID` | Adres e-mail konta Apple Developer |
| `APPLE_TEAM_ID` | Team ID z informacji o członkostwie w koncie deweloperskim |
| `APPLE_APP_PASSWORD` | Osobne hasło aplikacji utworzone dla notaryzacji DUI |

Aby skopiować certyfikat do schowka na Macu:

```sh
base64 -i "/pełna/ścieżka/DeveloperID.p12" | pbcopy
```

Wklej wynik bezpośrednio do sekretu GitHuba. Hasło aplikacji utwórz w
**account.apple.com → Logowanie i zabezpieczenia → Hasła aplikacji**.
To hasło do notaryzacji, osobne od hasła eksportu `.p12` i głównego hasła Apple.
Nie umieszczaj certyfikatu, klucza ani haseł w repozytorium lub rozmowie.
Sam Team ID nie jest kluczem podpisującym.
[Sekrety certyfikatów — GitHub](https://docs.github.com/en/actions/how-tos/deploy/deploy-to-third-party-platforms/sign-xcode-applications)
[Hasła aplikacji — Apple](https://support.apple.com/en-us/102654)

## 3. Sprawdź i włącz wydania

1. Uruchom **Actions → Desktop builds → Run workflow** na `main`, zaznaczając
   **Sign and notarize macOS packages** (`sign_macos`).
2. Pobierz obie paczki macOS. `BUILD.txt` powinien zawierać
   `macOS distribution: Developer ID + Apple notarization`.
3. Sprawdź start z Findera po pobraniu ZIP przez przeglądarkę, dźwięk i grafikę
   na obu architekturach. Sam sukces notaryzacji nie testuje działania gry.
4. W **Settings → Secrets and variables → Actions → Variables** dodaj zmienną
   repozytorium `MACOS_SIGNING_ENABLED` o wartości `true`.
   Od tego momentu tagi `vA.B.C.D` automatycznie podpisują i notaryzują oba Maci.

Bez tej zmiennej tagi zachowują dotychczasowy tryb ad hoc. Ręczna opcja
`sign_macos` umożliwia próbę przed włączeniem podpisywania tagów.
Podpisywanie obsługuje wyłącznie `main` i tagi wydań; pull requesty uruchamiają
testy bez sekretów. Brak sekretu albo błąd Apple przerywa podpisany build,
bez publikowania paczek ad hoc w jego zastępstwie.

## Co robi workflow

[Skrypt konfiguracji](../scripts/setup-macos-signing.sh) importuje tożsamość
do tymczasowego pęku kluczy na jednorazowym runnerze GitHuba. Sprawdza typ
certyfikatu oraz zgodność Team ID i zapisuje profil `notarytool`.
Sekrety są dostępne tylko w kroku konfiguracji macOS. Końcowy krok usuwa
pęk kluczy również po błędzie; plik `.p12` jest usuwany zaraz po konfiguracji.
Nie używaj skryptu konfiguracji na własnym Macu ani runnerze self-hosted.

[release.py](../scripts/release.py) podpisuje wszystkie dołączone biblioteki
natywne i `createdump`, a potem `DUI.app`. Podpis Developer ID włącza hardened
runtime i znacznik czasu Apple. Główny program zachowuje `allow-jit` dla .NET.
Usługa Apple musi zwrócić `Accepted`; log notaryzacji trafia do logu joba.
Skrypt dołącza ticket do aplikacji, tworzy końcowy ZIP i sprawdza podpis,
ticket oraz ocenę Gatekeepera po jego rozpakowaniu. Dopiero potem powstają
sumy kontrolne i artefakty. Windows i Linux zachowują dotychczasowe pakowanie.

Oczekiwanie na Apple ma limit 20 minut. Po przekroczeniu limitu sprawdź
identyfikator zgłoszenia w logu; usługa może nadal je przetwarzać. Job nie
wydaje wówczas paczki. Po rozwiązaniu problemu ponów workflow.
[Notaryzacja — Apple](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution)
[Podpis i JIT w .NET — Microsoft](https://learn.microsoft.com/en-us/dotnet/core/deploying/macos)

## Podpis lokalny

Na Macu z zainstalowaną tożsamością wyświetl jej odcisk SHA-1:

```sh
security find-identity -v -p codesigning
xcrun notarytool store-credentials dui-notary
```

Drugie polecenie interaktywnie poprosi o dane Apple. Następnie:

```sh
python3 scripts/release.py package --rid osx-arm64 --version v1.0.0.0 \
  --source artifacts/publish/osx-arm64 \
  --signing-identity "40-ZNAKOWY-ODCISK-SHA1" --notary-profile dui-notary
```

Podstaw rzeczywisty odcisk, wersję i architekturę. Opcjonalne `--keychain`
wskazuje osobny pęk kluczy; domyślnie narzędzia korzystają z pęku użytkownika.
Podpis i notaryzacja zawsze występują razem. Bez obu opcji powstaje paczka ad hoc.
