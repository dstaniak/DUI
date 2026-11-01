# DUI

Edukacyjny prototyp dla dorosłych dotyczący jazdy pod wpływem alkoholu,
skierowany również do osób, które faktycznie tak jeżdżą. Ton gry ma być
przystępny i zabawny, bez zawstydzania odbiorcy.

Pierwsza gra wykorzystuje mechanikę snake’a z tutorialu DungeonSlime:
gracz prowadzi czerwony samochód i zbiera losowane piwo, wódkę, proszek, grzybki
oraz kiełbaskę. Alkohol i proszek wydłużają kolumnę radiowozów, zakąska zmniejsza
poziom alkoholu, a powerupy tymczasowo zmieniają efekty i sterowanie.

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

## Oprawa retro

Cała klatka, łącznie z UI, ma **1280×720 pikseli**. Kafelek zajmuje
80×80 pikseli, a pas witryny 1280×160, dzięki czemu widać kształty samochodów,
butelki, przekąski i cieniowanie grafik. `PointClamp` zachowuje ostre
piksele przy skalowaniu. Obraz wypełnia całe okno bez ramki i pasów.
Przy innych proporcjach okna każda oś jest dopasowywana osobno; dla
naturalnych proporcji najlepszy jest widok 16:9. Fizyka, prędkość i granice
kolizji pozostają w jednostkach planszy.

Poziom alkoholu w promilach steruje shaderem podwójnego widzenia na warstwie planszy.
UI jest rysowane osobno i nakładane po wszystkich efektach świata. Przy 0,0‰ lub aktywnym proszku shader jest pomijany.
Powyżej zera, bez proszku, kopia obrazu
zatacza koło wokół środka okna w ciągu 8 sekund; sam obraz nie zmienia
orientacji. Alfa rośnie od bazowego 0,1 o 0,01 na każde 0,1‰, do maksimum 0,5:
przy 0,2‰ wynosi 0,12, przy 1‰ — 0,2, a przy 2‰ — 0,3. Od 4‰ pozostaje 0,5.
Promień rośnie liniowo: 12 pikseli na 1‰ przy
720p, czyli 2,4 piksela za piwo i 4,8 za wódkę; kiełbaska zmniejsza efekt.
Promień skaluje się z krótszym bokiem okna,
a ruch pozostaje kołowy także przy innych proporcjach. Tylko kopia używa
filtrowania liniowego dla płynnych przesunięć mniejszych niż piksel;
oryginał zachowuje `PointClamp`. Poza krawędziami przesuniętej kopii widać
sam oryginał. Pauza i koniec gry zatrzymują orbitę; RETRY zeruje efekt.

Retro wygląd tworzą same grafiki: uproszczone kształty, pikselowy font,
kolory inspirowane C64 i wybrane dwukolorowe wzory w cieniach.
Każdy segment wystawy zawiera 12 smukłych butelek. Ich boki mają
naprzemienne, zbliżone ciemniejsze odcienie własnego koloru — na czerwonych
ciemniejszą czerwień, bez czarnych wcięć. Środki pozostają jednolite.
Auta i zbierana butelka piwa są przerysowane jako proste ikony z dużych
bloków koloru, z pojedynczym cieniem, bez połysku, odbić i drobnych obwódek.
To uproszczenie kompozycji sprite’ów, bez filtra zmniejszającego rozdzielczość.
Wzory są częścią tekstur; ekran nie nakłada
ditheringu ani nie ogranicza całej klatki do 16 kolorów. Oryginalne odcienie
tekstur, przezroczystość i wygaszanie podczas pauzy zachowują szczegóły.
`C64Palette` dostarcza czerń oprawy i biel małych refleksów belki z palety
[Pepto PAL używanej przez VICE](https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/C64/pepto-pal.vpl).

Witryna jest składana z elementów skalowanych jednakowo w obu osiach.
Oryginał 2172×724 tworzy środkową fasadę 480×160 z wejściem i szyldem;
powtarzane segmenty boczne wypełniają pozostałą szerokość. Drzwi, litery
i towary zachowują proporcje, a pas sklepu nadal zajmuje dwa górne kafelki.

UI ma układ logiczny 320×180 i zoom 4, rysowany w osobnym przezroczystym
buforze 720p, skalowanym tak samo jak plansza. Licznik, przyciski i panele
pauzy oraz końca gry nie podlegają podwójnemu widzeniu, tęczy ani drżeniu. Całe UI używa ekranowego fontu C64 na siatce 8×8:
cyfry promili, znak ‰, PAUSE, RESUME, RETRY i nagłówki paneli.
Pasek ma tło C64 Orange `#6F4F25`, żółty tekst i ramkę Yellow `#B8C76F`.
Panele pauzy i końca gry mają tę samą paletę i prostokątne ramki.
Przyciski są płaskie: fokus lub najechanie daje pomarańczowy tekst na żółtym tle,
a naciśnięcie rozjaśnia tło do bieli. Białe ikony i wypełnienie gwiazdek
zachowują kontrast; puste gwiazdki używają jasnej szarości C64 `#959595`.
Wszystkie kolory UI są przypisane do ról w `C64Theme`: tło, tekst, akcent,
naciśnięcie, przygaszone elementy i symbole. Kolejny wariant wymaga tylko
zmiany tych przypisań i ponownego uruchomienia gry.
Bitmapowy font skaluje się bez wygładzania: standardowy znak ma 32×32 px,
a nagłówki paneli 64×64 px przy obrazie 720p.

## Zasady i sterowanie

- Strzałki / WASD lub D-pad / lewy drążek pada zmieniają kierunek.
- Auto jedzie z prędkością 400 jednostek planszy na sekundę; proszek
  zwiększa ją na 10 sekund do 480 (1,2×), grzybek zmniejsza do 200 (0,5×),
  a oba efekty razem dają 240 (0,6×).
  Proste odcinki przechodzą przez środki kafelków 80×80. Skręt czeka na
  najbliższą krawędź kafelka, po czym łączy osie rzędów i kolumn łukiem
  o promieniu 40 px. Zmiana kierunku o 90° trwa około 157 ms czasu symulacji
  przy stałej prędkości. Późne polecenie czeka na kolejną krawędź; auto
  nie cofa się ani nie przeskakuje na sąsiedni tor. Dwa łuki nawrotu
  zmieniają rząd dokładnie o 80 px, bez narastającego przesunięcia.
- Kolejka przechowuje dwa oczekujące skręty. Rozpoczęty manewr zawsze
  kończy się przed następnym. Kierunek jest sprawdzany względem ostatniego
  zaplanowanego celu; natychmiastowy zwrot, powtórzenie celu i przepełnienie
  są pomijane. Sekwencja „prawo → dół → lewo” wykonuje płynny nawrót.
- Segmenty odtwarzają pozycje i obroty głowy z jej śladu, w odstępach 80
  jednostek wzdłuż drogi. Nowe auto pojawia się za ogonem, bez przesuwania
  pozostałych. Symulacja używa podkroków 5 ms i interpolacji obrazu.
- Każdy segment ogona jest srebrno-niebieskim radiowozem. Światła na dachu
  mają dwa stany: jaskrawoniebieski i czerwony, zmieniane co 250 ms.
  Lampy są nieprzezroczystymi blokami 8×12 z małym białym refleksem,
  więc kolor błysku pozostaje wyraźny. Belka obraca się,
  skaluje i podskakuje razem z autem. Pauza i koniec gry zamrażają światła,
  a RETRY zaczyna od stanu niebieskiego. Auto gracza pozostaje czerwone.
- Delikatne falowanie ma amplitudę ±1,5 jednostki i ±2°, z okresem 1,2 s.
  Faza zależy od przebytej drogi, więc każdy segment powtarza ruch głowy.
  To efekt wizualny; pauza go zatrzymuje, a RETRY zeruje fazę.
- Kolizje z ogonem używają ruchomych okręgów o promieniu 32, z pominięciem
  bezpośredniego sąsiada głowy. Są sprawdzane także pomiędzy końcami podkroku.
  Przy ścianach sprawdzany jest obrys auta 74×36, obracany razem z jego
  kierunkiem, bez przezroczystego marginesu tekstury. Zbieranie przedmiotów
  zachowuje promień głowy 40. Granica kolizji biegnie przy widocznym
  ogrodzeniu (x=46–1234, dół y=666), a nie na krawędzi jego kafelków.
  Zamknięta brama zajmuje x=560–720 i wystaje do y=654; jej narożniki
  są sprawdzane względem obróconego auta. Witryna kończy się na y=160.
- Na planszy jest jeden pulsujący przedmiot, poruszający się i odbijający
  od granic parkingu. Na początku, po RETRY i po każdym zebraniu losowany
  jest następny: **50% piwo, 20% kieliszek wódki, 5% proszek,
  5% grzybek, 20% kiełbaska**.
  Piwo dodaje 0,2‰ i samochód, a przy zebraniu rozpryskuje piwo i pianę.
  Wódka dodaje 0,4‰ i samochód. Kiełbaska odejmuje 0,4‰, do minimum 0‰,
  i nie zmienia długości kolumny. Licznik zaczyna od `0,0‰` i pokazuje
  jedną cyfrę po przecinku; znak `‰` jest glifem tej samej czcionki C64.
  Kolejny przedmiot pojawia się po przeciwnej stronie parkingu.
  Witryna, ogrodzenie, zamknięta brama lub własna kolumna aut kończą grę.
- Biały proszek w woreczku strunowym dodaje jeden radiowóz i przez
  **10 sekund** wyłącza podwójne widzenie, drżenie planszy oraz tęczę.
  Jednocześnie przyspiesza symulację kolumny do **1,2×**; odstępy między
  autami zostają takie same, a skręty i animacje aut podążają za jej zegarem.
  Nie zmienia promili ani odwróconego sterowania; przy aktywnym grzybku
  mnożniki prędkości łączą się do 0,6×.
- Grzybek z ostrym kapeluszem i cienką nóżką przez **10 sekund** odwraca
  wszystkie kierunki (lewo ↔ prawo, góra ↔ dół), na klawiaturze, padzie
  oraz dla gestów. Jednocześnie spowalnia całą kolumnę do **0,5×**
  (200 px/s), dając więcej czasu na reakcję. Nie zmienia promili ani długości kolumny.
  `rainbowEffect` nakłada na całą warstwę planszy, poniżej UI, tęczowy gradient
  z alfą 0,22. Gradient przesuwa się płynnie, wykonując cykl co 6 sekund.
- Ponowne zebranie tego samego powerupu odnawia jego czas do 10 sekund.
  Timery obu powerupów, drżenia i shaderów używają zwykłego czasu gry,
  niezależnego od przyspieszenia lub spowolnienia kolumny. Pauza i przegrana je zamrażają,
  RETRY usuwa efekty i przywraca normalne sterowanie oraz prędkość.
  Proszek ukrywa tęczę i drżenie, ale ich czas nadal upływa. Spowolnienie
  i odwrócone sterowanie grzybka pozostają aktywne. Jeśli po
  wygaśnięciu proszku dany efekt nadal trwa, ponownie staje się widoczny.
- Za wynikiem i znakiem ‰ są trzy stałe miejsca na ikonki: wódka, proszek,
  grzybek. To osobne białe sprite’y 28×28 px z przezroczystym tłem,
  dopasowane do pikselowego licznika. Ikonka aktywnego przedmiotu mruga: 0,3 s widoczna, 0,3 s ukryta.
  W ostatniej sekundzie każdego efektu mruga trzy razy szybciej: 0,1 s widoczna,
  0,1 s ukryta, zaczynając od widocznego błysku na granicy ostatniej sekundy.
  Odliczanie pochodzi z tego samego timera co efekt (3 s / 10 s / 10 s).
  Ponowne zebranie zaczyna od widocznej ikonki, wygaśnięcie i RETRY ją gaszą,
  a pauza i koniec gry zamrażają mruganie. Wskaźniki pokazują aktywne timery
  również wtedy, gdy proszek chwilowo ukrywa ich efekty wizualne.
  Wszystkie trzy przesuwają się za promilami, gdy rośnie liczba cyfr licznika.
- Wynikiem gry jest liczba segmentów kolumny, razem z autem gracza.
  Za trzema slotami powerupów jest pięć białych gwiazdek 28×28 px.
  Ich jasnoszare tło z palety C64 (#959595) jest zawsze widoczne; białe wypełnienie narasta
  od lewej do prawej w skali 0–84 segmentów, z dokładnością do jednego
  piksela obrazu 720p. Odstępy między gwiazdkami nie wliczają się do postępu.
  Start i RETRY dają 1 punkt za głowę, 42 segmenty to 2,5 gwiazdki,
  a 84 lub więcej — pełne pięć. Piwo, wódka i proszek dodają punkt;
  kiełbaska oraz grzybek nie zmieniają wyniku. Promile pozostają osobnym
  licznikiem. Gwiazdki nie mrugają i nie podlegają efektom świata.
- Wódka uruchamia mocne drżenie kafelków i sprite’ów na planszy w lewo
  i prawo: ±24 piksele przy 720p, 18 cykli na sekundę, przez 3 sekundy.
  UI jest rysowane osobno i nie drży. Kolejna wódka odnawia czas;
  piwo i kiełbaska nie przerywają drżenia. Pauza i przegrana zatrzymują
  jego fazę oraz odliczanie, a RETRY je zeruje. Drżenie jest wizualne
  i nie zmienia kolizji; odsłonięty skraj planszy jest czarny.
- Plansza jest parkingiem pod sklepem z alkoholem i przekąskami. Witryna
  zajmuje dwa górne rzędy; po bokach i na dole jest ogrodzenie z siatki,
  a pośrodku dolnego ogrodzenia zamknięta brama szerokości dwóch kafelków.
  Dolne narożniki mają kątowniki „L” łączące boczną siatkę z dolnym przęsłem.
- Dyskretna siatka pokazuje granice kafelków 80×80 na asfalcie i trawnikach.
  Szare linie C64 `#959595` mają 1 piksel grubości i krycie 35%, także na
  skrzyżowaniach. Kończą się przy ogrodzeniu i bramie, są pod autami oraz
  przedmiotami i podlegają tym samym efektom co plansza.
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
  RESUME wznawia, RETRY rozpoczyna nową rozgrywkę z 1 punktem i `0,0‰`.
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
  granice po każdym podkroku. Rysuje czerwone auto gracza oraz radiowozy
  w ogonie. `SegmentCount` udostępnia wynik razem z głową. `PoliceLightbar`
  nakłada dwuklatkowe światła na dach, używając czasu modelu i tej samej transformacji co auto; posiada i zwalnia teksturę
  jednego piksela. `CarSway` dodaje małe wizualne falowanie.
- `Easings` — statyczne funkcje interpolacji (30 krzywych oraz `Linear`);
  `CarHop` używa `EaseOutCubic` i `EaseInQuad` do wybicia oraz lądowania.
  `ConvoyMovement` wyznacza styczne łuki geometrycznie, zachowując stałą prędkość.
- `ParkingLot` — układ parkingu, granice jazdy, składanie proporcjonalnej
  witryny z centralnej fasady i powtarzanych segmentów wystawy oraz
  wspólna geometria kafelków trawy i płynnego powiększania aut do 106%.
  Rysuje też półprzezroczystą siatkę pól; posiada teksturę jednego piksela,
  zwalnianą przez `GameScene.UnloadContent`. `GridOpacity` i `GridLineWidth`
  określają wygląd siatki.
  `ParkingGeometry` wyznacza krawędzie ogrodzenia, osobny obrys bramy
  i prostokątny obszar ruchu przedmiotów, który mieści się przed bramą.
  `CarHop` ma osobny stan dla każdego auta i wykrywa przekroczenie
  krawężnika przez jego przód; renderowanie korzysta z czasu modelu ruchu.
- `Pickup` — wspólny ruch, pulsowanie i kolizje wszystkich pięciu przedmiotów.
  `PickupType` rozróżnia ich rodzaj, a `PickupRules` określa losowanie,
  zmianę promili i wydłużanie kolumny. `BeerBurst` rysuje krople i pianę
  wyłącznie po piwie. `ScreenShake` odmierza 3 sekundy drżenia po wódce;
  `GameScene` przesuwa tylko partię rysowania planszy, przed UI Gum.
- `GameDisplay` — mapuje planszę i klatkę 1280×720 na całe okno oraz
  określa logiczny układ UI 320×180 przy zoomie 4. Odwrotna transformacja
  obsługuje gesty oraz kursor Gum, aż do krawędzi widocznej planszy.
- `RetroRenderer` — posiada dwa bufory 1280×720: świata i przezroczystego UI.
  Nakłada podwójne widzenie i tęczę na świat, a dopiero potem komponuje UI
  bez efektów. Obie warstwy skaluje przez `PointClamp` do tego samego obszaru.
  `DuiGame` przekazuje promile, czas aktywnej gry i flagi powerupów; przełącza
  bufory między `GameScene.Draw` i `DrawUI`, a przy wyjściu zwalnia oba.
  Shadery należą do głównego `ContentManager`.
- `C64Theme` — wspólna czcionka, role kolorów i prostokątne powierzchnie z palety C64.
  `C64Button` zachowuje obsługę wejścia Gum, używając własnych kolorów
  fokusu, najechania, naciśnięcia i wyłączenia.
- `ScoreStars` — pięć przyciemnionych gwiazdek z białym wypełnieniem
  przycinanym do całych pikseli na podstawie `GameScene.Score` (długości kolumny).
  `GameSceneUI` ustawia je za slotami powerupów w osobnej warstwie UI.
- `DUI.Tests` — testy regresji uruchamiane bez okna i urządzenia graficznego.
- [Mapa klas](Docs/ClassMap.html) — odpowiedzialności i przepływy aplikacji.

## Mobile

Kod uwzględnia gesty dotykowe, pauzę po utracie aktywności oraz stałą
planszę i klatkę 1280×720 dopasowaną do całego okna. Zmiana rozmiaru
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

57 testów obejmuje oczekiwanie na krawędź kafelka i spóźnione polecenie,
500 kolejnych skrętów bez zejścia z osi siatki, wszystkie kierunki skrętów,
kolejkę, odrzucanie nieprawidłowych poleceń, stałą prędkość,
interpolację i różne podziały czasu. Sprawdzają odtwarzanie śladu przez
cztery zakręty, odstępy, wzrost, ograniczenie pamięci historii, ciasny nawrót,
przecięcie ogona między podkrokami, granice parkingu, pauzę i restart.
Testy animacji obejmują ograniczone falowanie, wjazd z czterech stron
oraz po skosie, wybicie i lądowanie, brak ponownego podskoku podczas
animacji i jazdy po trawie, ponowny wjazd oraz opóźnienie segmentów.
Trzy regresje ścian sprawdzają ciasne skręty przy czterech krawędziach,
obrócone boki i narożniki auta oraz uderzenia po minięciu ostatniego
bezpiecznego miejsca rozpoczęcia skrętu.
Cztery testy geometrii parkingu obejmują asfalt wewnątrz kafelków płotu,
mijanie narożników bramy ukośnym autem, osobne krawędzie bramy i przęsła
oraz bezpieczny obszar ruchu przedmiotów.
Trzy testy `GameDisplayTests` sprawdzają brak ramki i przycinania, pełne
wypełnienie okna, zgodność kursora Gum przy zoomie 4 z wejściem planszy po zmianie
rozmiaru (także nieparzystym i bardzo małym) oraz odrzucanie gestów
z HUD i spoza planszy. Pozostają też testy gestów i kolizji z butelkami.
Program testowy zwraca kod różny od zera, jeśli dowolny test zawiedzie.
Trzy testy `PickupTests` sprawdzają dokładne proporcje 50/20/5/5/20, mieszane
sekwencje punktacji i wzrostu oraz dolny limit 0‰ po zakąsce. Cztery testy
`ScreenShakeTests` sprawdzają amplitudę i tempo, czas 3 sekund, pauzę,
reset, ponowne wyzwolenie oraz niezależność od podziału czasu.
Jedenaście testów `PowerupEffectsTests` sprawdza kierunki, oba czasy 10 sekund,
odnawianie, nakładanie efektów, reset i podział czasu oraz prędkości 480, 200 i 240 px/s,
odstępy aut, zwykłe tempo timerów, niezależne wygasanie mnożników w obu kolejnościach,
część klatki po wygaśnięciu i ułamki ticków.
Renderowanie, dźwięk, interakcje Gum i cykl życia aplikacji wymagają też
sprawdzenia w uruchomionej grze, w tym niezmienionych kolorów i położenia
UI przy aktywnych efektach i po zmianie rozmiaru okna, a także mrugania,
wygasania i położenia trzech wskaźników za wynikiem; te testy nie
potwierdzają działania na telefonie.

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
[Grafika radiowozu](DUI.Game/Content/Images/police-car.png) to wariant
uproszczonego auta wygenerowany wbudowanym `image_gen`, z zachowaniem
sylwetki, orientacji i przezroczystości. Oba auta mają mniej detali,
większe bloki koloru i jednolite szyby bez refleksów w klimacie C64.
Dach radiowozu jest pusty w teksturze; światła rysuje kod gry.
Zbierana butelka ma płaskie szkło, pojedynczy cień i prostą etykietę.
Nowe [kieliszek wódki](DUI.Game/Content/Images/vodka-shot.png) i
[kiełbaska](DUI.Game/Content/Images/sausage.png) powstały we wbudowanym
`image_gen` jako osobne przezroczyste sprite’y retro. Ich prompty również
są zapisane w `AssetPrompts.json`.
[Woreczek proszku](DUI.Game/Content/Images/powder-bag.png) i
[grzybek](DUI.Game/Content/Images/mushroom.png) również powstały we wbudowanym
`image_gen`, jako przezroczyste pikselowe sprite’y. Prompty są w tym samym pliku.

Aktywny [atlas parkingu C64](DUI.Game/Content/Images/c64-parking-atlas.png) zawiera
16 kafelków asfaltu, linii, krawężników, trawy, siatki i bramy.
[Witryna C64](DUI.Game/Content/Images/c64-storefront.png) jest osobną panoramą.
Oba zasoby i ich warianty z wzorami powstały we wbudowanym `image_gen`; prompty zapisano w tym samym
pliku AssetPrompts.json. Definicja mapy wycina wnętrza kafelków bez linii
podziału atlasu; `ParkingLot` skaluje je do logicznych 80×80.
Oryginalne parking-atlas.png i storefront.png pozostają materiałem źródłowym.
Tutorialowy font i atlas UI zostały zastąpione motywem C64.
Narożniki są składane z fragmentów pionowego i poziomego przęsła tego samego
atlasu, dzięki czemu zachowują rozstaw szyn oraz wzór siatki. Na styku jest
[kątownik](DUI.Game/Content/Images/fence-corner.png) wycięty podczas rysowania
z grafiki wygenerowanej we wbudowanym `image_gen`. Całe połączenie jest
odbijane lustrzanie po prawej stronie. Prompt grafiki jest w AssetPrompts.json.

[Czcionka UI C64](DUI.Game/Content/Fonts/c64.fnt) korzysta z
[fontu C64 autorstwa zeiche](https://github.com/zeiche/c64font), udostępnionego
na CC0. [Źródło i licencja](SourceAssets/Fonts/README.md) są w repozytorium.
Skrypt `SourceAssets/Fonts/build_font.py` tworzy atlas bez antyaliasingu:
ASCII 32–95 oraz dodatkowy znak ‰, dopasowany do pikselowej siatki.
Gra wczytuje gotowy bitmapowy font; nie wymaga instalowania czcionki w systemie.
Promile używają `decimal`, aby przyrosty o 0,2 były dokładne.

Wskaźniki aktywnych efektów mają osobne, białe sprite’y 28×28:
[kieliszek](DUI.Game/Content/Images/UI/vodka-indicator.png),
[woreczek](DUI.Game/Content/Images/UI/powder-indicator.png) i
[grzybek](DUI.Game/Content/Images/UI/mushroom-indicator.png).
Ich źródłowe SVG są w `SourceAssets/UI/`. Powstały z pikselowych prostokątów
na siatce 14×14, powiększonych dwukrotnie bez antyaliasingu; używają tylko
bieli i przezroczystości, podobnie jak atlas nowego fontu.
