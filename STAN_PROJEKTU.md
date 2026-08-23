# Stan projektu — przekazanie do nowej sesji

Plik dla asystenta w nowym oknie czatu. Opisuje, czym jest projekt, co już zrobiono, jak
uruchamiać poszczególne elementy i co zostało do zrobienia.

**Trzy dokumenty czyta się razem:**

| plik | zawartość |
|---|---|
| `GDD_zaktualizowany.docx` | pełna specyfikacja gry, metryk i systemu rozmytego (aktualna, zgodna z kodem) |
| `WSKAZOWKI_DO_PRACY.md` | wyniki badawcze, wnioski i rady do pisania pracy magisterskiej |
| **`STAN_PROJEKTU.md`** (ten plik) | struktura kodu, sposób uruchamiania, stan prac, następne kroki |

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
| `test_mapy_kontrolne.py` | weryfikacja na mapach wzorcowych i celowo zepsutych |

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
| `mapy_kontrolne_wyniki.json` | wyniki weryfikacji na mapach kontrolnych |
| `granice_genow_wyniki.json` | przemiat poza granicami zakresów genów (kontrola metodologiczna) |
| `Wyniki_Batch/` | raport tekstowy z każdego meczu (ponad 30 000 plików) |
| `nsga2_front.json` / `.csv` | front Pareto oraz `historia` — 428 ocen z numerem pokolenia; stąd odtwarza się krzywą hiperobjętości |
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
- [x] Bazy reguł uzupełnione do kompletu (27 i 18 kombinacji, zero dziur)
- [x] Conquering Rate zdegradowany do bramki poprawności
- [x] Peak Differences liczone na trzech zasobach i uśrednione (korelują +0,88 do +0,96)
- [x] **Peak Differences wdrożone wg wzoru (7) z artykułu** — amplituda wahnięcia `max(d) − min(d)`
      przy różnicy ZE ZNAKIEM, zamiast maksimum modułu. Przy okazji ujednolicona normalizacja
      wszystkich trzech pików (wcześniej terytorialny dzielił się przez całą planszę, a pozostałe
      przez stan posiadania obu botów)
- [x] **Reconquering Rate wdrożony wg wzoru (6) z artykułu** — średnia na turę zamiast sumy;
      eksportowany jako „procent pól zmieniających właściciela na 100 tur"
- [x] Metryka zmian prowadzenia (na 100 tur) jako trzecie wejście dynamizmu
- [x] Bitwy polowe mierzone jako metryka diagnostyczna (poza systemem rozmytym)
- [x] **Peak Differences przeniesione do metryk diagnostycznych (wariant C)** — baza reguł
      dynamizmu z 18 na 6, bez utraty rozdzielczosci ocen. Powod zmierzony: +0,930 z nierownowaga
      terytorialna, ujemnie z odbijaniem i bitwami
- [x] Kalibracja progów na kwantylach — **przeliczona po zmianie metryk**, 50 konfiguracji × 60
      meczów, wszystkie progi z jednego odtwarzalnego pliku `pilotaz_wyniki.json`
- [x] Punkt nasycenia zbioru WYSOKI przesunięty na maksimum z pilotażu
- [x] Naprawiona definicja siły militarnej (tokeny + garnizon bazy, w procentach)
- [x] Naprawione raportowanie remisów (17 % meczów było fałszywie przypisywanych)
- [x] Usunięta systematyczna przewaga bazy nr 1 (było 55,8 % zwycięstw, jest 48,4 %)
- [x] Sztywna naprzemienność kolejności ruchu w trybie wsadowym
- [x] Zmierzona przewaga pierwszego ruchu (nie wykryto; 53,6 % przy 1,0 sigma)
- [x] Weryfikacja na mapach kontrolnych i wzorcowych — **zaliczona**
- [x] Cała populacja oceniana w jednym uruchomieniu Unity
- [x] Zabezpieczenia: kasowanie starego wyniku, timeout, kontrola liczby wyników
- [x] NSGA-II zaimplementowany, przetestowany i **uruchomiony w pelnym przebiegu**
- [x] Przemiat poza granicami zakresow genow — **zakres genotypu obejmowal optimum**

### Wynik glownego eksperymentu

Przebieg NSGA-II: populacja 20, 25 pokolen, 60 meczow na ocene, **428 ocenionych konfiguracji**,
11,7 godziny, zero odrzucen przez bramki poprawnosci.

**Znalezione optimum:** `population_max` ok. 99 · `populationToCreateNewUnit` ok. 420 ·
`minSpawnDistance` ok. 10. Czyli: bogaty swiat, tanie jednostki, umiarkowany dystans startowy.

**Zbieznosc:** hiperobjetosc 0,6912 → 0,7051, plaska od pokolenia 16. 99,4 % wyniku osiagnieto
w pierwszych 20 % czasu — 15 pokolen w zupelnosci wystarcza.

**Front Pareto ma 4 rozwiazania, ale jest to szum, nie kompromis.** Rozpietosc frontu (balans
0,0072) jest mniejsza lub rowna odchyleniu standardowemu ocen w tym samym rejonie przestrzeni
(0,0068 dla balansu, 0,0138 dla dynamizmu, na 102 chromosomach). To empiryczne potwierdzenie
wyniku glownego: skoro cele kooperuja, optymalizacja wielokryterialna degeneruje sie do
jednokryterialnej.

Pelne omowienie w `WSKAZOWKI_DO_PRACY.md` rozdz. 8. Dane: `nsga2_front.json`, `nsga2_front.csv`,
`nsga2_postep.json`.

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

- [ ] **Pelny przebieg NSGA-II na nowym systemie** — `python nsga2_optymalizacja.py`, okolo 12 godzin.
      Wyniki z poprzedniego przebiegu liczone sa starymi wzorami, starymi progami i stara baza regul,
      wiec nie sa porownywalne. Oczekiwanie: optimum sie nie przesunie, bo wyznaczaja je metryki
      balansu, ktorych wzory sie nie zmienily, a przemiat granic pokazal szeroki plaskowyz.
      **Po przebiegu zaktualizowac rozdz. 8 wskazowek** (front, hiperobjetosc, optimum) i tabele
      liczb w rozdz. 9.
- [ ] **Powtorzyc weryfikacje na mapach kontrolnych** — `python test_mapy_kontrolne.py`, okolo
      15 minut. Oceny w rozdz. 7 wskazowek pochodza sprzed zmiany metryk i progow.
- [ ] **Powtorzyc przemiat granic genow** — `python test_granic_genow.py`, okolo 17 minut, jesli
      chcesz miec rozdz. 8 w calosci na nowych metrykach.
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
