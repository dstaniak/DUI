# DUI

Edukacyjny prototyp dla dorosłych dotyczący jazdy pod wpływem alkoholu,
skierowany również do osób, które faktycznie tak jeżdżą. Ton gry ma być
przystępny i zabawny, bez zawstydzania odbiorcy.

Pierwsza gra wykorzystuje mechanikę snake’a z tutorialu DungeonSlime:
gracz prowadzi kolumnę samochodów i zbiera pulsujące butelki piwa.

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
- Auto jedzie ze stałą prędkością 400 jednostek planszy na sekundę.
  Skręt zaczyna się bez czekania na kafelek, trwa 160 ms i zmienia kierunek
  o 90° przez `EaseInOutSine`. Samochód porusza się po rzeczywistej krzywej.
- Kolejka przechowuje dwa oczekujące skręty. Rozpoczęty manewr zawsze
  kończy się przed następnym. Kierunek jest sprawdzany względem ostatniego
  zaplanowanego celu; natychmiastowy zwrot, powtórzenie celu i przepełnienie
  są pomijane. Sekwencja „prawo → dół → lewo” wykonuje płynny nawrót.
- Segmenty odtwarzają pozycje i obroty głowy z jej śladu, w odstępach 80
  jednostek wzdłuż drogi. Nowe auto pojawia się za ogonem, bez przesuwania
  pozostałych. Symulacja używa podkroków 5 ms i interpolacji obrazu.
- Delikatne falowanie ma amplitudę ±1,5 jednostki i ±2°, z okresem 1,2 s.
  Faza zależy od przebytej drogi, więc każdy segment powtarza ruch głowy.
  To efekt wizualny; pauza go zatrzymuje, a RETRY zeruje fazę.
- Kolizje z ogonem używają ruchomych okręgów o promieniu 32, z pominięciem
  bezpośredniego sąsiada głowy. Są sprawdzane także pomiędzy końcami podkroku.
  Granice parkingu i zbieranie butelek zachowują promień głowy 40.
- Butelka piwa pulsuje, porusza się i odbija od granic parkingu. Jej zebranie
  wywołuje rozprysk złotych kropli i białej piany, dodaje samochód oraz
  0,2‰ do wyniku. Licznik zaczyna od `0,0‰` i pokazuje jedną cyfrę po
  przecinku; cały znak `‰` jest osobnym pikselowym sprite’em.
  Kolejna butelka pojawia się po przeciwnej stronie parkingu.
  Witryna, ogrodzenie, zamknięta brama lub własna kolumna aut kończą grę.
- Plansza jest parkingiem pod sklepem z alkoholem i przekąskami. Witryna
  zajmuje dwa górne rzędy; po bokach i na dole jest ogrodzenie z siatki,
  a pośrodku dolnego ogrodzenia zamknięta brama szerokości dwóch kafelków.
  Dolne narożniki mają kątowniki „L” łączące boczną siatkę z dolnym przęsłem.
- Dwie symetryczne wysepki trawnika mają po 2×4 kafelki. Można po nich
  przejechać: gdy przód auta przekracza krawężnik z asfaltu na trawę,
  auto podskakuje o 12
  jednostek planszy i powiększa się do około 128%. Wybicie korzysta
  z `EaseOutCubic`, a lądowanie z `EaseInQuad`; cały podskok trwa 200 ms.
  Po lądowaniu zostaje skala 106%, a wyjazd przywraca 100%.
  Jazda po trawie, wyjazd i aktywna animacja nie uruchamiają nowego podskoku. Pauza zatrzymuje go
  w bieżącej fazie, a RETRY rozpoczyna nową animację ruchu.
  Efekt jest liczony osobno dla każdego segmentu, bez zmiany
  ruchu ani kolizji. Nad i pod trawnikami pozostaje po jednym rzędzie przejazdu.
- Pulsowanie jest wyłącznie wizualne; rozmiar obszaru kolizji pozostaje stały.
  Pauza zatrzymuje też rozprysk, a RETRY usuwa pozostałe cząsteczki.
- Escape / Start / Back lub przycisk PAUSE pauzuje grę.
  RESUME wznawia, RETRY rozpoczyna nową rozgrywkę z wynikiem `0,0‰`.
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
- `ConvoyMovement` — kolejka, skręty, stały podkrok oraz kolizje z ogonem.
  `CarPose` przechowuje środek, obrót i drogę auta; `ConvoyTrail` ogranicza
  historię do długości kolumny i zapasu na nowy segment.
  `ConvoyCollision` sprawdza ruchome okręgi oraz granice parkingu.
- `CarConvoy` — łączy model z rysowaniem, aktualizuje podskoki i sprawdza
  granice po każdym podkroku. `CarSway` dodaje małe wizualne falowanie.
- `Easings` — statyczne funkcje interpolacji (30 krzywych oraz `Linear`);
  `ConvoyMovement` używa `EaseInOutSine` do skrętów, a `CarHop` używa
  `EaseOutCubic` i `EaseInQuad` do wybicia oraz lądowania.
- `ParkingLot` — układ parkingu, granice jazdy, rysowanie witryny oraz
  wspólna geometria kafelków trawy i płynnego powiększania aut do 106%.
  `CarHop` ma osobny stan dla każdego auta i wykrywa przekroczenie
  krawężnika przez jego przód; renderowanie korzysta z czasu modelu ruchu.
- `BeerBottle` — ruch i pulsowanie butelki; `BeerBurst` — krótkotrwałe
  krople piwa i piana, rysowane prostymi figurami pikselowymi.
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

27 testów obejmuje natychmiastową reakcję między kafelkami, wszystkie
kierunki skrętów, kolejkę, odrzucanie nieprawidłowych poleceń, stałą prędkość,
interpolację i różne podziały czasu. Sprawdzają odtwarzanie śladu przez
cztery zakręty, odstępy, wzrost, ograniczenie pamięci historii, ciasny nawrót,
przecięcie ogona między podkrokami, granice parkingu, pauzę i restart.
Testy animacji obejmują ograniczone falowanie, wjazd z czterech stron
oraz po skosie, wybicie i lądowanie, brak ponownego podskoku podczas
animacji i jazdy po trawie, ponowny wjazd oraz opóźnienie segmentów.
Pozostają też testy gestów, mapowania ekranu i kolizji z butelkami.
Program testowy zwraca kod różny od zera, jeśli dowolny test zawiedzie.
Renderowanie, dźwięk, interakcje Gum i cykl życia aplikacji wymagają też
sprawdzenia w uruchomionej grze; te testy nie potwierdzają działania na telefonie.

## Pochodzenie

Kod i zasoby przeniesiono z lokalnego DungeonSlime, stan
`ec41fb7eefb4787a652538e37bc0eb0d025998dd`. Zachowano tempo,
rozmiar planszy, UI i dźwięki tutorialu; ruch po siatce zastąpiły płynne skręty. Postacie zastępują nowe grafiki samochodu
i butelki; efekt rozprysku piwa powstaje w kodzie. Loch zastępuje parking.
Pominięto ekran tytułowy, jego opcje i zasoby,
nakładkę ImGui oraz desktopowy mechanizm obserwowania shaderów.
Efekt wygaszania kolorów podczas pauzy i końca gry pozostał.

Tutorial: [Building a 2D Game — MonoGame](https://docs.monogame.net/articles/tutorials/building_2d_games/).

Grafiki [samochodu](DUI.Game/Content/Images/car.png) i
[butelki](DUI.Game/Content/Images/beer-bottle.png) wygenerowano narzędziem
`image_gen` z przezroczystym tłem. Pełne prompty: [AssetPrompts.json](Docs/AssetPrompts.json).

Nowy [atlas parkingu](DUI.Game/Content/Images/parking-atlas.png) zawiera
16 kafelków asfaltu, linii, krawężników, trawy, siatki i bramy.
[Witryna](DUI.Game/Content/Images/storefront.png) jest osobną panoramą.
Oba zasoby powstały we wbudowanym `image_gen`; prompty zapisano w tym samym
pliku AssetPrompts.json. Definicja mapy wycina wnętrza kafelków bez linii
podziału atlasu; `ParkingLot` skaluje je do logicznych 80×80.
Dotychczasowy atlas pozostaje źródłem fontu i elementów UI.
Narożniki są składane z fragmentów pionowego i poziomego przęsła tego samego
atlasu, dzięki czemu zachowują rozstaw szyn oraz wzór siatki. Na styku jest
[kątownik](DUI.Game/Content/Images/fence-corner.png) wycięty podczas rysowania
z grafiki wygenerowanej we wbudowanym `image_gen`. Całe połączenie jest
odbijane lustrzanie po prawej stronie. Prompt grafiki jest w AssetPrompts.json.

Cały [znak promila](DUI.Game/Content/Images/per-mille.png) jest pojedynczą
teksturą 42×29 z przezroczystym tłem. Narysowano go jako pikselowe figury
w [źródłowym SVG](SourceAssets/UI/per-mille.svg) i zrasteryzowano do PNG.
UI dopasowuje go do skali fontu i przesuwa za liczbą przy zmianie jej szerokości.
Punktacja używa `decimal`, aby przyrosty o 0,2 były dokładne.
