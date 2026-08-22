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

**7. Weryfikacja** — porównanie z mapami wzorcowymi i kontrolnymi (patrz punkt 6 niżej).

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

## 7. Weryfikacja — plan po sugestii promotora

Promotor ma rację i to jest najsłabszy obecnie punkt pracy: **wiemy, że metryki różnicują mapy,
ale nie wiemy, czy różnicują je zgodnie z ludzką oceną**.

Dodatkowy problem, który warto uświadomić sobie przed weryfikacją: obecny generator **z założenia
nie potrafi wyprodukować bardzo niezbalansowanej mapy**. Liczba pól lądowych jest stała (360),
pula populacji stała (72 pola na każdy z pięciu progów), rozmieszczenie losowe. Jedyna asymetria
bierze się z lokalizacji baz i lokalnego szczęścia. Dlatego oceny balansu układają się w wąskim
paśmie, a wartość „0,13" nie oznacza mapy złej w sensie bezwzględnym, tylko najgorszą z dość
dobrego zbioru.

**Oceny są względne wobec zaobserwowanego rozkładu, nie bezwzględne.** To trzeba napisać wprost
i to jest dokładnie powód, dla którego weryfikacja na zewnętrznych punktach odniesienia jest
potrzebna.

### Plan minimum: kontrole negatywne i pozytywne

Wymaga dorobienia wczytywania mapy z pliku (kilkadziesiąt linii w generatorze).

**Kontrole negatywne** — mapy, o których człowiek bez wahania powie „to jest zepsute":
1. wszystkie pola z najwyższego progu populacji skupione wokół bazy Bota 1, najniższe wokół Bota 2
2. pas wody dzielący mapę tak, że jeden bot ma dostęp do 70% lądu
3. bazy postawione w odległości 2 heksów od siebie

**Kontrole pozytywne** — mapy zaprojektowane jako sprawiedliwe:
1. układ z symetrią obrotową 180° (jak większość map 1v1 w StarCraft II)
2. układ z symetrią lustrzaną
3. mapa z równomiernym rozłożeniem zasobów i równym dystansem do centrum

**Kryterium sukcesu:** kontrole negatywne muszą dostać ocenę balansu wyraźnie niższą niż pozytywne.
Jeśli system tego nie rozróżni — metryki nie mierzą tego, co deklarują, i to trzeba naprawić przed
uruchomieniem NSGA-II.

### Plan rozszerzony: odniesienie do map z istniejących gier

Zgodnie z drugą sugestią promotora. Mapy 1v1 ze StarCrafta II i Warcrafta III mają udokumentowane
zasady projektowe:

- symetria obrotowa lub lustrzana względem środka
- identyczna liczba baz rozszerzeń dla obu graczy
- równy dystans od bazy głównej do pierwszego rozszerzenia
- przesmyki (choke points) w symetrycznych miejscach

Nie trzeba odwzorowywać konkretnej mapy heks po heksie. Wystarczy **zaimplementować te zasady**
jako alternatywny generator, wygenerować kilkanaście map i pokazać, że system rozmyty ocenia je
wyżej niż mapy losowe. To jest weryfikacja „na bazie czegoś, co dobrze funkcjonuje w zbliżonej
grze" — dokładnie o to promotor prosił.

### Plan maksimum: ocena ekspercka

10–20 map przedstawionych 3–5 osobom grającym w strategie, z prośbą o ocenę „jak bardzo ta mapa
wygląda na sprawiedliwą" w skali 1–5. Następnie korelacja ocen ludzkich z oceną systemu rozmytego.
Współczynnik korelacji rzędu 0,6–0,7 byłby bardzo mocnym argumentem.

---

## 8. Liczby, które warto mieć pod ręką

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
| Reguł w bazie balansu | 27 (komplet 3³) |
| Reguł w bazie dynamizmu | 18 (2 × 3 × 3) |
| Błąd średniej przy 20 meczach | terytorium ±2,0 · gospodarka ±2,5 · odbijanie ±9,7 |

---

## 9. Pułapki i rzeczy, o których łatwo zapomnieć

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
