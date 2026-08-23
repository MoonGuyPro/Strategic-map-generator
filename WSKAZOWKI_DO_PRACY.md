# Wskazówki do pisania pracy magisterskiej

Notatka robocza. Zbiera to, co wyszło z dotychczasowej pracy nad projektem i podpowiada,
na czym warto oprzeć narrację pracy.

---

## 1. Co jest najmocniejszą kartą tej pracy

Praca zaczęła się jako odtworzenie metody z artykułu Lara-Cabrera i in. dla innej gry.
W trakcie okazało się, że **odtworzenie się nie udaje — i to jest najciekawsze**.

Trzy rzeczy, których nie da się przeczytać w artykule, a które masz zmierzone:

1. **Metryki z artykułu nie przenoszą się bezpośrednio na inną grę.** Trzy z siedmiu trzeba było
   przedefiniować albo wycofać, bo w tej mechanice mierzyły co innego, niż zakładali autorzy.
2. **Progi funkcji przynależności wzięte „z sensu" nie działają.** Zanim je skalibrowałeś,
   kryterium dynamizmu rozróżniało mapy w zakresie 0,02 na skali 0–1. Po kalibracji — 0,70.
3. **Balans i dynamizm w tej grze nie są sprzeczne, tylko idą w parze.** W artykule były sprzeczne.

To nie są porażki wdrożenia. To są wyniki badawcze i wokół nich zbudowałbym całą pracę.

---

## 2. Proponowana struktura rozdziałów

**1. Wstęp** — PCG w grach, po co generować mapy automatycznie, cel i zakres pracy.

**2. Przegląd literatury** — PCG, algorytmy ewolucyjne, NSGA-II, logika rozmyta w ocenie
grywalności. Osobny podrozdział na artykuł źródłowy i jego metodę, bo do niego wracasz przez
całą pracę.

**3. Środowisko badawcze** — projekt gry: siatka heksagonalna, ekonomia, walka, drzewo decyzyjne
bota. Tu wchodzi treść GDD. Podkreśl, **czym ta gra różni się od Planet Wars** — to jest fundament
pod rozdział z wynikami.

**4. Metryki balansu i dynamiki** — definicje, uzasadnienie każdej, i **co zmieniłeś względem
artykułu oraz dlaczego**. To najważniejszy rozdział metodologiczny.

**5. Kalibracja systemu rozmytego** — badanie pilotażowe, rozkłady empiryczne, wyznaczanie progów
na kwantylach, bazy reguł. Rozdział, którego w artykule w ogóle nie ma.

**6. Optymalizacja wielokryterialna** — NSGA-II, genotyp, operatory, front Pareto.

**7. Weryfikacja** — mapy wzorcowe i kontrolne, dowód że ocena zgadza się z ludzką intuicją
(patrz rozdz. 7 niżej).

**8. Wyniki i dyskusja** — front Pareto, charakterystyka najlepszych map, **relacja balans–dynamizm**.

**9. Wnioski** — w tym ten najważniejszy: przenośność metryk zależy od mechaniki gry.

---

## 3. O genotypie — dlaczego akurat te trzy geny

Warto poświęcić temu osobny podrozdział, bo dobór genów to decyzja projektowa, o którą recenzent
zapyta.

### Kryteria, którymi się kierowaliśmy

- **Gen musi realnie wpływać na przebieg gry.** Parametr, który niczego nie zmienia, tylko
  powiększa przestrzeń przeszukiwania i spowalnia zbieżność (klątwa wymiarowości).
- **Geny nie powinny się dublować.** Dwa parametry sterujące tym samym zjawiskiem marnują budżet
  obliczeniowy.
- **Gen musi być interpretowalny.** Wynik pracy ma być wskazówką projektową, a nie wektorem liczb.

### Wybrane geny i ich role

| Gen | Zakres | Za co odpowiada |
|---|---|---|
| `minSpawnDistance` | 8–18 | ile czasu boty mają na spokojny rozwój przed pierwszym starciem |
| `population_max` | 20–100 | zamożność świata; skaluje wszystkie pięć progów populacji pól |
| `populationToCreateNewUnit` | 400–1000 | koszt i siła startowa oddziału; zagęszczenie armii na mapie |

Każdy dotyka innej warstwy: **przestrzeni**, **ekonomii** i **militariów**.

### Co pokazał pilotaż — i to warto opisać uczciwie

Zmierzone korelacje genów z metrykami (50 konfiguracji × 20 meczów):

- **`population_max` dominuje**: +0,87 z liczbą starć, +0,82 ze wskaźnikiem odbijania,
  −0,76 z nierównowagą militarną. To najsilniejsza dźwignia w całym systemie.
- **`populationToCreateNewUnit` ma wpływ umiarkowany**: około −0,5 na te same metryki.
- **`minSpawnDistance` jest najsłabszy**: maksymalnie +0,45.

Wniosek do pracy: nie wszystkie geny są równie ważne, a to ma konsekwencje dla ewolucji — NSGA-II
będzie głównie „kręcił" zamożnością świata. Warto to napisać wprost i zaproponować, co dalej
(np. rozszerzenie genotypu o parametry topograficzne, bo obecnie żaden gen nie steruje kształtem
terenu).

### Uwaga o `waterProbability`

W obecnej wersji nie jest genem — ilość wody jest stała. Warto o tym wspomnieć jako świadomym
ograniczeniu: dzięki temu wszystkie mapy mają identyczną liczbę pól lądowych i identyczną pulę
populacji, więc porównania między mapami są uczciwe. Ceną jest to, że **generator nie steruje
topografią**, a to najbardziej naturalny wymiar generowania map. To dobry kandydat na „kierunki
dalszych badań".

---

## 4. O metrykach — rdzeń wkładu własnego

Tutaj masz najwięcej materiału. Każda zmiana ma zmierzone uzasadnienie.

### 4.1. Growth Imbalance — zmiana definicji

Pierwotnie mierzona jako różnica stanów kont botów. Problem: konto zależy od decyzji o wydatkach,
a nie od właściwości mapy. Bot prowadzący wojnę wydaje na bieżąco i ma konto puste, bot pasywny
gromadzi.

Dowód w liczbach: przy niemal równym podziale mapy (nierównowaga terytorialna 9,2%) konta wynosiły
6475 i 0.

Po zmianie na **zdolność produkcyjną terytorium** (suma populacji posiadanych pól) korelacja
z nierównowagą terytorialną wzrosła z +0,47 do +0,94 — metryka zaczęła mierzyć mapę, a nie styl gry.

**Efekt uboczny wart opisania:** współczynnik regresji Growth względem Territorial wynosi 1,23.
Oznacza to, że bot z 10% przewagą terytorialną ma 12–13% przewagi gospodarczej. Powód leży w AI —
priorytety 2, 3, 6 i 7 sortują cele po populacji, więc kto szybciej się rozwija, ten zgarnia
najbogatsze pola. **To jest mechanizm kuli śnieżnej i tłumaczy późniejszy wynik główny.**

### 4.2. Conquering Rate — degradacja do bramki poprawności

W Planet Wars metryka rozstrzygała, czy gracz w ogóle podjął ekspansję. Miało to sens, bo:
zajęcie planety kosztowało statki, planet było 15–30, a rywalizowały trzy różne boty.

W naszej grze: zajęcie pola jest bezkosztowe, pól jest 360, a boty są identyczne i mają ekspansję
wpisaną w drzewo priorytetów ponad atakiem. Wynik: 74–99,9% z medianą 95,7%, a w dłuższych meczach
praktycznie stała.

Metryka została bramką walidacyjną. **To dobry przykład na to, że metryka nie jest własnością
metody, tylko relacją między metodą a mechaniką gry.**

### 4.3. Peak Differences — trzy zasoby i odwrócenie kierunku

Zgodnie z artykułem rozszerzone na trzy zasoby (terytorium, gospodarka, wojsko) i uśrednione;
korelują ze sobą +0,87 do +0,94, więc rozdzielanie ich nie wnosiłoby informacji.

**Świadome odstępstwo od artykułu:** tam wysoki pik podnosi dynamizm monotonicznie. U nas wysoki
pik oznacza dominację, a nie dramaturgię — dowód z podziału na tercyle:

| pik | reconquering | bitwy | nierównowaga terytorialna |
|---|---:|---:|---:|
| niski (49,8%) | 94,8% | 50,8 | 11,5% |
| średni (60,9%) | 74,9% | 37,2 | 14,3% |
| wysoki (72,1%) | 58,3% | 28,2 | 18,5% |

Im wyższy pik, tym **mniej** się dzieje. Dlatego wartością pożądaną jest zbiór ŚREDNI.

### 4.4. Bitwy polowe i zmiany prowadzenia — dwie próby dodania trzeciego wymiaru

Obie warto opisać, **łącznie z tym, że nie przyniosły oczekiwanego efektu**. To pokazuje rzetelność
warsztatu.

- **Bitwy polowe** (starcia token vs token): korelacja +0,96 ze wskaźnikiem odbijania na poziomie
  pojedynczych meczów, stały stosunek około 1 bitwy na 7 przejęć pola. Oba zdarzenia są skutkiem
  tego samego — czasu spędzonego przez oddziały na froncie.
- **Zmiany prowadzenia**: mniej redundantne (+0,55), ale korelują ujemnie z metrykami balansu
  (−0,63 z militarną). I to nie jest wada pomiaru — **lider może się zmienić tylko wtedy, gdy boty
  są blisko siebie**, więc metryka z definicji mierzy również wyrównanie.

Wniosek metodologiczny: w tej grze trudno znaleźć miarę dynamizmu niezależną od balansu.

---

## 5. Wynik główny — balans i dynamizm kooperują

To będzie najczęściej cytowane zdanie z Twojej pracy.

**Zmierzona korelacja ocen: od +0,54 do +0,60**, zależnie od zestawu metryk. Sprawdzone na trzech
niezależnych konfiguracjach — wynik jest stabilny.

W artykule relacja jest odwrotna: maksymalny balans osiągano tam przez **bezczynność graczy**
(boty siedzące w bazach mają idealnie równy stan posiadania), więc balans i dynamizm się wykluczały.

### Dlaczego u nas jest inaczej — mechanizm do opisania

1. Ekspansja jest darmowa i wymuszona przez drzewo priorytetów, więc **bezczynność nie jest
   możliwa**. Nie da się osiągnąć balansu przez nicnierobienie.
2. Przewaga terytorialna napędza gospodarkę ze współczynnikiem 1,23, a gospodarka napędza armię.
   **Nie ma mechaniki powrotu do gry.**
3. Wniosek: mapa niezbalansowana rozstrzyga się szybko i jednostronnie, czyli jest nudna.
   Mapa zbalansowana daje długą, wyrównaną wojnę, czyli jest dynamiczna.

### Jak to sformułować we wnioskach

Nie „artykuł się mylił", tylko: **relacja między balansem a dynamizmem nie jest własnością metody
oceny, lecz mechaniki gry**. W grach z silnym efektem kuli śnieżnej i bez mechanizmu wyrównywania
szans cele te są zbieżne; w grach dopuszczających strategię pasywną — rozbieżne.

To jest teza, której w literaturze nie ma, a masz na nią 2000 meczów.

### Konsekwencja praktyczna, o której trzeba napisać

Skoro cele są zbieżne, **front Pareto jest wąski** (2–4 punkty z 50 losowych konfiguracji).
Nie ukrywaj tego — wyjaśnij, że jest to bezpośrednie następstwo zbieżności celów, i że
w takiej sytuacji podejście wielokryterialne degeneruje się do jednokryterialnego. To również
jest wynik.

---

## 6. Poprawność eksperymentu — trzy problemy wykryte analizą statystyczną

Bardzo mocny materiał na rozdział metodologiczny. Pokazuje, że wyniki były weryfikowane,
a nie brane na wiarę — i że **analiza danych wykryła wady niewidoczne przy czytaniu kodu**.

### 6.1. Remisy zapisywane jako zwycięstwa

17,1 % meczów (341 z 2000) kończy się limitem tur, bez zdobycia którejkolwiek bazy. Funkcja
wyznaczająca zwycięzcę zwracała w takiej sytuacji zawsze tego samego bota, więc **340 z 341
remisów zapisano jako jego wygrane**. Błąd nie wpływał na metryki rozmyte (te nie patrzą na
zwycięzcę), ale fałszował każdą analizę skuteczności. Naprawiony — remis jest teraz osobnym wynikiem.

### 6.2. Systematyczna przewaga bazy nr 1

Wykryta w danych, nie w kodzie. Na 1659 rozstrzygniętych meczów baza nr 1 wygrywała **55,8 %**,
co daje odchylenie **4,7 sigma** od równowagi.

Przyczyna leżała w procedurze losowania: pierwsza baza wybierana była swobodnie spośród wszystkich
kandydatów, a druga **tylko spośród pól odległych o co najmniej `minSpawnDistance`**. Ten warunek
systematycznie spychał ją na peryferie mapy. Symulacja 3000 generacji potwierdziła:

| | baza 1 | baza 2 |
|---|---:|---:|
| średni dystans do środka mapy | 7,14 | **8,71** (+1,56 heksa, 23,7 sigma) |
| pól lądowych w promieniu 3 | 31,69 | **29,92** (o 6 % mniej) |

Naprawa: po wylosowaniu obu pozycji następuje ich zamiana z prawdopodobieństwem 50 %, co wyrównuje
rozkłady. Efekt potwierdzony pomiarem: **48,4 %, odchylenie 0,4 sigma**.

To jest gotowy przykład na to, że symetria rozstawienia nie bierze się sama z siebie — nawet przy
losowym generatorze trzeba jej pilnować.

### 6.3. Przewaga pierwszego ruchu — odpowiedź na artykuł Adamsa

Ernest Adams w „Designer's Notebook: A Symmetry Lesson" zwraca uwagę, że **sama symetria
geometryczna planszy nie wystarcza**, bo gracz wykonujący pierwszy ruch zyskuje przewagę tempa.
Wymienia trzy sposoby jej kompensowania. Warto pokazać, że badana gra ma wszystkie trzy:

| mechanizm wg Adamsa | realizacja w tej grze |
|---|---|
| ograniczenie siły pierwszego ruchu / rozstawienie uniemożliwiające natychmiastowe zagrożenie | `minSpawnDistance` 8–18 heksów; oddział pokonuje 1 heks na swoją turę, więc pierwszy kontakt następuje po kilkudziesięciu turach |
| wydłużenie rozgrywki | mecze trwają 300–450 tur, jedna tura to 0,25 % partii |
| losowość | mnożnik strat w walce 0,8–1,2, losowa mapa, losowe bazy, naprzemienna kolejność |

**Projekt eksperymentu (wart opisania osobno).** Zastosowano porównanie parowane: każda mapa
rozgrywana jest dwukrotnie — raz zaczyna bot 1, raz bot 2 — przy identycznym terenie, rozkładzie
populacji i pozycjach baz. Eliminuje to zmienność map, która w zwykłym pomiarze jest 2,6 raza
większa od badanego efektu. Wykonano 100 par, czyli 200 meczów.

**Wynik 1 — statystyki mapy nie zależą od tego, kto zaczyna.** Wszystkie osiem metryk mieści się
w granicach szumu (odchylenia 0,3–0,8 sigma):

| metryka | zaczynał bot 1 | zaczynał bot 2 | różnica | sigma |
|---|---:|---:|---:|---:|
| Territorial imbalance | 16,09 | 15,61 | +0,47 | 0,5 |
| Growth imbalance | 21,84 | 20,99 | +0,85 | 0,7 |
| Military imbalance | 22,65 | 22,36 | +0,29 | 0,3 |
| Reconquering rate | 71,88 | 75,26 | −3,39 | 0,8 |
| Conquering rate | 96,90 | 97,80 | −0,90 | 0,6 |
| Zmiany prowadzenia /100 tur | 2,26 | 2,06 | +0,20 | 0,5 |
| Bitwy polowe | 37,01 | 38,40 | −1,39 | 0,7 |
| Liczba tur | 332,3 | 341,3 | −9,03 | 0,8 |

To uzasadnia, że przy ocenie chromosomów nie trzeba rozdzielać wyników według kolejności ruchu.

**Wynik 2 — nie wykryto przewagi w skuteczności.** Bot zaczynający wygrał 53,6 % z 192
rozstrzygniętych meczów, odchylenie 1,0 sigma.

**Uczciwe zastrzeżenie do zapisania w pracy:** przy 192 meczach błąd standardowy wynosi 3,6 punktu,
więc test wykrywa dopiero przewagę powyżej 57 %. Poprawne sformułowanie brzmi „nie wykryto przewagi
pierwszego ruchu", a nie „przewagi nie ma". Rozstrzygnięcie wymagałoby około 400 par.

### 6.4. Ile z wyniku pochodzi z mapy, a ile z losu

Eksperyment parowany pozwala rozłożyć zmienność na dwie części, bo dysponujemy dwoma przebiegami
na **identycznym** terenie.

| metryka | sd między meczami | sd z MAPY | sd z LOSU | udział mapy |
|---|---:|---:|---:|---:|
| Territorial imbalance | 9,39 | 5,85 | 7,34 | **39 %** |
| Growth imbalance | 12,01 | 8,75 | 8,23 | **53 %** |
| Military imbalance | 10,84 | 7,19 | 8,11 | **44 %** |
| Reconquering rate | 38,35 | 21,61 | 31,68 | **32 %** |
| Liczba tur | 84,62 | 29,30 | 79,38 | **12 %** |

**Mapa wyjaśnia jedynie od 12 do 53 % różnic między meczami.** Pozostała część to losowość samej
symulacji: mnożnik strat w walce oraz zależność dalszego przebiegu od pojedynczych rozstrzygnięć.
Ta sama mapa rozegrana dwukrotnie potrafi dać mecz 250-turowy i 450-turowy.

Wniosek do dyskusji w pracy — **napięcie projektowe**: losowość walk skutecznie tłumi przewagę
pierwszego ruchu, zgodnie z zaleceniem Adamsa, ale jednocześnie zaszumia ocenę jakości mapy. Ten
sam mechanizm pomaga w jednym celu i szkodzi w drugim. To także wyjaśnia, dlaczego pojedynczy mecz
jest bezużyteczny jako miara jakości mapy i dlaczego potrzeba ich kilkudziesięciu.

---

## 7. Weryfikacja funkcji przystosowania — WYKONANA

Odpowiedź na uwagę promotora. To jest osobny rozdział pracy i najmocniejszy argument
metodologiczny, bo dowodzi, że system ocenia zgodnie z ludzką intuicją, a nie tylko sam ze sobą.

### Dlaczego była potrzebna

Kalibracja progów gwarantuje, że system rozróżnia mapy pochodzące z generatora. Nie dowodzi
jednak, że rozróżnia je **poprawnie**. Ocena 0,13 znaczyła dotąd wyłącznie „najgorsza z
wygenerowanych", a nie „mapa obiektywnie zła".

Dodatkowo obecny generator z założenia nie potrafi wyprodukować mapy skrajnie niesprawiedliwej:
liczba pól lądowych jest stała (360), pula populacji stała (72 pola na każdy z pięciu progów),
rozmieszczenie losowe. Żeby sprawdzić, czy metryka wykrywa mapę zepsutą, trzeba było taką mapę
**celowo wytworzyć**.

### Jak zbudowano zestaw kontrolny

Pięć trybów generatora, po 20 meczów każdy:

| tryb | opis | co testuje |
|---|---|---|
| **wzorzec** | symetria obrotowa 180° | czy system docenia mapę idealnie sprawiedliwą |
| **normalny** | generator losowy | punkt odniesienia, to co optymalizuje NSGA-II |
| zepsuty 1 | najbogatsze pola skupione wokół bazy 1 | Growth Imbalance |
| zepsuty 2 | baza 2 zepchnięta na skraj, baza 1 w centrum | Territorial Imbalance |
| zepsuty 3 | bazy tuż obok siebie | Game Length, efekt kuli śnieżnej |

**O symetrii obrotowej warto napisać osobno.** Na siatce odd-r odwzorowanie
`(x, y) → (szerokość−1−x, wysokość−1−y)` odpowiada odbiciu punktowemu we współrzędnych
sześciennych, więc **zachowuje wszystkie odległości heksowe** — potwierdzone numerycznie na
20 000 losowych par pól. Plansza dzieli się na 200 rozłącznych par, żadne pole nie jest własnym
obrazem, więc woda i populacja przydzielane są parami, a baza drugiego bota jest obrazem bazy
pierwszego. Warunki startowe są tożsame nie z przybliżenia, lecz z konstrukcji. Obrót o 180° to
standard projektowy map turniejowych 1v1 na sztywnych siatkach (Advance Wars, Wargroove,
Into the Breach), bo zachowuje długości dróg przemarszu — czego odbicie lustrzane nie gwarantuje.

### Wynik: metryki działają

| tryb mapy | teryt % | growth % | mil % | reconq % |
|---|---:|---:|---:|---:|
| symetria obrotowa | **14,2** | **18,2** | **20,9** | **104,2** |
| generator normalny | 19,1 | 27,1 | 26,5 | 53,4 |
| bogata strefa przy bazie 1 | 21,0 | **59,1** | **43,2** | 8,9 |
| baza 2 na skraju | **23,5** | 31,6 | 28,6 | 35,1 |

Każde zaburzenie wykryte przez tę metrykę, która miała je wykryć:

- **bogata strefa** podniosła Growth Imbalance z 27,1 % do **59,1 %**, ponad dwukrotnie — to
  bezpośrednie potwierdzenie zasadności przedefiniowania tej metryki (rozdz. 4.1)
- **baza na skraju** dała najwyższą nierównowagę terytorialną
- **symetria** dała najniższe wszystkie trzy nierównowagi i **dwukrotnie wyższy** wskaźnik
  odbijania niż mapa losowa — idealnie wyrównane siły powodują nieustanne falowanie frontu

### Najciekawszy przypadek: bazy obok siebie

Ten tryb miał **najniższe** surowe nierównowagi z całego zestawienia: terytorialna 0,7 %,
militarna 3,1 %. Wyglądał więc na mapę idealnie zbalansowaną — bo gra kończy się, zanim
ktokolwiek zdąży zbudować przewagę.

Odrzuciła go dopiero **bramka poprawności**: wskaźnik podboju 7,2 % wobec progu 60 %.

To jest empiryczne uzasadnienie decyzji o przeniesieniu Conquering Rate z wejść systemu do
warunków dopuszczenia wyniku (rozdz. 4.2). Gdyby pozostał zwykłym wejściem, mapa ta dostałaby
wysoką ocenę, a NSGA-II ewoluowałby w stronę map, na których rozgrywka kończy się po kilkudziesięciu
turach.

### Co weryfikacja wykryła w samym systemie oceny

Pierwsze uruchomienie **nie przeszło** kryterium: trzy różne mapy dostały identyczną ocenę 0,1333,
czyli matematyczne dno wyjścia systemu rozmytego.

Przyczyna: zbiór WYSOKI osiągał pełną przynależność już na kwantylu 75 % rozkładu z pilotażu.
Wszystko powyżej było „równie złe", więc mapa umiarkowanie zła i katastrofalna dawały ten sam
wynik. Sprawdzenie na danych pilotażowych pokazało, że **24 % konfiguracji lądowało na tym dnie** —
jedna czwarta przestrzeni przeszukiwania była dla algorytmu ewolucyjnego płaska.

Poprawka: punkt nasycenia zbioru WYSOKI przesunięty z kwantyla 75 % na **wartość maksymalną
zaobserwowaną w pilotażu**.

| | konfiguracji na dnie | rozróżnialnych ocen |
|---|---:|---:|
| przed | 13 / 50 (24 %) | 41 / 50 |
| **po** | **1 / 50 (2 %)** | **47 / 50** |

Po poprawce oceny układają się w oczekiwanej kolejności:

| tryb mapy | BALANS | DYNAMIZM |
|---|---:|---:|
| symetria obrotowa | **0,590** | **0,759** |
| generator normalny | 0,174 | 0,561 |
| bogata strefa | 0,141 | 0,154 |
| baza na skraju | 0,133 | 0,157 |
| bazy obok siebie | 0,000 | 0,000 |

Oba warunki spełnione: wzorzec ≥ losowa, losowa > najlepsza zepsuta.

**To jest bardzo dobry fragment pracy** — pokazuje, że weryfikacja nie była formalnością, tylko
wykryła realną wadę kalibracji, którą naprawiono przed uruchomieniem optymalizacji.

### Zastrzeżenie do zapisania

Skala ocen pozostaje **względna wobec rozkładu z pilotażu**. Mapa z bogatą strefą ma Growth
Imbalance 59,1 %, podczas gdy generator normalny wytwarza 13,9–32,4 % — jest poza zakresem
kalibracji i nadal się nasyca. Nie jest to wada, lecz właściwość metody: system kalibrowano po to,
by rozróżniał mapy rzeczywiście wytwarzane przez generator, a nie by mierzył patologie w skali
bezwzględnej.

Dlatego w rozdziale weryfikacyjnym należy podawać **i surowe metryki, i ocenę rozmytą**. Surowe
dowodzą, że wykrywanie działa nawet daleko poza zakresem kalibracji.

### Co jeszcze można zrobić, jeśli zostanie czas

**Odniesienie do map z istniejących gier.** Mapy 1v1 ze StarCrafta II i Warcrafta III mają
udokumentowane zasady: symetria względem środka, identyczna liczba baz rozszerzeń, równy dystans
do pierwszego rozszerzenia, przesmyki w symetrycznych miejscach. Nie trzeba odwzorowywać konkretnej
mapy — wystarczy zaimplementować te zasady jako kolejny tryb generatora.

Uwaga na pułapkę: **nie odwzorowuj konkretnej mapy 1:1**. Mapa ze StarCrafta jest zbalansowana
*dla StarCrafta*, jego jednostek i tempa. Jeśli w tej grze wypadnie źle, nie będzie wiadomo, czy
zawiodła metryka, czy topologia po prostu nie pasuje do innej mechaniki.

**Ocena ekspercka.** 10–20 map przedstawionych 3–5 osobom grającym w strategie, ocena „jak bardzo
ta mapa wygląda na sprawiedliwą" w skali 1–5, następnie korelacja z oceną systemu. Współczynnik
rzędu 0,6–0,7 byłby bardzo mocnym argumentem.

---

## 8. Wynik główny eksperymentu — NSGA-II

To jest rozdział z wynikami pracy. Przebieg: populacja 20, 25 pokoleń, 60 meczów na ocenę
chromosomu, **428 ocenionych konfiguracji**, 11,7 godziny obliczeń, zero odrzuceń przez bramki
poprawności.

### Znalezione optimum

| gen | wartość w najlepszych rozwiązaniach | dozwolony zakres |
|---|---|---|
| `population_max` | 93–99 (mediana **99**) | 20–100 |
| `populationToCreateNewUnit` | 400–619 (mediana **420**) | 400–1000 |
| `minSpawnDistance` | 8–13 (mediana **10**) | 8–18 |

Przepis na dobrą mapę w tej grze: **bogaty świat, tanie jednostki, umiarkowany dystans startowy.**
Duża pula zasobów pozwala obu botom rozwinąć się porównywalnie, tanie jednostki zapełniają mapę
armiami i wymuszają ciągłą walkę.

Front Pareto (4 rozwiązania niezdominowane):

| # | spawnDist | popMax | unitCost | BALANS | DYNAMIZM | teryt % | reconq % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 10 | 99 | 619 | 0,8422 | 0,8356 | 7,5 | 104,3 |
| 2 | 10 | 98 | 414 | 0,8409 | 0,8361 | 8,0 | 114,9 |
| 3 | 10 | 99 | 442 | 0,8380 | 0,8366 | 8,5 | 137,7 |
| 4 | 13 | 99 | 402 | 0,8350 | 0,8372 | 9,1 | 141,8 |

### Zbieżność

Hiperobjętość: 0,6912 (pok. 1) → 0,7011 (pok. 5) → **0,7051** (pok. 16, dalej płasko do 25).

**99,4 % wyniku osiągnięto w pierwszych 20 % czasu.** Od pokolenia 16 krzywa jest całkowicie
płaska. Wniosek do opisania: dla tej przestrzeni parametrów 15 pokoleń w zupełności wystarcza.
Krzywa hiperobjętości z 25 punktami to gotowy wykres do pracy — dane w `nsga2_postep.json`.

### Najważniejszy wniosek: front Pareto to szum, a nie kompromis

Cztery rozwiązania na froncie różnią się balansem o **0,0072**, a dynamizmem o **0,0016**.

Do porównania wzięto 102 chromosomy z tego samego, najlepszego rejonu przestrzeni
(`popMax ≥ 97`, `unitCost ≤ 460`, `spawn ≤ 13`) — czyli praktycznie ten sam zestaw parametrów —
i zmierzono rozrzut ich ocen:

| | rozpiętość na froncie | odchylenie standardowe w tym samym rejonie |
|---|---:|---:|
| BALANS | 0,0072 | **0,0068** |
| DYNAMIZM | 0,0016 | **0,0138** |

**Szum pomiarowy jest równy lub większy od całej szerokości frontu.** Cztery rozwiązania są
statystycznie nierozróżnialne — to, które z nich trafiło na front, jest kwestią losu, a nie jakości.

To nie jest wada implementacji, lecz **empiryczne potwierdzenie wyniku głównego z rozdziału 5**:
skoro balans i dynamizm w tej mechanice kooperują, nie ma czego kompromisować, więc optymalizacja
wielokryterialna **degeneruje się do jednokryterialnej**. Front Pareto zapada się do jednego
punktu, a jego pozorna szerokość pochodzi wyłącznie z niepewności pomiaru.

Ten wniosek warto postawić na równi z wynikiem o kooperacji celów — jest jego bezpośrednią
konsekwencją operacyjną i ma na poparcie 428 ocen.

### Ograniczenie: dwa geny na krawędzi zakresu

Uwaga metodologiczna, o którą recenzent zapyta na pewno.

- `population_max` = 99 w **138 z 428** chromosomów, przy górnej granicy 100
- `populationToCreateNewUnit` ≤ 450 w **234 z 428**, przy dolnej granicy 400

Optimum leży **na krawędzi dozwolonej przestrzeni**, a nie w jej wnętrzu. Nie wiadomo więc, czy
przy szerszych zakresach wynik nie byłby jeszcze lepszy.

Sposób zamknięcia tematu jednym akapitem: celowany przemiat poza granicami (bez ponownego
uruchamiania NSGA-II) sprawdzający, czy ocena rośnie dalej, czy się nasyca. Skrypt
`test_granic_genow.py`, około 20 minut obliczeń.

---

## 9. Liczby, które warto mieć pod ręką

| Wielkość | Wartość |
|---|---|
| Rozegranych meczów w pilotażach | 2000 (2 × 50 konfiguracji × 20 meczów) |
| Meczów na ocenę jednej konfiguracji | 20 |
| Wielkość mapy | 20 × 20, dokładnie 360 pól lądu i 40 wody |
| Rozkład populacji | 5 progów po 20% pól, suma na mapie stała |
| Rozpiętość ocen przed kalibracją | balans 0,19 · dynamizm 0,02 |
| Rozpiętość ocen po kalibracji | balans 0,70 · dynamizm 0,70 |
| Korelacja balans ↔ dynamizm | +0,54 do +0,60 |
| Udział mapy w zmienności wyniku | 12–53 % (reszta to losowość symulacji) |
| Przewaga pierwszego ruchu | 53,6 % zwycięstw, 1,0 sigma — nie wykryto |
| Przewaga pozycyjna bazy nr 1 | 55,8 % przed poprawką → 48,4 % po |
| Meczów kończących się remisem | 17,1 % |
| Ocena mapy wzorcowej (symetria 180°) | balans 0,590 · dynamizm 0,759 |
| Ocena mapy losowej | balans 0,174 · dynamizm 0,561 |
| Ocena map celowo zepsutych | balans 0,141 do 0,000 |
| Nasycenie skali przed poprawką / po | 24 % / 2 % konfiguracji na dnie |
| NSGA-II: ocenionych konfiguracji | 428 (populacja 20, 25 pokoleń, 11,7 h) |
| NSGA-II: hiperobjętość | 0,6912 → 0,7051, płaska od pokolenia 16 |
| Znalezione optimum | popMax ≈ 99 · unitCost ≈ 420 · spawnDist ≈ 10 |
| Szerokość frontu Pareto vs szum | 0,0072 wobec sd 0,0068 — nierozróżnialne |
| Reguł w bazie balansu | 27 (komplet 3³) |
| Reguł w bazie dynamizmu | 18 (2 × 3 × 3) |
| Błąd średniej przy 20 meczach | terytorium ±2,0 · gospodarka ±2,5 · odbijanie ±9,7 |

---

## 10. Pułapki i rzeczy, o których łatwo zapomnieć

- **Opisz kompletność baz reguł.** Pierwotna baza balansu miała luki — dla 32% możliwych wyników
  żadna reguła się nie aktywowała, co kończyło się błędem. Opisanie, jak to wykryto i naprawiono
  (tabela decyzyjna generowana programowo, z asercją kompletności), pokazuje warsztat.

- **Podaj wielkość błędu pomiaru.** Przy 20 meczach wskaźnik odbijania ma błąd średniej ±9,7 punktu.
  Bez tego każda różnica między konfiguracjami jest niefalsyfikowalna. Jeśli w wynikach porównujesz
  dwie mapy, sprawdź najpierw, czy różnica przekracza szum.

- **Nie chowaj wyników negatywnych.** Dwie próby znalezienia trzeciego wymiaru dynamizmu nie
  wypaliły. To materiał na podrozdział, nie na przemilczenie — pokazuje, że wniosek o zbieżności
  celów nie jest efektem jednego niefortunnego doboru metryk.

- **Losowość jest udokumentowana, ale nie kontrolowana.** Mnożnik strat w walce (0,8–1,2),
  rozmieszczenie wody, populacji i baz — wszystko losowe bez stałego ziarna. Napisz o tym w
  ograniczeniach i podaj, ile powtórzeń było potrzebnych, żeby to uśrednić.

- **Napisz, czego generator nie potrafi.** Nie steruje topografią, nie tworzy przesmyków ani
  spójnych lądów o zaplanowanym kształcie. To uczciwe ograniczenie i naturalny kierunek rozwoju.

- **Rozdziel „nie wykryto" od „nie ma".** Przy każdym wyniku nieistotnym statystycznie podaj, jak
  duży efekt test w ogóle był w stanie wykryć. Dla przewagi pierwszego ruchu przy 192 meczach była
  to granica 57 % — poniżej niej test jest ślepy.

- **Zachowaj surowe dane.** `pilotaz_wyniki.json` i katalog `Wyniki_Batch` to materiał dowodowy pod
  każdą liczbę w pracy. Warto je zarchiwizować razem z wersją kodu, która je wygenerowała.
