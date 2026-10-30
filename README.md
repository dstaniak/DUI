# DUI

Prototyp zestawu gier. Pierwsza gra to snake przeniesiony z lokalnego
projektu DungeonSlime opartego na tutorialu MonoGame.

## Uruchomienie

Wymagany .NET SDK 10. Z katalogu repozytorium:

```sh
dotnet tool restore
dotnet run --project DUI.Desktop
```

Gra otwiera od razu planszę. Projekt DesktopGL służy do iteracji na macOS,
Windows i Linux. MGCB buduje zasoby podczas kompilacji. Kompilacja shadera
na macOS/Linux wymaga środowiska Wine skonfigurowanego dla MonoGame MGFXC,
tak jak w projekcie źródłowym.

## Zasady i sterowanie

- Strzałki / WASD lub D-pad / lewy drążek pada zmieniają kierunek.
- Kolejka przechowuje dwa przyszłe ruchy, wykonywane po jednym co 200 ms.
  Kierunek jest sprawdzany względem ostatniego ruchu w kolejce.
  Można więc zaplanować „prawo → góra → lewo”, ale nie natychmiastowy zwrot.
  Kolejny ruch przy pełnej kolejce jest pomijany, tak jak w DungeonSlime.
- Pozycja jest płynnie interpolowana między polami, a segmenty podążają
  za głową. Nietoperz porusza się i odbija od ścian.
- Zebranie nietoperza dodaje segment i 100 punktów; nietoperz pojawia się
  po przeciwnej stronie pokoju. Ściana lub własne ciało kończy grę.
- Escape / Start / Back lub przycisk PAUSE pauzuje grę.
  RESUME wznawia, RETRY rozpoczyna nową rozgrywkę z wynikiem 0.
  Przyciski obsługują także Enter / A na padzie.
- Gest przesunięcia na planszy trafia do tej samej kolejki. Na komputerze
  można go sprawdzić przeciągnięciem lewego przycisku myszy.
  Jeden kontakt oznacza jeden ruch; kolejny kontakt pozwala zaplanować następny.
- Utrata aktywności aplikacji pauzuje grę. Powrót wymaga świadomego wznowienia.

Nie ma menu głównego ani przycisków powrotu do niego. Okno desktopowe zamyka
się standardowym przyciskiem systemu.

## Struktura

- `DUI.Desktop` — punkt startowy i zależność od backendu DesktopGL.
- `DUI.Game` — wspólny kod gry, UI Gum, zasoby i potrzebne części biblioteki
  tutorialowej. `DUI.Game.projitems` dołącza źródła do projektu platformowego,
  dzięki czemu przyszły host mobilny może użyć własnego backendu MonoGame.
- `SlimeMovement` — kolejka, segmenty i interpolacja niezależne od grafiki
  oraz urządzenia wejściowego; `Slime` odpowiada za animację i rysowanie.
- `DUI.Tests` — testy regresji uruchamiane bez okna i urządzenia graficznego.
- [Mapa klas](Docs/ClassMap.html) — odpowiedzialności i przepływy aplikacji.

## Mobile

Kod uwzględnia gesty dotykowe, pauzę po utracie aktywności oraz stałą
planszę 1280 × 720, skalowaną z zachowaniem proporcji. Zmiana rozmiaru
ekranu nie zmienia zasad, geometrii kolizji ani prędkości gry. Aktualizacje
odbywają się w stałym rytmie 60 Hz, także na ekranach o wyższym odświeżaniu.

Pierwszy wariant mobilny warto przygotować w orientacji poziomej, aby zachować
układ tutorialu. Projekt nie zawiera jeszcze hostów Android/iOS ani paczek
APK/IPA. Następny etap to wybranie pierwszej platformy, dodanie jej hosta,
SDK, obszarów bezpiecznych dla wycięć ekranu i testy na fizycznym urządzeniu.
Gest i wielkości przycisków wymagają wtedy oceny na rzeczywistym telefonie.
Wersja pionowa skaluje dziś planszę; osobny układ pionowy wymaga decyzji
o nowej wizualizacji.

MonoGame udostępnia osobne backendy Android i iOS:
[obsługiwane platformy](https://docs.monogame.net/articles/getting_started/platforms.html),
[wejście dotykowe](https://docs.monogame.net/articles/getting_to_know/howto/input/HowTo_UseMultiTouchInput.html).

## Weryfikacja

```sh
dotnet build DUI.slnx
dotnet run --project DUI.Tests
```

Testy obejmują kolejkę i jej pojemność, zakaz zawracania, powtarzane
kierunki, czas ruchu i interpolację, wzrost, kolizję z ciałem, świeży stan
nowej gry, gesty i ich anulowanie, skalowanie oraz okrągłe obszary kolizji.
Program testowy zwraca kod różny od zera, jeśli dowolny test zawiedzie.
Renderowanie, dźwięk, interakcje Gum i cykl życia aplikacji wymagają też
sprawdzenia w uruchomionej grze; te testy nie potwierdzają działania na telefonie.

## Pochodzenie

Kod i zasoby przeniesiono z lokalnego DungeonSlime, stan
`ec41fb7eefb4787a652538e37bc0eb0d025998dd`. Zachowano zasady, tempo,
grafikę i dźwięki tutorialu. Pominięto ekran tytułowy, jego opcje i zasoby,
nakładkę ImGui oraz desktopowy mechanizm obserwowania shaderów.
Efekt wygaszania kolorów podczas pauzy i końca gry pozostał.

Tutorial: [Building a 2D Game — MonoGame](https://docs.monogame.net/articles/tutorials/building_2d_games/).
