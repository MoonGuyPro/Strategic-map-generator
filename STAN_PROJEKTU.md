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

Artykuł źródłowy: Lara-Cabrera, Nogueira-Collazo, Cotta, Fernández-Leiva, *Procedural Content
Generation for Real-Time Strategy Games*, IJIMAI 2015. Autorzy oceniali mapy do gry Planet Wars
za pomocą siedmiu metryk i logiki rozmytej, a optymalizowali je algorytmem NSGA-II.

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
| `pilotaz_wyniki.json` | 50 konfiguracji × 60 metryk — podstawa kalibracji progów |
| `mapy_kontrolne_wyniki.json` | wyniki weryfikacji na mapach kontrolnych |
| `Wyniki_Batch/` | raport tekstowy z każdego meczu (~2400 plików) |
| `nsga2_front.json` / `.csv` | front Pareto po zakończeniu optymalizacji |
| `nsga2_postep.json` | stan po każdym pokoleniu (zabezpieczenie przed przerwaniem) |

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
- DYNAMIZM ← zmiany prowadzenia na 100 tur, Reconquering Rate, Peak Differences (18 reguł)
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
- [x] Peak Differences rozszerzone na trzy zasoby i uśrednione; wartość pożądana = ŚREDNIA
- [x] Metryka zmian prowadzenia (na 100 tur) jako trzecie wejście dynamizmu
- [x] Bitwy polowe mierzone jako metryka diagnostyczna (poza systemem rozmytym)
- [x] Kalibracja progów na kwantylach z 2 × 50 konfiguracji × 20 meczów
- [x] Punkt nasycenia zbioru WYSOKI przesunięty na maksimum z pilotażu
- [x] Naprawiona definicja siły militarnej (tokeny + garnizon bazy, w procentach)
- [x] Naprawione raportowanie remisów (17 % meczów było fałszywie przypisywanych)
- [x] Usunięta systematyczna przewaga bazy nr 1 (było 55,8 % zwycięstw, jest 48,4 %)
- [x] Sztywna naprzemienność kolejności ruchu w trybie wsadowym
- [x] Zmierzona przewaga pierwszego ruchu (nie wykryto; 53,6 % przy 1,0 sigma)
- [x] Weryfikacja na mapach kontrolnych i wzorcowych — **zaliczona**
- [x] Cała populacja oceniana w jednym uruchomieniu Unity
- [x] Zabezpieczenia: kasowanie starego wyniku, timeout, kontrola liczby wyników
- [x] NSGA-II zaimplementowany i przetestowany na sztucznej funkcji oceny

### Co zostało

- [ ] **Uruchomić pełny przebieg NSGA-II** (~12 h) i zinterpretować front Pareto
- [ ] Opisać wyniki w pracy według `WSKAZOWKI_DO_PRACY.md`
- [ ] Opcjonalnie: rozszerzyć weryfikację o mapy oparte na zasadach projektowych StarCrafta
- [ ] Opcjonalnie: doprecyzować pomiar przewagi pierwszego ruchu (400 par zamiast 100)
- [ ] Opcjonalnie: wspólne ziarna losowe dla precyzyjniejszego porównywania chromosomów

---

## 5. Jak uruchamiać

**Zawsze zamknij edytor Unity przed uruchomieniem.** Tryb wsadowy potrzebuje wyłącznego dostępu
do projektu i inaczej kończy się błędem „another Unity instance is running".

```bash
# glowny eksperyment - pelny przebieg, okolo 12 godzin
python nsga2_optymalizacja.py

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

2. **Balans i dynamizm w tej grze KOOPERUJĄ, a nie konkurują.** Korelacja ocen +0,54 do +0,60,
   potwierdzona na trzech różnych zestawach metryk. W artykule były sprzeczne. Przyczyną jest
   efekt kuli śnieżnej: przewaga terytorialna napędza gospodarkę ze współczynnikiem 1,23,
   a w grze nie ma mechaniki powrotu. Mapy niezbalansowane są więc automatycznie nudne.
   **To jest główny wynik pracy** — promotor zaakceptował go jako wniosek.

3. **Progi funkcji przynależności dobrane „na wyczucie" nie działają.** Przed kalibracją
   dynamizm rozróżniał mapy w zakresie 0,02 na skali 0–1; po kalibracji 0,70.

4. **Mapa wyjaśnia tylko 12–53 % zmienności wyniku.** Reszta to losowość symulacji. Dlatego
   ocena jednego chromosomu wymaga kilkudziesięciu meczów.

5. **Analiza statystyczna wykryła dwie wady niewidoczne w kodzie**: przewagę pozycyjną bazy nr 1
   i fałszywe raportowanie remisów.

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
