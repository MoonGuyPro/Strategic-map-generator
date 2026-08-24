# Stan projektu — przekazanie do nowej sesji

Plik dla asystenta w nowym oknie czatu. Opisuje, czym jest projekt, co już zrobiono, jak
uruchamiać poszczególne elementy i co zostało do zrobienia.

**Trzy dokumenty czyta się razem:**

| plik | zawartość |
|---|---|
| `GDD_zaktualizowany.docx` | pełna specyfikacja gry, metryk i systemu rozmytego (aktualna, zgodna z kodem) |
| `WSKAZOWKI_DO_PRACY.md` | wyniki badawcze, wnioski i rady do pisania pracy magisterskiej |
| **`STAN_PROJEKTU.md`** (ten plik) | struktura kodu, sposób uruchamiania, stan prac, następne kroki |

**Jak czytać te dokumenty — stan na koniec prac obliczeniowych.**

Wszystkie eksperymenty wykonano na **aktualnym systemie oceny**: wzory (6) i (7) z artykułu
źródłowego, punkty kulminacyjne jako metryka diagnostyczna, progi wyznaczone z jednego badania
pilotażowego 50 × 60 meczów. Nic nie czeka na przeliczenie. Dokumenty i kod są ze sobą zgodne —
sprawdzone automatycznie.

Trzy fragmenty `WSKAZOWKI_DO_PRACY.md` mają charakter **historyczny** i są tam wyraźnie oznaczone
ramką ostrzegawczą. Nie przepisuj z nich liczb bez jej przeczytania:

| fragment | czego dotyczy |
|---|---|
| rozdz. 7, „Wynik przy genach historycznych 12 / 60 / 700" | wcześniejszy wariant weryfikacji; rozstrzygający jest wariant z genami z optimum, opisany wyżej w tym samym rozdziale |
| rozdz. 6.4, tabela rozkładu wariancji | pomiar na 100 parach i starych definicjach metryk; wniosek potwierdzony nowszym wynikiem 77 % |
| rozdz. 11 i 12 | zapis analizy, która doprowadziła do poprawek; podane tam liczby opisują stan sprzed zmian |

**Najważniejsze liczby aktualne:** korelacja balans–dynamizm **+0,586** · optimum `population_max`
90–99, `populationToCreateNewUnit` 412–447, `minSpawnDistance` 10–13 · mapa wzorcowa **0,8475**
balansu przy sufcie 0,8667 · nie wykryto przewagi pierwszego ruchu (51,2 % przy 300 parach).

---

## 1. Czym jest projekt

Praca magisterska: **zastosowanie NSGA-II i metryk balansu oraz dynamiki do proceduralnego
generowania map dla gier strategicznych**.

**Artykuł źródłowy (właściwy):** Lara-Cabrera, Cotta, Fernández-Leiva, *On Balance and Dynamism in
Procedural Content Generation with Self-Adaptive Evolutionary Algorithms*, **Natural Computing 2014**.
Zawiera pełne wzory metryk, bazy reguł i wyniki. **Metodę opisuj stąd i stąd bierz cytaty.**

Artykuł przeglądowy: Lara-Cabrera, Nogueira-Collazo, Cotta, Fernández-Leiva, *Procedural Content
Generation for Real-Time Strategy Games*, IJIMAI 2015 — te same wnioski w skrócie, bez wzorów.
Projekt powstawał początkowo na jego podstawie, stąd kilka rozbieżności wykrytych dopiero po
lekturze wersji pełnej (`WSKAZOWKI_DO_PRACY.md` rozdz. 11 i 12).

Autorzy oceniali mapy do gry Planet Wars za pomocą siedmiu metryk i logiki rozmytej, a optymalizowali
je algorytmem NSGA-II.

Ten projekt przenosi tę metodę na **własną grę turową na siatce heksagonalnej** (Unity, C#),
w której rywalizują dwa identyczne boty. Python steruje eksperymentem i liczy oceny rozmyte.

**Nie szukamy jednej idealnej mapy.** Szukamy zestawów parametrów generatora, które produkują
mapy jednocześnie zbalansowane i dynamiczne. Wynikiem jest front Pareto zestawów parametrów.

---

## 2. Architektura

```
Python                                Unity (C#)
------                                ----------
nsga2_optymalizacja.py
  └─ pipeline_fuzzy.py
       ├─ system rozmyty (skfuzzy)
       └─ evaluate_population()  ──►  map_input.json
                                        {"recipes":[...], "pairedFirstMove":bool}
                                              │
                                        BatchRunner.RunSim
                                        BotTurnManager.ExecuteBatchSimulations
                                          - dla kazdego przepisu: 60 meczow
                                          - HexMapGenerator generuje mape
                                          - boty graja na przemian
                                              │
                                  ◄──  metrics_output.json
                                        {"results":[...]}
```

Unity uruchamiane jest w trybie wsadowym (`-batchmode -nographics`). **Cała populacja oceniana
jest w JEDNYM uruchomieniu Unity** — start edytora trwa ~40 s, więc uruchamianie go osobno dla
każdego chromosomu byłoby nie do przyjęcia czasowo.

### Pliki Python

| plik | rola |
|---|---|
| `pipeline_fuzzy.py` | **rdzeń**: definicje zbiorów rozmytych, progi, bazy reguł, `evaluate_population()`, `score()` |
| `nsga2_optymalizacja.py` | główny eksperyment — NSGA-II |
| `pilotaz.py` | badanie pilotażowe do kalibracji progów |
| `test_pierwszy_ruch.py` | pomiar przewagi pierwszego ruchu (porównanie parowane) |
| `test_mapy_kontrolne.py` | weryfikacja na mapach wzorcowych i celowo zepsutych; przyjmuje argument `domyslny` albo `optimum` (zestaw genow) |

### Pliki Unity (`Assets/Scripts/`)

| plik | rola |
|---|---|
| `HexTileMapGenerator.cs` | generowanie mapy: woda, progi populacji, bazy, tryby kontrolne |
| `BotTurnManager.cs` | pętla meczu, tryb wsadowy, zbieranie metryk, most JSON |
| `BotController.cs` | logika bota: drzewo priorytetów, ruch, walka, ekonomia |
| `GameMetricsCollector.cs` | raporty tekstowe z pojedynczych meczów |
| `Editor/BatchRunner.cs` | punkt wejścia dla `-executeMethod` |

### Dane

| plik / katalog | zawartość |
|---|---|
| `pilotaz_wyniki.json` | 50 konfiguracji × 60 meczów, wszystkie metryki — podstawa kalibracji progów |
| `mapy_kontrolne_wyniki.json` | weryfikacja na mapach kontrolnych, geny historyczne 12 / 60 / 700 |
| `mapy_kontrolne_optimum_wyniki.json` | **weryfikacja rozstrzygajaca**, geny z optimum 11 / 96 / 447 |
| `granice_genow_wyniki.json` | przemiat poza granicami zakresów genów (kontrola metodologiczna) |
| `Wyniki_Batch/` | raport tekstowy z każdego meczu (ponad 30 000 plików) |
| `nsga2_front.json` / `.csv` | front Pareto oraz `historia` — 408 ocen z numerem pokolenia; stąd odtwarza się krzywą hiperobjętości |
| `nsga2_postep.json` | plik kontrolny nadpisywany co pokolenie — zawiera tylko stan końcowy, nie całą krzywą |

---

## 3. Genotyp i cele

**Trzy geny całkowite:**

| gen | zakres | co steruje |
|---|---|---|
| `minSpawnDistance` | 8–18 | dystans między bazami, czyli czas spokojnego rozwoju |
| `population_max` | 20–100 | najwyższy z pięciu progów populacji; skaluje zamożność świata |
| `populationToCreateNewUnit` | 400–1000 | koszt i siła startowa oddziału |

**Dwa cele, oba maksymalizowane:** BALANS i DYNAMIZM, każdy z systemu rozmytego w skali 0–1.

**Wejścia systemu rozmytego:**

- BALANS ← Territorial Imbalance, Growth Imbalance, Military Imbalance (27 reguł)
- DYNAMIZM ← zmiany prowadzenia na 100 tur, Reconquering Rate (6 reguł)
- **Metryki diagnostyczne** (mierzone i raportowane, poza systemem): Peak Differences, bitwy polowe
- **Bramki poprawności** (poza systemem): `gameLength ≥ 15 %` oraz `conqueringRate ≥ 60 %`.
  Niespełnienie któregokolwiek → obie oceny zerowe.

---

## 4. Stan prac — co jest zrobione

Wszystko poniżej jest **wdrożone, przetestowane i opisane w GDD**.

- [x] Growth Imbalance przedefiniowany z „stanu kont botów" na „zdolność produkcyjną terytorium"
- [x] Rozkład populacji: pięć progów po 20 % pól, skalowanych przez `population_max`
- [x] Dokładnie 40 pól wody z 400 (stała liczba lądu = 360)
- [x] Bazy reguł kompletne, z asercją sprawdzaną przy starcie (27 kombinacji balansu, 6 dynamizmu)
- [x] Conquering Rate zdegradowany do bramki poprawności
- [x] Peak Differences liczone na trzech zasobach i uśrednione (korelują +0,88 do +0,96)
- [x] **Peak Differences wdrożone wg wzoru (7) z artykułu** — amplituda wahnięcia `max(d) − min(d)`
      przy różnicy ZE ZNAKIEM, zamiast maksimum modułu. Przy okazji ujednolicona normalizacja
      wszystkich trzech pików (wcześniej terytorialny dzielił się przez całą planszę, a pozostałe
      przez stan posiadania obu botów)
- [x] **Reconquering Rate wdrożony wg wzoru (6) z artykułu** — średnia na turę zamiast sumy;
      eksportowany jako „procent pól zmieniających właściciela na 100 tur"
- [x] Metryka zmian prowadzenia (na 100 tur) jako wejście dynamizmu — jedyne niezależne od balansu
- [x] Bitwy polowe mierzone jako metryka diagnostyczna (poza systemem rozmytym)
- [x] **Peak Differences przeniesione do metryk diagnostycznych** — nadal liczone i raportowane,
      ale nie sa juz wejsciem kryterium dynamizmu; baza reguł
      dynamizmu z 18 na 6, bez utraty rozdzielczosci ocen. Powod zmierzony: +0,930 z nierownowaga
      terytorialna, ujemnie z odbijaniem i bitwami
- [x] Kalibracja progów na kwantylach — **przeliczona po zmianie metryk**, 50 konfiguracji × 60
      meczów, wszystkie progi z jednego odtwarzalnego pliku `pilotaz_wyniki.json`
- [x] Punkt nasycenia zbioru WYSOKI przesunięty na maksimum z pilotażu
- [x] Naprawiona definicja siły militarnej (tokeny + garnizon bazy, w procentach)
- [x] Naprawione raportowanie remisów (17 % meczów było fałszywie przypisywanych)
- [x] **Raport tekstowy ujednolicony z eksportem JSON.** `GameMetricsCollector` to osobna,
      rownolegla sciezka liczenia metryk, uzywana wylacznie do plikow w `Wyniki_Batch/`. Liczyl
      jeszcze wskaznik odbijania i punkty kulminacyjne starymi wzorami; oba poprawiono. **Nie mialo
      to wplywu na zaden wynik** — wszystkie oceny rozmyte ida sciezka JSON z `BotTurnManager`,
      a raporty tekstowe czyta tylko `test_pierwszy_ruch.py` i tylko dla jednego wiersza tabeli.
      Pozostale metryki (trzy nierownowagi, podboj, bitwy, zmiany prowadzenia) obie sciezki liczyly
      identycznie
- [~] Przewaga pozycyjna bazy nr 1 — **w wiekszosci usunieta, ale nie do zera**. Bylo 55,8 % przy
      4,7 sigma; kontrola na 300 parach daje 54,7 % przy 1,85 sigma, czyli formalnie w granicach
      szumu, lecz tuz pod granica wykrywalnosci. Nie wplywa na oceny rozmyte (te nie patrza na
      zwyciezce), wplywa na kazda analize skutecznosci. Rozstrzygniecie wymagaloby ok. 900 par
- [x] Sztywna naprzemienność kolejności ruchu w trybie wsadowym
- [x] Zmierzona przewaga pierwszego ruchu — **powtorzona na 300 parach**, nie wykryto
      (51,2 % przy 0,74 sigma; granica wykrywalnosci 53,1 % wobec 57 % poprzednio)
- [x] Weryfikacja na mapach kontrolnych — **zaliczona w dwoch wariantach genow**. Przy genach
      z optimum mapa wzorcowa osiaga 0,8475 balansu i 0,8667 dynamizmu, bijac zarowno generator
      losowy, jak i najlepsze rozwiazanie z frontu NSGA-II
- [x] Cała populacja oceniana w jednym uruchomieniu Unity
- [x] Zabezpieczenia: kasowanie starego wyniku, timeout, kontrola liczby wyników
- [x] NSGA-II zaimplementowany, przetestowany i **uruchomiony w pelnym przebiegu**
- [x] Przemiat poza granicami zakresow genow — powtorzony na aktualnych metrykach; **nie wykryto
      istotnej poprawy poza zakresem**, przy czym powyzej popMax ok. 140 funkcja oceny dobija do
      sufitu i nie byloby jej czym wykryc
- [x] Naprawione dwa bledy w `test_granic_genow.py`: nieaktualna stala szumu oraz porownanie
      maksimum z maksimum przy roznej liczbie probek (teraz srednia kontra srednia)

### Wynik glownego eksperymentu

Przebieg NSGA-II na poprawionym systemie metryk: populacja 20, 25 pokolen, 60 meczow na ocene,
**408 ocenionych konfiguracji**, 11 h 45 min, zero odrzucen przez bramki, 112 genotypow z pamieci.

**Znalezione optimum:** `population_max` 90-99 · `populationToCreateNewUnit` 412-447 ·
`minSpawnDistance` 10-13 (mediany 91 / 439 / 11). Czyli: bogaty swiat, tanie jednostki, umiarkowany
dystans startowy — **ten sam rejon co przed poprawka metryk**, mimo zmiany dwoch wzorow,
przeliczenia wszystkich progow i usuniecia jednego wejscia dynamizmu. Wniosek projektowy jest
odporny na szczegoly konstrukcji funkcji oceny.

**Zbieznosc:** hiperobjetosc 0,7011 → 0,7293, przy czym **99 % koncowej wartosci osiagnieto
w pokoleniu 2**. Ponad 10 godzin dalszych obliczen dodalo 0,0066.

**Front Pareto ma 5 rozwiazan i — w odroznieniu od poprzedniego przebiegu — ma kszta³t.**
Rozpietosc frontu przekracza szum pomiarowy 1,24x w balansie i **1,44x w dynamizmie** (poprzednio
0,12x, czyli czysty szum). Korelacja obu ocen na samym froncie wynosi **-0,574**: rozwiazanie
o najlepszym balansie ma najslabszy dynamizm i odwrotnie. Jest to kszta³t opisany w artykule
zrodlowym — lagodny spadek dynamizmu przy rosnacym balansie.

Poprawne sformulowanie wyniku glownego brzmi teraz: **cele kooperuja globalnie (korelacja +0,586 na
losowej probce z calej przestrzeni genotypu), a wymieniaja sie lokalnie, w samym rejonie optimum.**
Efekt jest slaby — 1,2-1,4 odchylenia — wiec nie wolno go przedstawiac jako wyraznego kompromisu.

**Ograniczenie ujawnione przez ten przebieg:** dynamizm nasyca sie u gory. 118 z 408 chromosomow
(29 %) osiagnelo ocene co najmniej 0,860 przy sufcie 0,8667, bo oba wejscia dynamizmu wychodza poza
zakres kalibracji (zmiany prowadzenia do 6,00 przy nasyceniu 4,91; odbijanie do 35,6 przy 34,3).
Zmierzona rozpietosc frontu w dynamizmie jest wiec **wartoscia dolna**.

Problem dwoch genow na krawedzi zakresu w duzej mierze zniknal: `population_max` >= 99 wystepuje
w 67 z 408 chromosomow (poprzednio 138 z 428), a front zawiera wartosci 90, 91, 96, 96, 99 —
optimum weszlo do wnetrza dozwolonej przestrzeni.

### Wynik przemiatu poza granice zakresow genow

Powtorzony na aktualnych metrykach, 14 konfiguracji x 60 meczow.

**Skrypt wypisal „zakres obcial optimum" i byl to falszywy alarm.** Trzy przyczyny, wszystkie
naprawione w kodzie:

1. stala szumu pochodzila z poprzedniego systemu oceny (0,0068 zamiast 0,0102);
2. punkt odniesienia popMax = 99 nie trafial do grupy „w zakresie", wiec porownywano najlepszy punkt
   spoza zakresu z konfiguracjami gorszymi od znalezionego optimum — po dolaczeniu go roznica spada
   z +0,0174 do +0,0014;
3. porownywano maksimum z 5 probek z maksimum z 3, co samo z siebie daje okolo +0,006 przewagi.

Po poprawce, przy porownaniu srednich: `population_max` +0,0057 balansu (0,77 sigma) i +0,0116
dynamizmu (1,44 sigma); `populationToCreateNewUnit` +0,0019 (0,24 sigma) i +0,0110 (1,30 sigma).
**Zadna roznica nie jest istotna.**

**Zastrzezenie, ktore trzeba opisac w pracy:** powyzej `population_max` ok. 140 wskaznik odbijania
przekracza punkt nasycenia (34,30), a dynamizm dobija do sufitu 0,8667. Kryterium jest tam slepe
z konstrukcji, wiec „brak poprawy" nie znaczy „nie ma poprawy". Surowe metryki sugeruja slaby,
niemonotoniczny trend dalszej poprawy (nierownowaga terytorialna 10,6 % dla 90-100 wobec 8,8 %
dla 120-200, korelacja -0,360).

Przekroj kosztu jednostki jest czytelniejszy i nadaje sie na wykres: ponizej 400 brak poprawy,
powyzej 500 monotoniczne pogorszenie wszystkich metryk naraz (przy 800 balans 0,8132, odbijanie
23,9, podboj 91,1 %). Dolna granica 400 zostala dobrana trafnie, gorna 1000 z duzym zapasem.

Pelne omowienie w `WSKAZOWKI_DO_PRACY.md` rozdz. 8.

### Wynik pomiaru przewagi pierwszego ruchu

Powtorzony na aktualnych metrykach: 300 par, 600 meczow.

1. **Statystyki mapy nie zaleza od kolejnosci ruchu.** Wszystkie osiem metryk w granicach szumu,
   odchylenia 0,1-1,1 sigma.
2. **Nie wykryto przewagi pierwszego ruchu**: 51,2 % przy 0,74 sigma. Granica wykrywalnosci spadla
   z 57 % do **53,1 %**, wiec wniosek jest teraz znacznie mocniejszy.
3. **Przewaga bazy nr 1 nie zniknela calkowicie**: 54,7 % przy 1,85 sigma. Patrz rozdz. 6.2
   wskazowek — twierdzenie „usunieta" zostalo oslabione do „w wiekszosci usunieta".
4. **Pulapka statystyczna warta opisania**: skrypt liczyl istotnosc, traktujac 579 meczow jako
   niezalezne, a dwa mecze w parze dziela te sama mape i te same pozycje baz. W 232 parach z 300
   (77 %) obie rozgrywki wygral ten sam bot, przy 50 % oczekiwanych. Po poprawce istotnosc przewagi
   bazy nr 1 spada z 2,54 do 1,85 sigma. Skrypt poprawiony.

Liczba 77 % jest zarazem najprostszym dowodem na to, ze mapa przesadza o wyniku — prostszym niz
rozklad wariancji z rozdz. 6.4.

Pelne omowienie w `WSKAZOWKI_DO_PRACY.md` rozdz. 6.2, 6.3 i 6.3.1.

### Wynik weryfikacji na mapach kontrolnych

Wykonana w dwoch wariantach po 5 trybow x 60 meczow, roznia sie wylacznie zestawem genow.

**WARIANT ROZSTRZYGAJACY — geny z optimum 11 / 96 / 447** (`mapy_kontrolne_optimum_wyniki.json`):

| tryb | teryt % | growth % | mil % | BALANS | DYNAMIZM |
|---|---:|---:|---:|---:|---:|
| symetria obrotowa 180 st. | **6,4** | **8,1** | **8,9** | **0,8475** | **0,8667** |
| generator normalny | 9,4 | 12,7 | 10,2 | 0,8320 | 0,8468 |
| baza 2 zepchnieta w rog | 12,6 | 20,2 | 11,4 | 0,6058 | 0,8324 |
| bogata strefa przy bazie 1 | 14,5 | 41,1 | 21,0 | 0,1474 | 0,5168 |
| bazy tuz obok siebie | 10,2 | 43,3 | 23,8 | 0,0000 | 0,0000 |

**Mapa wzorcowa osiaga 97,8 % sufitu balansu i doklednie 100,0 % sufitu dynamizmu, bijac zarowno
generator losowy (+0,0155 balansu), jak i najlepsze rozwiazanie z frontu NSGA-II (0,8418).** Jej
surowe nierownowagi sa najnizsze w calym projekcie. Funkcja przystosowania przyznaje wiec najwyzsza
ocene mapie, o ktorej z konstrukcji wiadomo, ze jest sprawiedliwa — i to mimo ze mapa ta nigdy nie
brala udzialu w optymalizacji. **To jest odpowiedz na uwage promotora.**

Trzy dalsze wnioski:

1. **Poprzedni slaby wynik mapy wzorcowej (0,4148) byl artefaktem parametrow, nie mapy.** Ta sama
   plansza przy genach 12 / 60 / 700 daje 0,4148, przy 11 / 96 / 447 daje 0,8475.
2. **Asymetrie przestrzenna da sie skompensowac parametrami, zasobowej nie.** Baza w rogu: balans
   0,1419 -> 0,6058 (+0,4639). Bogata strefa: 0,1378 -> 0,1474 (**+0,0096**). Roznica prawie
   piecdziesieciokrotna. Wskazowka projektowa: rozmieszczenie zasobow wymaga wiekszej starannosci
   niz rozmieszczenie pozycji startowych.
3. **Podloga nierownowagi zalezy od parametrow swiata.** W swiecie ubogim z drogimi jednostkami
   pojedyncze przegrane starcie waży bardzo duzo, wiec nawet idealnie symetryczna plansza konczy
   z nierownowaga 14 %; w swiecie bogatym z tanimi jednostkami ta sama plansza schodzi do 6,4 %.
   Udzial mapy w wyniku rosnie wraz z bogactwem swiata, bo maleje udzial przypadku — co tlumaczy,
   dlaczego NSGA-II zbiegl wlasnie tam.

Bramka podboju nadal dziala: mapa z bazami obok siebie osiaga podboj 32,5 % (bylo 6,2 %) i wciaz
zostaje odrzucona progiem 60 %.

**WARIANT HISTORYCZNY — geny 12 / 60 / 700** (`mapy_kontrolne_wyniki.json`): wzorzec 0,4148 >=
losowa 0,1644 > najlepsza zepsuta 0,1419. Rozdzielczosc miedzy dwiema mapami zepsutymi wzrosla po
zmianie metryk z 0,0025 do 0,2051, czyli 82-krotnie.

Pelne omowienie w `WSKAZOWKI_DO_PRACY.md` rozdz. 7.

### Kontrola granic zakresow genow — wykonana

`test_granic_genow.py`, 14 konfiguracji x 60 meczow = 840 meczow, ok. 17 min. Dwa jednowymiarowe
przemiaty wokol optimum, siegajace poza zadeklarowany genotyp.

- `population_max` 90–200 (granica GDD: 100): najlepszy w zakresie 0,8365 / 0,8367, najlepszy poza
  zakresem 0,8410 / 0,8412. Roznica +0,0045 przy progu istotnosci 0,0136.
- `populationToCreateNewUnit` 150–800 (granica GDD: 400): najlepszy w zakresie 0,8303 / 0,8312,
  najlepszy poza zakresem 0,8385 / 0,8337. Roznica +0,0081 przy progu istotnosci 0,0136.

**Wniosek: zakres genotypu obejmowal optimum.** Poza granicami ocena sie nasyca. Dodatkowo
optimum lezy na **plaskowyzu**, nie w ostrym maksimum — 13 z 14 konfiguracji dostalo ocene
0,82–0,84. Jedyny wyrazny spadek to `populationToCreateNewUnit` = 800 (dynamizm 0,5345).

Najmocniejszy argument znaleziono jednak przez analize samej funkcji przystosowania: **sufit
matematyczny wyjscia systemu rozmytego wynosi 0,8667**, a znalezione optimum osiaga 0,8365, czyli
96,5 % tego sufitu. Nawet mapa idealna (wszystkie nierownowagi = 0) dalaby tylko +0,0302. Nie bylo
wiec czego obcinac. Pelne omowienie w `WSKAZOWKI_DO_PRACY.md` rozdz. 8, dane:
`granice_genow_wyniki.json`.

### Co zostało

**Stan systemu oceny po wszystkich zmianach** — zweryfikowany na pilotazu 50 x 60 meczow:

| | |
|---|---|
| BALANS | nierownowaga terytorialna, gospodarcza, militarna — 27 regul |
| DYNAMIZM | wskaznik odbijania, zmiany prowadzenia na 100 tur — 6 regul |
| bramki | dlugosc gry >= 15 %, wskaznik podboju >= 60 % |
| diagnostyczne | punkty kulminacyjne, bitwy polowe |
| zakresy ocen | balans 0,1340–0,8335 · dynamizm 0,1474–0,8650 |
| korelacja obu ocen | +0,586 |

- [ ] Opisać wyniki w pracy według `WSKAZOWKI_DO_PRACY.md`, w tym **rozdz. 4.5** (pelne porownanie
      metryk z artykulem) i **rozdz. 4.6** (dlaczego dlugosc gry, wskaznik podboju i punkty
      kulminacyjne nie sa wejsciami systemu, choc w artykule byly)
- [ ] Opcjonalnie: rozszerzyć weryfikację o mapy oparte na zasadach projektowych StarCrafta
- [ ] Opcjonalnie: doprecyzować pomiar przewagi pierwszego ruchu (400 par zamiast 100)
- [ ] Opcjonalnie: wspólne ziarna losowe dla precyzyjniejszego porównywania chromosomów

---

## 5. Jak uruchamiać

**Zawsze zamknij edytor Unity przed uruchomieniem.** Tryb wsadowy potrzebuje wyłącznego dostępu
do projektu i inaczej kończy się błędem „another Unity instance is running".

```bash
# glowny eksperyment - pelny przebieg, okolo 12 godzin (JUZ WYKONANY)
python nsga2_optymalizacja.py

# sprawdzenie, czy optimum nie zostalo obciete granicami zakresow genow (~17 min, JUZ WYKONANY)
python test_granic_genow.py

# ten sam kod w wersji skroconej, do sprawdzenia czy wszystko dziala (~30 min)
python nsga2_optymalizacja.py test

# kalibracja progow (jesli zmieni sie mechanika gry)
python pilotaz.py

# weryfikacja na mapach kontrolnych
python test_mapy_kontrolne.py

# pomiar przewagi pierwszego ruchu
python test_pierwszy_ruch.py
```

Ścieżka do Unity jest zapisana na stałe w `pipeline_fuzzy.py` (`UNITY_EXE`).

### Ważne szczegóły techniczne

- **Unity NIE dostaje flagi `-quit`.** `BatchRunner.RunSim` tylko włącza tryb gry i natychmiast
  wraca, więc `-quit` zamknąłby edytor przed startem symulacji. Proces kończy
  `EditorApplication.Exit(0)` po zapisaniu wyników, a zabezpieczeniem jest timeout.
- **`MECZOW_NA_CHROMOSOM` w `pipeline_fuzzy.py` musi być równe `batchSimulationCount` w scenie
  Unity.** Obecnie oba wynoszą 60. Zmiana wymaga edycji obu miejsc.
- Raporty tekstowe w trybie wsadowym trafiają do `Wyniki_Batch/` (poza `Assets/`), żeby nie
  zamulać importu w Unity.
- W trybie RealTime (gra z edytora) wyniki idą do `metrics_output_realtime.json`, żeby nie
  nadpisać wyników pętli Pythona.

---

## 6. Najważniejsze wyniki badawcze

Pełne omówienie w `WSKAZOWKI_DO_PRACY.md`. W skrócie:

1. **Metryki z artykułu nie przenoszą się wprost na inną grę.** Trzy z siedmiu wymagały
   przedefiniowania lub wycofania, bo w tej mechanice mierzyły coś innego, niż zakładali autorzy.
   Najtwardszy dowód: prog zbioru WYSOKI dla Reconquering Rate wynosi u autorow 0,1 na ture, a caly
   nasz rozklad miesci sie w przedziale 0,0006–0,0036 — **zero z 50 konfiguracji** osiagneloby ten
   zbior, wiec kazda mapa dostalaby dynamizm NISKI. Przyczyna: u nich mapa ma 15–30 planet, u nas
   360 pol, wiec jedno przejecie to 3–7 % kontra 0,28 % mapy. Metryki normalizowane przez liczbe
   obiektow nie sa przenosne miedzy grami o roznej ziarnistosci mapy.

2. **Balans i dynamizm w tej grze KOOPERUJĄ, a nie konkurują.** Korelacja ocen +0,54 do +0,60,
   potwierdzona na trzech różnych zestawach metryk. W artykule były sprzeczne. Przyczyną jest
   efekt kuli śnieżnej: przewaga terytorialna napędza gospodarkę ze współczynnikiem 1,14,
   a w grze nie ma mechaniki powrotu. Mapy niezbalansowane są więc automatycznie nudne.
   **To jest główny wynik pracy** — promotor zaakceptował go jako wniosek.

3. **Progi funkcji przynależności dobrane „na wyczucie" nie działają.** Przed kalibracją
   dynamizm rozróżniał mapy w zakresie 0,02 na skali 0–1; po kalibracji 0,70.

4. **Mapa wyjaśnia tylko 12–53 % zmienności wyniku.** Reszta to losowość symulacji. Dlatego
   ocena jednego chromosomu wymaga kilkudziesięciu meczów.

5. **Analiza statystyczna wykryła dwie wady niewidoczne w kodzie**: przewagę pozycyjną bazy nr 1
   i fałszywe raportowanie remisów.

6. **Front Pareto zapadł się do jednego punktu**, a jego pozorna szerokość to szum pomiarowy.
   Jest to bezposrednia konsekwencja punktu 2 i sam w sobie wynik wart opisania.

7. **Optimum to plaskowyz, a nie szczyt.** Przemiat poza granicami zakresow genow pokazal, ze
   caly rejon `population_max` >= 100 i `populationToCreateNewUnit` <= 500 daje oceny
   nierozroznialne statystycznie. Zalecenie projektowe jest wiec odporne, a nie wyostrzone.

8. **Skala oceny rozmytej jest silnie scisnieta u gory.** Sufit wyjscia wynosi 0,8667, a rejon
   optimum osiaga 0,8365. Prawie cala zdolnosc rozrozniania systemu miesci sie w waskiej strefie
   przejscia miedzy ocena 0,5 a 0,83. To ograniczenie metody, o ktorym trzeba napisac uczciwie.

---

## 7. Kontekst od promotora

Promotor (Rafał Szrajber) zaakceptował kierunek: **nie szukać innych metryk tylko po to, by
wyszła sprzeczność jak w artykule**, lecz opisać własny wynik i go zweryfikować.

Poprosił o weryfikację „na bazie czegoś, co dobrze funkcjonuje w zbliżonej grze". Zrealizowano to
przez mapy wzorcowe z symetrią obrotową 180° — standard projektowy map turniejowych 1v1 na
sztywnych siatkach. Weryfikacja została zaliczona i opisana w GDD §8.2 oraz w rozdziale 7
wskazówek.

Pozostaje opcjonalne rozszerzenie: mapy budowane według pełnych zasad projektowych map 1v1
ze StarCrafta II lub Warcrafta III oraz ocena ekspercka przez graczy.

---

## 8. Zasady współpracy, które się sprawdziły

- **Nie zgadywać — sprawdzać w kodzie albo w danych.** Kilka razy intuicja okazała się błędna,
  a rozstrzygał dopiero pomiar. Przy pytaniu „co robi ta metryka" należy przeczytać kod, a nie
  pisać skrypt zgadujący.
- **Odróżniać „nie wykryto" od „nie ma".** Przy każdym wyniku nieistotnym statystycznie podawać,
  jak duży efekt test w ogóle mógł wykryć.
- **Nie chować wyników negatywnych.** Dwie próby dodania trzeciego wymiaru dynamizmu nie
  zadziałały i to również jest materiał do pracy.
- **Po każdej zmianie w kodzie aktualizować GDD**, tak by dokument i implementacja zawsze się
  zgadzały. Do sprawdzania zgodności tabel reguł istniał skrypt porównujący docx z kodem.
- Użytkownik pisze po polsku i prosi o prosty, konkretny język bez żargonu.
