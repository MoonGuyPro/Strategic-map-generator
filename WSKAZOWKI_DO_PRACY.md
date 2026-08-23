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
3. **Balans i dynamizm w tej grze nie są sprzeczne, tylko idą w parze.** W artykule są „partially
   conflicting", i to wyłącznie na górnym końcu skali balansu — dokładne sformułowanie i cytaty
   w rozdz. 5.

To nie są porażki wdrożenia. To są wyniki badawcze i wokół nich zbudowałbym całą pracę.

**Czwarta rzecz, o mniejszym ciężarze, ale ważna metodologicznie:** genem jest tu przepis na mapę,
a nie mapa. To zmienia sens wyniku — dostajesz regułę projektową zamiast jednego artefaktu — i jest
drugim, osobnym źródłem szumu, który zniszczył front Pareto. Uzasadnienie wyboru w rozdz. 3,
porównanie z artykułem w rozdz. 11.2, odpowiedź na zarzut recenzenta w rozdz. 11.5.

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

Zmierzone korelacje genów z metrykami (pilotaż: 50 konfiguracji Latin Hypercube × 60 meczów,
`pilotaz_wyniki.json` — wszystkie liczby poniżej są z tego jednego pliku i w pełni odtwarzalne):

| gen | bitwy polowe | odbijanie | nierównowaga militarna | nierównowaga terytorialna |
|---|---:|---:|---:|---:|
| `population_max` | **+0,87** | **+0,85** | **−0,81** | −0,50 |
| `populationToCreateNewUnit` | −0,49 | −0,54 | −0,04 | −0,41 |
| `minSpawnDistance` | +0,39 | +0,39 | −0,16 | +0,10 |

- **`population_max` dominuje** — najsilniejsza dźwignia w całym systemie, i to jednocześnie na
  dynamikę (więcej bitew, więcej odbijania) i na balans (mniejsza nierównowaga militarna).
  **To jest bezpośrednia, mierzalna przyczyna zbieżności obu celów** i warto tę tabelę pokazać
  w rozdziale z wynikami obok korelacji ocen.
- **`populationToCreateNewUnit` działa umiarkowanie i głównie na dynamikę**: −0,49 z bitwami,
  −0,54 z odbijaniem, ale **−0,04 z nierównowagą militarną**, czyli praktycznie zero. Wcześniejsza
  wersja notatki podawała „około −0,5 na te same metryki" — dla nierównowagi militarnej to
  nieprawda i nie wolno tego przepisać do pracy.
- **`minSpawnDistance` jest najsłabszy**: maksymalnie +0,39.

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

### Dlaczego genem jest przepis, a nie mapa — uzasadnienie do napisania

Czym różni się nasz genotyp od genotypu z artykułu, opisuje tabela w rozdz. 11.2. Tutaj zbieram
**uzasadnienie wyboru**, bo to jest osobne pytanie i recenzent zada je wprost.

1. **Przestrzeń rozwiązań jest tu o rząd wielkości większa.** Mapa w Planet Wars to 15–30 planet,
   czyli 60–120 liczb w chromosomie. Tutaj plansza to 400 heksów, każdy z typem terenu i wartością
   populacji — około 800 liczb przy sztywnej strukturze siatki. Ewolucja bezpośrednia wymagałaby
   operatorów naprawczych pilnujących niezmienników generatora (dokładnie 40 pól wody, baza
   otoczona sześcioma polami lądu, stała pula populacji), a każda naprawa psuje dziedziczenie:
   potomek przestaje przypominać rodziców, więc krzyżowanie traci sens.
2. **Koszt oceny jest nieporównanie wyższy.** Planet Wars jest grą czasu rzeczywistego, w której
   mecz trwa sekundy — autorzy wykonali 10 000 ewaluacji na przebieg, i to dziesięciokrotnie.
   Tutaj ocena jednego chromosomu to 60 meczów po 200–460 tur, a cały przebieg NSGA-II zajął
   11,7 godziny na 428 chromosomów. Szersza przestrzeń wymagałaby większej populacji, a budżet
   czasowy rośnie wielokrotnie.
3. **Cel pracy jest inny niż u autorów.** Oni chcieli dostarczyć zestaw grywalnych plansz. Tu
   pytanie brzmi: **jakie właściwości świata sprawiają, że mapy wychodzą zbalansowane i dynamiczne**.
   Wynik „bogaty świat, tanie jednostki, umiarkowany dystans startowy" da się przenieść na inną grę;
   konkretna plansza — nie.
4. **Wynik jest odporny na przeuczenie.** Chromosom oceniany jest na 60 różnych planszach, więc
   wysoka ocena oznacza, że dobra jest **cała rodzina** map z danego przepisu. Genotyp „mapa"
   optymalizuje jeden artefakt i może wypromować planszę, która akurat pasuje do słabości bota,
   a nie taką, która jest dobrze zaprojektowana.

Cena, którą trzeba uczciwie wymienić w ograniczeniach:

- **Generator nie steruje topografią.** Żaden gen nie decyduje o kształcie lądu, przesmykach ani
  spójności obszarów. To realne zawężenie względem artykułu, w którym pozycje planet były
  bezpośrednio ewoluowane — i właśnie dlatego autorzy mogli sformułować wnioski o **geometrii**
  mapy (planety rozrzucone szerzej, układ mniej regularny), a my nie możemy.
- **Dochodzi drugie źródło szumu.** Do losowości symulacji przy ustalonej mapie (12–53 % zmienności,
  rozdz. 6.4) dochodzi losowanie planszy z rodziny. Przy opisie zapadnięcia się frontu Pareto
  rozdziel oba źródła, zamiast mówić ogólnie o „szumie pomiarowym".
- **Nie da się wskazać jednej najlepszej mapy.** Można wygenerować przykładową planszę z najlepszego
  przepisu i pokazać ją jako rysunek, ale trzeba ją podpisać jako **próbkę z rozkładu**, a nie jako
  rozwiązanie. Warto to zrobić — tytuł pracy mówi o generowaniu map i ten rysunek zamyka lukę
  między tytułem a wynikiem.

Gdyby został czas: **procedura dwustopniowa** — NSGA-II znajduje przepis, a następnie krótki drugi
przebieg ewoluuje już konkretne plansze w obrębie tego przepisu, z operatorami naprawczymi
zawężonymi do jednej rodziny map. Łączy oba podejścia i jest naturalnym zamknięciem rozdziału
o kierunkach dalszych badań.

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
z nierównowagą terytorialną wzrosła z +0,47 do **+0,94** — metryka zaczęła mierzyć mapę, a nie styl
gry. (Wartość po zmianie przeliczona z nowego pilotażu, 50 × 60 meczów: +0,9368. Wartości sprzed
zmiany definicji nie da się odtworzyć, bo nie zachował się plik z tamtego przebiegu — w pracy podaj
ją jako pomiar historyczny albo powtórz pomiar.)

**Ważne przy opisie — to nie jest odstępstwo od artykułu, tylko powrót do jego definicji.** Autorzy
definiują growth imbalance jako różnicę „in the capacity for producing new ships", czyli sumę
rozmiarów posiadanych planet. Suma populacji posiadanych pól jest dokładnym odpowiednikiem.
Pierwotna implementacja (stany kont botów) była naszym własnym odejściem od artykułu, wykrytym
dopiero w danych. Opisz to w tej kolejności: własna nadinterpretacja definicji → wykrycie pomiarem →
powrót do sformułowania źródłowego. Tak opisane jest to mocniejsze niż „zmieniliśmy metrykę", bo
pokazuje, że analiza danych wyłapała rozjazd między implementacją a literaturą.

**Efekt uboczny wart opisania:** współczynnik regresji Growth względem Territorial wynosi **1,17**
(regresja liniowa na 50 konfiguracjach z `pilotaz_wyniki.json`, 60 meczów każda). Wcześniejsze
wersje notatki podawały 1,23; na zachowanych danych wychodzi 1,17, więc podawaj tę liczbę.
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

### 4.3. Peak Differences — wzór z artykułu wdrożony, kierunek metryki się NIE odwrócił

To jest najmocniejszy pojedynczy wynik metodologiczny w całej pracy. Poświęć mu osobny podrozdział.

**Sprostowanie terminologiczne.** W artykule peak difference **od początku jest rodziną trzech
zmiennych** — osobno dla planet, zdolności produkcyjnej i statków — używanych w regułach niezależnie.
Naszą zmianą nie jest więc „rozszerzenie na trzy zasoby", tylko **uśrednienie ich do jednego
wejścia**. Uzasadnienie zostaje: korelują ze sobą od **+0,88 do +0,96**, więc rozdzielanie ich nie
wniosłoby informacji.

**Co mówi wzór (7).**

    Δ = max_j [ (φ¹−φ²) / (φ¹+φ²) ] − min_j [ (φ¹−φ²) / (φ¹+φ²) ]

Różnica brana jest **ze znakiem**: najlepszy moment gracza 1 minus najlepszy moment gracza 2, zakres
0–2. Metryka mierzy **amplitudę wahnięcia prowadzenia**, a nie głębokość przewagi. Autorzy piszą
wprost, czego się po niej spodziewają: „if the game is very imbalanced and one player thoroughly
dominates the other, these variables may take lower values than in games in which the dominated
player makes a comeback".

Pierwotna implementacja liczyła co innego — maksimum **wartości bezwzględnej** dysproporcji, zakres
0–1 — czyli głębokość dominacji zamiast amplitudy. Taka metryka z definicji nie odróżnia comebacku
od dominacji. **Wzór (7) został wdrożony** (`BotTurnManager.cs`, zapamiętywanie minimum i maksimum
różnicy ze znakiem dla wszystkich trzech zasobów; przy okazji ujednolicono normalizację — wcześniej
pik terytorialny dzielił się przez całą planszę, a dwa pozostałe przez stan posiadania obu botów).
Pilotaż powtórzono: 50 konfiguracji × 60 meczów.

**Wynik: kierunek metryki się nie odwrócił.** Tercyle z nowego pilotażu, nowa metryka:

| pik | reconquering | bitwy polowe | zmiany prow./100 tur | teryt % | growth % | mil % |
|---|---:|---:|---:|---:|---:|---:|
| niski (71,1 %) | **22,9** | **50,5** | **3,10** | 11,1 | 16,1 | 13,8 |
| średni (80,8 %) | 19,4 | 37,4 | 2,75 | 14,3 | 19,9 | 20,1 |
| wysoki (91,2 %) | 14,9 | 25,7 | 1,95 | 18,7 | 25,6 | 26,4 |

Zależność jest monotoniczna i idzie **w stronę przeciwną do zakładanej w artykule**: im większa
amplituda wahnięcia, tym mniej przejęć pól, mniej bitew i rzadsze zmiany prowadzenia.

**Co więcej, wzór z artykułu pogłębił problem, zamiast go rozwiązać.** Korelacja pika z nierównowagą
terytorialną:

| wersja metryki | korelacja z nierównowagą terytorialną | z odbijaniem | z bitwami |
|---|---:|---:|---:|
| stara (maksimum modułu) | +0,893 | −0,456 | −0,498 |
| **nowa, wzór (7) z artykułu** | **+0,930** | −0,304 | −0,399 |

**Dlaczego tak jest — mechanizm do opisania.** Wzór (7) zakłada, że w grze zdarzają się powroty.
Mecz startuje symetrycznie, więc d = 0 w turze zerowej, a `max(d) − min(d)` rozkłada się na
„jak daleko zaszedł zwycięzca" plus „jak daleko zaszedł przegrany w swoim najlepszym momencie".
W grze z efektem kuli śnieżnej i bez mechaniki wyrównywania szans ten drugi składnik to wyłącznie
szum z pierwszych kilkudziesięciu tur, kiedy obaj boci mają jeszcze po kilka pól. Amplituda redukuje
się więc do głębokości ostatecznej dominacji powiększonej o niemal stałą wartość — i dlatego mierzy
balans, a nie dramaturgię.

**Jak to sformułować w pracy.** To już nie jest „uproszczenie" ani pomyłka implementacyjna, tylko
**zweryfikowany wynik**: metryka zdefiniowana wzorem z artykułu, zaimplementowana dokładnie tak jak
tam, mierzy w tej grze co innego, niż zakładali autorzy. Dowód jest mocny, bo pochodzi z porównania
obu wersji na tym samym generatorze. Jest to zarazem **ilościowe rozwinięcie tego, co autorzy sami
napisali** — że ich definicja dynamizmu „implicitly incorporates a component of balance via the
peak-difference variables". My tę zawartość zmierzyliśmy: +0,93.

### 4.3.1. Problem, który to rodzi — i podjęta decyzja (wariant C, wdrożony)

Skoro pik koreluje z nierównowagą terytorialną na +0,93, a nierównowaga terytorialna jest wejściem
BALANSU, to trzymanie pika wśród wejść DYNAMIZMU sprawia, że **dynamizm częściowo mierzy balans**.
Korelacja obu ocen — czyli wynik główny pracy — byłaby wtedy po części wytworzona przez konstrukcję
systemu, a nie przez mechanikę gry. Recenzent postawi ten zarzut i trzeba mieć na niego liczby.

Policzone na nowym pilotażu, te same progi, trzy warianty bazy reguł dynamizmu:

| wariant | zakres ocen | korelacja z BALANSEM |
|---|---|---:|
| A — pik pożądany ŚREDNI (obecna baza 18 reguł) | 0,156–0,833 | +0,651 |
| B — pik pożądany NISKI (zgodnie z pomiarem) | 0,156–0,833 | **+0,834** |
| C — **bez pika**, tylko zmiany prowadzenia × reconquering | 0,147–0,865 | **+0,586** |

**Najważniejsze: wynik główny przeżywa usunięcie pika.** Nawet w wariancie C, gdzie dynamizm nie ma
żadnego wspólnego wejścia z balansem i nie zawiera niczego, co mierzyłoby nierównowagę, korelacja
wynosi **+0,586**. Zbieżność celów nie jest artefaktem konstrukcji systemu rozmytego. To jest
odpowiedź na najgroźniejszy zarzut wobec całej pracy i trzeba ją umieścić w rozdziale z wynikami.

Wariant B odpada właśnie dlatego, że jest najwyższy: +0,834 bierze się stąd, że pik z odwróconym
kierunkiem staje się po prostu czwartą metryką balansu. Byłoby to mierzenie balansu dwa razy
i nazywanie tego zbieżnością celów.

**Wdrożono wariant C.** Uzasadnienie jest dokładnie tej samej natury co przy Conquering Rate
(rozdz. 4.2): metryka nie jest własnością metody, tylko relacją między metodą a mechaniką gry.
W Planet Wars pik mierzył zwroty akcji, bo tam powroty się zdarzały. Tutaj mierzy dominację, więc
jako wejście dynamizmu jest nie tylko bezużyteczny, ale wręcz szkodliwy — zaciera granicę między
dwoma kryteriami. Przenosimy go do metryk diagnostycznych, obok bitew polowych, i **raportujemy
w pracy jako mierzony, lecz nieużywany**.

Cena wariantu C: dynamizm zostaje z dwoma wejściami i bazą 6 reguł zamiast 18. Opisz to uczciwie
i pokaż, że baza nadal jest kompletna (2 × 3 kombinacje). Argument, że dwa wejścia wystarczą —
korelacja między nimi +0,616, zakres ocen bez zmian — jest w rozdz. 4.6.

**Stan po wdrożeniu** (`pipeline_fuzzy.py`, zweryfikowane na pilotażu 50 × 60 meczów):

| | wartość |
|---|---|
| reguł balansu / dynamizmu | 27 / 6 |
| zakres oceny BALANS | 0,1340 – 0,8335 |
| zakres oceny DYNAMIZM | 0,1474 – 0,8650 |
| korelacja BALANS × DYNAMIZM | **+0,586** |
| konfiguracji na dnie skali | 1 z 50 |
| odrzuconych przez bramki | 0 z 50 |

Pełny bilans przeniesienia wszystkich metryk — co identyczne, co zmienione, co dodane —
w **rozdz. 4.5**. Jest to gotowe podsumowanie rozdziału metodologicznego pracy.

**Wyniki NSGA-II wymagają powtórzenia.** Oceny z dotychczasowego przebiegu liczone są starymi
wzorami metryk, starymi progami i starą bazą reguł, więc nie są porównywalne z obecnym systemem.
Optimum najprawdopodobniej się nie przesunie — wyznaczają je metryki balansu, których wzory się nie
zmieniły, a przemiat granic pokazał szeroki płaskowyż — ale trzeba to pokazać, a nie założyć.

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

### 4.5. Bilans przeniesienia metryk — pełne porównanie z artykułem

**Stan docelowy w jednym miejscu** — co po zmianie mierzy które kryterium:

| | metryki |
|---|---|
| **BALANS** (3 wejścia, 27 reguł) | nierównowaga terytorialna · nierównowaga gospodarcza · nierównowaga militarna |
| **DYNAMIZM** (2 wejścia, 6 reguł) | wskaźnik odbijania · zmiany prowadzenia na 100 tur |
| **bramki poprawności** (zerują obie oceny) | długość gry ≥ 15 % · wskaźnik podboju ≥ 60 % |
| **mierzone, lecz nieużywane do oceny** | punkty kulminacyjne · bitwy polowe |

Poniżej to samo w rozbiciu na „identyczne z artykułem", „inne" i „dodane od siebie".

Zakłada wariant C z rozdz. 4.3.1. To jest tabela, którą recenzent przeczyta najuważniej, więc każda
pozycja ma podane uzasadnienie i miejsce, gdzie stoi dowód.

#### 4.5.1. Co jest identyczne

| metryka | definicja w artykule | u nas | uwagi |
|---|---|---|---|
| **Nierównowaga terytorialna** | średnia po turach z różnicy udziałów w posiadanych obiektach; mianownik obejmuje **całą** pulę, łącznie z neutralnymi | to samo, w procentach zamiast 0–1 | przenosi się bez żadnej zmiany |
| **Nierównowaga gospodarcza** | to samo dla „capacity for producing new ships", czyli sumy rozmiarów posiadanych planet | suma `populationNumber` posiadanych pól, czyli podstawa pasywnego przyrostu | **nasza korekta była powrotem do ich definicji**, nie odejściem od niej (rozdz. 4.1) |
| **Nierównowaga militarna** | to samo dla łącznej liczby posiadanych statków | suma armii tokenów **plus garnizon bazy** | zgodne: w Planet Wars statki stoją na planetach, łącznie z macierzystą, więc garnizon też wchodzi do tej metryki |
| **Wskaźnik odbijania** | wzór (6): średnia na turę z liczby pól zmieniających właściciela między graczami, dzielonej przez wielkość mapy | ten sam wzór, przeskalowany do „% pól na 100 tur" dla czytelności | identyczne po poprawce; wcześniej brakowało dzielenia przez liczbę tur |
| aparat wnioskowania | Mamdani, t-norma min, t-konorma max, defuzyfikacja środkiem ciężkości | to samo (`skfuzzy`) | |
| algorytm | NSGA-II, dwa kryteria maksymalizowane | to samo (`pymoo`) | |

Trzy z trzech metryk **balansu** przeniosły się bez zmian koncepcyjnych.

#### 4.5.2. Co jest inne

| metryka / element | w artykule | u nas | dlaczego |
|---|---|---|---|
| **Długość gry** | rozmyte wejście, użyte i w regule balansu nr 3, i w regule dynamizmu nr 7 (z modyfikatorem *very*) | **twarda bramka** ≥ 15 %, zerująca obie oceny | krótka gra to rozstrzygnięcie, nie cecha stopniowalna; jako wejście pozwalała mapie „bazy obok siebie" zdobyć wysoką ocenę |
| **Wskaźnik podboju** | rozmyte wejście dynamizmu (reguły 1 i 7) | **twarda bramka** ≥ 60 % | u nas zajęcie pola jest bezkosztowe i wymuszone przez drzewo priorytetów, więc K wynosi 90–99 % zawsze i niczego nie różnicuje (rozdz. 4.2) |
| **Punkty kulminacyjne** | trzy osobne wejścia dynamizmu (osobno dla terytorium, gospodarki i wojska), wartość WYSOKA = dobra, monotonicznie | **metryka diagnostyczna**, mierzona i raportowana, ale poza systemem | wzór (7) wdrożony dokładnie; w tej mechanice koreluje +0,930 z nierównowagą terytorialną i ujemnie z akcją (rozdz. 4.3) |
| liczba stanów lingwistycznych | 2 (LO / HI) na wszystkich wejściach | 3 (NISKI / ŚREDNI / WYSOKI), poza zmianami prowadzenia — 2 | trzy stany dają gradient tam, gdzie dwa dawały skok |
| kompletność bazy reguł | **świadomie niepełna** — 3 reguły balansu, 7 dynamizmu; działa, bo dwa nachodzące zbiory pokrywają całą dziedzinę | **pełna** — 27 reguł balansu (3³), 6 reguł dynamizmu (2 × 3), asercja kompletności przy starcie | przy trzech wąskich zbiorach pojawiały się dziury, więc komplet stał się koniecznością; **to nie jest poprawianie błędu autorów**, tylko konsekwencja naszej własnej zmiany |
| progi funkcji przynależności | rozpięte na zakresie teoretycznym zmiennej; jedyne dopasowanie to ściśnięcie dziedziny wskaźnika odbijania do 0,1 „in practice" | **kalibrowane na kwantylach rozkładu empirycznego** z pilotażu 50 × 60 meczów | systematycznej kalibracji w artykule nie ma; u nas to osobny rozdział pracy |
| modyfikatory lingwistyczne | *very* jako x², użyty przy T | nie używamy | konsekwencja przeniesienia T do bramki |
| zakres bramek | wskaźnik podboju wpływał tylko na dynamizm | nasze bramki zerują **obie** oceny | świadome zaostrzenie: mapa nierozegrana nie powinna dostawać punktów za balans |
| limit meczu | τmax = 400 tur | 500 tur | |
| agenci oceniający | **trzy różne** boty z Google AI Challenge 2010 | **dwa identyczne** boty na drzewie priorytetów | patrz rozdz. 5 — to jedna z przyczyn zaniku konfliktu celów |
| genotyp | sama mapa (15–30 planet, długość zmienna, samoadaptacja) | trzy parametry generatora | rozdz. 3 i 11.2 |

#### 4.5.3. Co dodajemy od siebie

| element | opis | po co |
|---|---|---|
| **Zmiany prowadzenia na 100 tur** | liczba przejęć prowadzenia terytorialnego, znormalizowana do długości meczu | w artykule nie ma takiej zmiennej. Jest za to **słowna definicja dynamizmu** — gracz, który „is at a disadvantage at a certain point can regain their position" — którą autorzy zoperacjonalizowali przez piki. My operacjonalizujemy ją wprost. W wariancie C jest to **jedyne wejście dynamizmu niezależne od balansu** |
| **Bitwy polowe** | liczba starć token vs token | metryka diagnostyczna; pokazuje, że dynamizm da się mierzyć również zdarzeniami militarnymi (korelacja +0,96 z odbijaniem, czyli redundantna — i to też jest wynik) |
| **Kalibracja empiryczna progów** | badanie pilotażowe, kwantyle, punkt nasycenia na maksimum rozkładu | cały etap metodologiczny, którego w artykule nie ma. Bez niego kryterium dynamizmu rozróżniało mapy w zakresie 0,02 na skali 0–1 |
| **Bramki poprawności** | oddzielenie „czy wynik w ogóle wolno oceniać" od „jak dobry jest" | u autorów obie funkcje pełniły te same zmienne rozmyte; rozdzielenie wykryło mapę, którą system inaczej oceniłby najwyżej (rozdz. 7) |
| **Programowa tabela decyzyjna z asercją** | reguły generowane ze słownika, kompletność sprawdzana przy starcie | gwarantuje zgodność dokumentacji z kodem i wyklucza sytuację bez aktywnej reguły |

#### 4.5.4. Wniosek, który wychodzi z tej tabeli sam

Policz, co przeżyło jako **wejście systemu rozmytego**:

| kryterium | metryki z artykułu | ile przeniesionych jako wejście | co się stało z resztą |
|---|---|---|---|
| **BALANS** | nierównowaga terytorialna, gospodarcza, militarna | **3 z 3** | jedna wymagała powrotu do definicji źródłowej |
| **DYNAMIZM** | wskaźnik podboju, wskaźnik odbijania, długość gry, punkty kulminacyjne | **1 z 4** — tylko wskaźnik odbijania | podbój i długość gry zostały bramkami, punkty kulminacyjne metryką diagnostyczną; wolne miejsce zajęły zmiany prowadzenia, czyli metryka własna |

**Kryterium balansu przenosi się między grami niemal w całości, kryterium dynamizmu prawie wcale.**
To jest mocniejsza i bardziej precyzyjna wersja tezy „metryki nie przenoszą się wprost", bo wskazuje
**gdzie dokładnie** leży granica przenośności.

Wyjaśnienie, które warto postawić obok: balans jest własnością **rozkładu stanu** — kto ile ma
w danej chwili — a to pojęcie znaczy to samo w każdej grze strategicznej z podziałem terytorium.
Dynamizm jest własnością **przebiegu w czasie** i zależy od tego, czy mechanika w ogóle dopuszcza
zjawiska, które te metryki zakładają: kosztowną ekspansję (wskaźnik podboju), powroty do gry
(punkty kulminacyjne) oraz rozstrzygnięcie przed limitem tur (długość gry). Żadne z tych trzech
założeń nie jest spełnione w badanej grze.

Do tego dochodzi argument o **ziarnistości mapy**, niezależny od powyższego: próg zbioru WYSOKI dla
wskaźnika odbijania zaczyna się w artykule przy 0,1 na turę, a cały nasz rozkład to 0,0006–0,0036, bo u nich jedno
przejęcie to 3–7 % mapy, a u nas 0,28 %. Metryka przenosi się więc **jako wzór, ale nie jako skala**
(rozdz. 12.3).

---

### 4.6. Dlaczego trzy metryki z artykułu nie wpływają na oceny — gotowe uzasadnienie

Recenzent zapyta o to na pewno: autorzy używali długości gry, wskaźnika podboju i punktów
kulminacyjnych jako pełnoprawnych wejść, a u nas żadna z tych trzech nie wpływa na ocenę. Poniżej
uzasadnienie dla każdej osobno, z liczbami. Wspólny mianownik: **żadnej z nich nie odrzuciliśmy
dlatego, że jest zła — odrzuciliśmy je dlatego, że w tej mechanice przestały nieść informację.**

#### Długość gry — bramka, nie wejście

W artykule długość gry wchodzi do reguły balansu („krótka gra oznacza, że jeden z graczy wygrał
łatwo") i do reguły dynamizmu. Ma to sens w Planet Wars, gdzie mecz kończy się w chwili utraty
wszystkich statków, więc może się urwać bardzo wcześnie.

W naszej grze mecz kończy się dopiero zdobyciem bazy przeciwnika, a baza ma garnizon 700 i limit
500 tur. Zmierzone długości:

| źródło | zakres długości gry | próg bramki |
|---|---|---|
| pilotaż nowy (50 konfiguracji) | 48,9–87,2 % | 15 % |
| pilotaż stary (50 konfiguracji) | 42,4–90,9 % | 15 % |
| mapy kontrolne, łącznie z celowo zepsutymi | 35,0–84,3 % | 15 % |

**Bramka nie odrzuciła jeszcze ani jednej konfiguracji** i trzeba to w pracy napisać wprost. Jako
wejście rozmyte metryka byłaby bezużyteczna: wszystkie mapy leżą w tym samym rejonie skali, więc
niczego by nie różnicowała. Zostaje jako zabezpieczenie na wypadek konfiguracji spoza przebadanej
przestrzeni — i jako taka jest tania, bo nic nie kosztuje.

**Rozważono też wariant odwrotny — nagradzanie długich meczów — i odrzucono go świadomie.** Warto
opisać to jako rozpatrzoną i odrzuconą alternatywę, bo pytanie jest naturalne: skoro krótka gra jest
zła, to czy długa nie powinna być dobra?

Po pierwsze, **to już jest częściowo mierzone**. Długość gry koreluje z metrykami, których używamy:

| długość gry a… | korelacja |
|---|---:|
| bitwy polowe | +0,684 |
| wskaźnik odbijania (już znormalizowany na turę) | +0,628 |
| wskaźnik podboju | +0,546 |
| nierównowaga militarna | −0,419 |
| ocena DYNAMIZM | +0,319 |
| ocena BALANS | +0,263 |

Dłuższe mecze rzeczywiście są bardziej dynamiczne i lepiej wyrównane, ale wskaźnik odbijania łapie
z tego blisko 40 % wariancji. Osobne wejście w dużej mierze dublowałoby istniejące.

Po drugie — i to jest powód rozstrzygający — **premiowanie długości otwiera tę samą pułapkę, przed
którą ostrzegają autorzy artykułu przy balansie.** U nich maksymalny balans dawało się osiągnąć
przez całkowitą bezczynność graczy. U nas maksymalną długość dawałaby mapa, na której **nikt nie
jest w stanie wygrać** — rozgrywka dobija do limitu 500 tur i kończy się bez rozstrzygnięcia
(dotyczy to około 17 % pojedynczych meczów). Taki pat ma świetny wskaźnik podboju i świetne
odbijanie, więc żadna z bramek by go nie zatrzymała, a NSGA-II miałby prostą drogę do jego
wyewoluowania. Nagradzanie długości byłoby więc wprowadzeniem nowej podatności w miejsce tej,
którą właśnie usunęliśmy z punktów kulminacyjnych.

Po trzecie, **w artykule tego nie ma**: reguła autorów karze wyłącznie krótkie gry („if T is lo then
bal is lo") i nie nagradza długich. Nasza bramka realizuje dokładnie tę samą asymetrię, tylko progiem
ostrym zamiast rozmytym.

Gdyby kiedyś wprowadzać tę metrykę jako wejście, jedyną bezpieczną postacią jest **zbiór pożądany
ŚREDNI**, a nie WYSOKI — dokładnie tak, jak pierwotnie próbowano z punktami kulminacyjnymi.

#### Efekt uboczny tego pomiaru — wyjaśnienie, dlaczego `minSpawnDistance` wyszedł słaby

To jest osobny, wartościowy wniosek do rozdziału o genotypie.

| gen | korelacja z długością gry |
|---|---:|
| **`minSpawnDistance`** | **+0,652** |
| `populationToCreateNewUnit` | −0,436 |
| `population_max` | +0,364 |

Dystans startowy jest **najsilniej związany właśnie z długością gry** — z żadną inną metryką nie
przekracza +0,39. Innymi słowy: ten gen realnie wpływa na przebieg rozgrywki, tylko wpływa na tę
jedną wielkość, której świadomie nie używamy do oceny. Stąd bierze się jego pozorna słabość
w pilotażu i to, że NSGA-II praktycznie go ignorował — nie dlatego, że nic nie robi, lecz dlatego,
że robi coś, czego funkcja przystosowania nie widzi.

Wniosek do „kierunków dalszych badań": jeśli wymiar przestrzenny ma mieć znaczenie w optymalizacji,
potrzebna jest metryka, która go łapie — ale bez premiowania patów.

#### Wskaźnik podboju — bramka, nie wejście

W artykule ta metryka rozstrzygała, czy gracze w ogóle podjęli ekspansję, zamiast siedzieć w bazach.
Było to realne pytanie, bo tam zajęcie planety kosztowało statki, a w turnieju grały trzy różne boty
o odmiennych strategiach.

U nas zajęcie pola neutralnego jest bezkosztowe, a drzewo priorytetów stawia ekspansję ponad atakiem,
więc bierność jest niewykonalna. Zmierzony rozkład: **65,0–99,7 %** w pilotażu, mediana blisko 95 %.
Jako wejście rozmyte metryka byłaby niemal stałą.

Ale — i to jest ważne dla pracy — **jako bramka zadziałała dokładnie raz i uratowała eksperyment**.
Mapa kontrolna z bazami postawionymi tuż obok siebie miała najniższe surowe nierównowagi z całego
zestawienia (terytorialna 0,9 %, militarna 4,5 %), bo gra kończy się, zanim ktokolwiek zdąży zbudować
przewagę. Wyglądała więc na mapę idealnie zbalansowaną. Odrzucił ją wskaźnik podboju: **6,5 % wobec
progu 60 %**. Gdyby pozostał zwykłym wejściem rozmytym, mapa dostałaby wysoką ocenę i NSGA-II
ewoluowałby w stronę map, na których rozgrywka kończy się po kilkudziesięciu turach.

To jest najlepszy pojedynczy argument za rozdzieleniem „czy wynik wolno oceniać" od „jak dobry jest".

#### Punkty kulminacyjne — metryka diagnostyczna, nie wejście

Pełne omówienie w rozdz. 4.3. W skrócie: wzór (7) z artykułu wdrożono dokładnie, a mimo to metryka
koreluje **+0,930 z nierównowagą terytorialną** i **ujemnie** z tym, co miała mierzyć (−0,304
z odbijaniem, −0,399 z bitwami). Powód: wzór zakłada powroty do gry, a ta gra ich nie ma.

Zostawienie jej jako wejścia dynamizmu oznaczałoby albo regułę w kierunku sprzecznym z pomiarem,
albo — po odwróceniu kierunku — mierzenie balansu po raz drugi i sztuczne zawyżenie wyniku głównego
(rozdz. 4.3.1). Dlatego jest mierzona, raportowana i opisana, ale nie wpływa na ocenę.

#### Czy dwa wejścia dynamizmu to nie za mało

Uczciwe pytanie i trzeba na nie odpowiedzieć w pracy, a nie czekać, aż zada je recenzent.

- **Nie dublują się.** Korelacja wskaźnika odbijania ze zmianami prowadzenia wynosi **+0,616**, czyli
  dzielą około 38 % wariancji. Mierzą pokrewne, ale różne rzeczy: odbijanie to intensywność ruchu
  linii frontu, zmiany prowadzenia to odwrócenia przewagi. Mapa może mieć ruchliwy front bez
  odwrócenia prowadzenia i odwrotnie.
- **Rozdzielczość oceny nie ucierpiała.** Dynamizm na dwóch wejściach daje zakres 0,147–0,865, czyli
  praktycznie pełną szerokość skali — tyle samo co wersja z trzema wejściami (0,156–0,833).
- **Baza reguł pozostaje kompletna**: 2 stany zmian prowadzenia × 3 stany odbijania = 6 kombinacji,
  wszystkie wymienione.
- Dla porównania: w artykule kryterium dynamizmu opisują cztery różne statystyki, ale trzy z nich
  (punkty kulminacyjne dla trzech zasobów) to w istocie jedno pojęcie policzone trzykrotnie, a autorzy
  sami przyznają, że zawiera ono składnik balansu. Realna liczba niezależnych pojęć po ich stronie
  to więc również około dwóch.

#### Jak to ująć jednym akapitem we wnioskach

> Trzy z siedmiu statystyk zaproponowanych w artykule źródłowym nie zostały użyte jako wejścia
> systemu oceny. Nie wynika to z ich wadliwości, lecz z braku informacji, jaką niosą w badanej
> mechanice: wskaźnik podboju i długość gry przyjmują w niej wartości niemal stałe, ponieważ
> ekspansja jest bezkosztowa i wymuszona, a rozgrywka kończy się dopiero zdobyciem bazy; punkty
> kulminacyjne, mimo zaimplementowania zgodnie ze wzorem źródłowym, mierzą głębokość dominacji
> zamiast zwrotów akcji, ponieważ gra nie zawiera mechaniki powrotu. Dwie pierwsze zachowano jako
> warunki dopuszczenia wyniku, trzecią jako metrykę diagnostyczną. Pokazuje to, że **informacyjność
> metryki nie jest jej wewnętrzną własnością, lecz relacją między metryką a regułami gry** —
> ta sama definicja różnicuje mapy w jednej grze i jest stała lub odwrócona w innej.

---

## 5. Wynik główny — balans i dynamizm kooperują

To będzie najczęściej cytowane zdanie z Twojej pracy.

**Zmierzona korelacja ocen: od +0,54 do +0,65**, zależnie od zestawu metryk i wersji systemu.
Sprawdzone na kilku niezależnych konfiguracjach — wynik jest stabilny. Na aktualnej kalibracji
(pilotaż 50 × 60 meczów, wzory (6) i (7) z artykułu) wynosi **+0,651**.

### Najgroźniejszy zarzut wobec tego wyniku — i odpowiedź na niego

Recenzent może powiedzieć: korelacja jest wytworzona przez konstrukcję systemu, bo Peak Differences
jest wejściem dynamizmu, a koreluje z nierównowagą terytorialną na +0,93 — czyli dynamizm po części
mierzy balans. Zarzut jest trafny i trzeba mieć na niego liczbę.

**Odpowiedź: wynik przeżywa całkowite usunięcie tego wejścia.** Dynamizm zbudowany wyłącznie ze
zmian prowadzenia i wskaźnika odbijania — bez jakiegokolwiek składnika mierzącego nierównowagę —
koreluje z balansem na **+0,586** (50 konfiguracji, ta sama kalibracja). Zbieżność celów nie jest
artefaktem systemu rozmytego. Pełne zestawienie trzech wariantów w rozdz. 4.3.1.

Drugi, niezależny dowód: korelacje samych genów (rozdz. 3). `population_max` jednocześnie podnosi
liczbę bitew (+0,87) i obniża nierównowagę militarną (−0,81). Zbieżność widać więc **na poziomie
surowych metryk, zanim jakikolwiek system rozmyty w ogóle zadziała**.

**Co dokładnie mówi artykuł — sprawdzone w oryginale, nie parafrazuj z pamięci.** Autorzy nazywają
te cele „**partially conflicting**" (rozdz. 3) i lokalizują konflikt wyłącznie na górnym końcu skali
balansu:

- optymalizacja samego balansu daje mapy, na których równowaga bierze się z bezczynności: „balance
  was achieved at the expense of complete inaction: both players sit on their home planets and do
  not attempt to conquer other planets, let alone engage in combat with the opponent";
- na dolnym końcu skali cele idą w tę samą stronę, dokładnie jak u nas: „a very unbalanced game is
  likely going to be short or feature less alternation between the players, hence resulting to be
  non-dynamic as well";
- stąd kształt frontu: „a gentle degradation of dynamism as the balance is increased, followed by
  a sharp decrease of the former upon reaching the high end of balance";
- i ich własny wniosek: „medium-high dynamism is compatible with medium balance".

Nie wolno więc pisać „w artykule balans i dynamizm były sprzeczne". Poprawnie: **w artykule są
częściowo sprzeczne, a sprzeczność ma jedną konkretną przyczynę — możliwość osiągnięcia idealnego
balansu przez bezczynność graczy.**

### Dlaczego u nas nie ma nawet tej częściowej sprzeczności — mechanizm do opisania

1. Ekspansja jest darmowa i wymuszona przez drzewo priorytetów, więc **bezczynność nie jest
   możliwa**. Nie da się osiągnąć balansu przez nicnierobienie.
2. **W turnieju nie ma pasywnego zawodnika.** To jest drugi, niezależny powód i łatwo go przeoczyć.
   U autorów mapy oceniały **trzy różne boty** z Google AI Challenge 2010 (Manwe, bot Flagscappera
   i bot fglidera), każdy z inną strategią, i to właśnie mecz między konkretną parą botów mógł
   skończyć się obopólną biernością. W tej pracy grają **dwa identyczne boty**, więc przestrzeń
   strategii ma jeden punkt i nie zawiera zachowania pasywnego. Bezczynność jest u nas niemożliwa
   nie tylko dlatego, że drzewo priorytetów jej zabrania, ale też dlatego, że nie ma agenta,
   który mógłby ją wybrać.
   Uczciwe zastrzeżenie: identyczne boty ułatwiają interpretację, bo różnice w metrykach pochodzą
   wyłącznie z mapy, a nie z różnicy umiejętności agentów. Ceną jest zawężenie wniosku — dotyczy on
   map ocenianych przez agentów jednorodnych. Autorzy sami piszą, że „the particular bots used in
   the experimentation exert an influence on these results", więc to zastrzeżenie dotyczy obu prac,
   tyle że u nas w innym kierunku.
3. Przewaga terytorialna napędza gospodarkę ze współczynnikiem 1,14, a gospodarka napędza armię.
   **Nie ma mechaniki powrotu do gry.**
4. Wniosek: mapa niezbalansowana rozstrzyga się szybko i jednostronnie, czyli jest nudna.
   Mapa zbalansowana daje długą, wyrównaną wojnę, czyli jest dynamiczna.

### TEZA DO WNIOSKÓW — przepisać do rozdziału 9

To jest gotowe sformułowanie wyniku głównego. Nie „artykuł się mylił", lecz wskazanie **warunku, od
którego zależy istnienie kompromisu**. Wersja do rozwinięcia w rozdziale z wnioskami:

> Autorzy artykułu źródłowego określają balans i dynamizm jako cele *częściowo* sprzeczne
> i sami zauważają, że na dolnym końcu skali obie własności idą w tę samą stronę: gra skrajnie
> niezbalansowana kończy się szybko i zawiera mało zmian prowadzenia, więc jest zarazem
> niedynamiczna. Sprzeczność pojawia się u nich wyłącznie na górnym końcu skali balansu i ma jedną
> konkretną przyczynę: idealną równowagę można tam osiągnąć przez całkowitą bezczynność graczy,
> którzy pozostają w bazach macierzystych.
>
> W badanej grze ta przyczyna nie występuje. Zajęcie pola neutralnego jest bezkosztowe, a drzewo
> priorytetów bota stawia ekspansję ponad atakiem, więc strategia pasywna jest niemożliwa do
> zrealizowania — balansu nie da się uzyskać przez nicnierobienie. Jednocześnie przewaga
> terytorialna napędza gospodarkę ze współczynnikiem 1,14, a gra nie zawiera żadnej mechaniki
> wyrównywania szans, więc każda przewaga narasta. Mapa niezbalansowana rozstrzyga się szybko
> i jednostronnie, czyli jest nudna; mapa zbalansowana daje długą, wyrównaną wojnę, czyli jest
> dynamiczna. Zmierzona korelacja obu ocen wynosi od +0,54 do +0,60 i jest stabilna na trzech
> niezależnych zestawach metryk.
>
> Wniosek ogólny: **relacja między balansem a dynamizmem nie jest własnością metody oceny, lecz
> mechaniki gry.** Kompromis między tymi celami istnieje tylko wtedy, gdy zasady gry dopuszczają
> osiągnięcie równowagi przez bezczynność. W grach z wymuszoną ekspansją, silnym efektem kuli
> śnieżnej i bez mechanizmu powrotu do gry oba cele są zbieżne, wielokryterialność traci
> uzasadnienie, a front Pareto zapada się do pojedynczego punktu.

Tak postawiona teza nie jest kontrą do artykułu, tylko jego **uogólnieniem**: podaje warunek
brzegowy, przy którym wynik autorów zachodzi, i pokazuje grę, w której ten warunek jest niespełniony.
Tego w literaturze nie ma, a stoi za tym 428 ocenionych konfiguracji i blisko trzydzieści tysięcy
rozegranych meczów.

**Kontrargument, na który trzeba odpowiedzieć w tym samym rozdziale** — patrz rozdz. 11.5.

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
| wydłużenie rozgrywki | mecze trwają 300–450 tur przy limicie 500, jedna tura to 0,2 % partii |
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

Pięć trybów generatora, po 60 meczów każdy (`test_mapy_kontrolne.py` używa tej samej stałej
`MECZOW_NA_CHROMOSOM` co eksperyment główny):

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

Liczby przeliczone z `mapy_kontrolne_wyniki.json`. **Wcześniejsza wersja tej notatki podawała
w tej tabeli wartości z jakiegoś starszego przebiegu i nie zgadzały się one z zapisanymi danymi
ani co do jednej pozycji** — oceny rozmyte zgadzały się, surowe metryki nie. Poniżej wersja
odtworzona z pliku.

| tryb mapy | teryt % | growth % | mil % | reconq % | conq % | BALANS | DYNAMIZM |
|---|---:|---:|---:|---:|---:|---:|---:|
| symetria obrotowa 180° | **13,3** | **16,2** | **19,0** | **114,8** | 98,7 | **0,590** | **0,759** |
| generator normalny | 17,2 | 23,8 | 25,0 | 63,9 | 92,8 | 0,174 | 0,561 |
| bogata strefa przy bazie 1 | 20,5 | **55,6** | **43,7** | 10,7 | 75,4 | 0,141 | 0,154 |
| baza 2 zepchnięta w róg | **26,9** | 37,1 | 33,8 | 33,3 | 96,0 | 0,133 | 0,157 |
| bazy tuż obok siebie | 0,9 | 28,8 | 4,5 | 17,4 | **6,5** | **0,000** | **0,000** |

Każde zaburzenie wykryte przez tę metrykę, która miała je wykryć:

- **bogata strefa** podniosła Growth Imbalance z 23,8 % do **55,6 %**, czyli 2,3-krotnie — to
  bezpośrednie potwierdzenie zasadności przedefiniowania tej metryki (rozdz. 4.1)
- **baza w rogu** dała najwyższą nierównowagę terytorialną (26,9 % wobec 17,2 % na mapie losowej)
- **symetria** dała najniższe wszystkie trzy nierównowagi i **1,8 raza wyższy** wskaźnik odbijania
  niż mapa losowa — idealnie wyrównane siły powodują nieustanne falowanie frontu

**Zastrzeżenie po zmianie definicji metryk.** Kolumna `reconq %` pochodzi sprzed wdrożenia wzoru (6)
z artykułu — jest to łączna liczba przejęć na 100 pól, bez dzielenia przez liczbę tur. Po
rekalibracji trzeba ten test powtórzyć (`python test_mapy_kontrolne.py`, 5 trybów × 60 meczów,
ok. 15 minut) i podmienić kolumny `reconq %` oraz `DYNAMIZM`. Kolumny `teryt`, `growth`, `mil`
i `BALANS` zmiana metryk nie dotyczy — te liczby zostają.

### Najciekawszy przypadek: bazy obok siebie

Ten tryb miał **najniższe** surowe nierównowagi z całego zestawienia: terytorialna 0,9 %,
militarna 4,5 %. Wyglądał więc na mapę idealnie zbalansowaną — bo gra kończy się, zanim
ktokolwiek zdąży zbudować przewagę.

Odrzuciła go dopiero **bramka poprawności**: wskaźnik podboju 6,5 % wobec progu 60 %.

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

**99,4 % wyniku osiągnięto w pierwszych 20 % czasu** (pokolenie 5 z 25). Od pokolenia 16 krzywa
jest całkowicie płaska. Wniosek do opisania: dla tej przestrzeni parametrów 15 pokoleń w zupełności
wystarcza.

**Uwaga o danych do wykresu.** `nsga2_postep.json` jest plikiem kontrolnym nadpisywanym po każdym
pokoleniu, więc zawiera **wyłącznie ostatnią wartość** hiperobjętości (0,7051), a nie całą krzywą.
Serię 25 punktów odtwarza się z `nsga2_front.json`, klucz `historia` — 428 wpisów z numerem
pokolenia i parą ocen. Liczenie: dla każdego pokolenia `g` bierzesz wszystkie chromosomy ocenione
do pokolenia `g` włącznie i liczysz hiperobjętość względem punktu odniesienia (0, 0), z celami
zapisanymi jako wartości ujemne. Odtworzenie daje dokładnie liczby podane wyżej — 0,6912 / 0,7011 /
0,7051 — więc wykres jest w pełni odtwarzalny. Jest to krzywa **skumulowana** (najlepszy front
znaleziony do danego pokolenia) i tak trzeba ją podpisać; wartości drukowane w konsoli podczas
przebiegu dotyczyły bieżącej populacji i nie zostały zapisane.

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

### Dwa geny na krawędzi zakresu — zarzut zamknięty pomiarem

Uwaga metodologiczna, o którą recenzent zapyta na pewno.

- `population_max` = 99 w **138 z 428** chromosomów, przy górnej granicy 100
- `populationToCreateNewUnit` ≤ 450 w **234 z 428**, przy dolnej granicy 400

Optimum leżało **na krawędzi dozwolonej przestrzeni**, a nie w jej wnętrzu, więc trzeba było
sprawdzić, czy zadeklarowany zakres nie obciął lepszych rozwiązań. Wykonano celowany przemiat poza
granice — bez ponownego uruchamiania NSGA-II — dwa jednowymiarowe przekroje po siedem konfiguracji,
każda oceniona w 60 meczach (razem 840 meczów, 17 minut). Skrypt `test_granic_genow.py`, dane
`granice_genow_wyniki.json`.

**Przemiat `population_max`** (zadeklarowana granica: 100; pozostałe geny w optimum):

| wartość | BALANS | DYNAMIZM | teryt % | growth % | mil % | reconq % | peaks % |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 90 | 0,8238 | 0,8265 | 11,0 | 14,4 | 12,6 | 139,4 | 44,4 |
| 100 | 0,8365 | 0,8367 | 8,8 | 12,3 | 9,1 | 128,2 | 37,0 |
| 120 | 0,8358 | 0,8380 | 8,9 | 11,9 | 9,2 | 131,8 | 36,0 |
| 140 | 0,8377 | 0,8393 | 8,5 | 12,0 | 8,6 | 136,7 | 35,0 |
| 160 | 0,8410 | 0,8412 | 8,0 | 11,1 | 8,3 | 135,2 | 33,5 |
| 180 | 0,8366 | 0,8403 | 8,5 | 12,3 | 8,1 | 122,3 | 34,2 |
| 200 | 0,8385 | 0,8387 | 8,4 | 11,6 | 8,4 | 145,3 | 35,5 |

**Przemiat `populationToCreateNewUnit`** (zadeklarowana granica: 400; pozostałe geny w optimum):

| wartość | BALANS | DYNAMIZM | teryt % | growth % | mil % | reconq % | conq % | długość % |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 150 | 0,8374 | 0,8337 | 8,6 | 11,7 | 9,0 | 126,1 | 99,3 | 74,0 |
| 250 | 0,8385 | 0,8286 | 8,4 | 11,3 | 9,8 | 142,2 | 99,6 | 81,4 |
| 350 | 0,8278 | 0,8273 | 10,3 | 14,1 | 10,8 | 126,8 | 98,3 | 73,3 |
| 420 | 0,8303 | 0,8312 | 9,9 | 13,5 | 10,5 | 131,4 | 98,8 | 76,3 |
| 500 | 0,8290 | 0,8262 | 9,9 | 14,2 | 10,7 | 123,9 | 97,1 | 72,3 |
| 650 | 0,8237 | 0,8292 | 10,7 | 15,4 | 11,1 | 104,6 | 95,5 | 68,7 |
| 800 | 0,8234 | **0,5345** | 9,6 | 15,5 | 11,5 | **70,1** | 88,6 | 55,1 |

**Wynik 1 — zakres genotypu obejmował optimum.** Najlepsza konfiguracja poza zakresem przewyższa
najlepszą w zakresie o 0,0045 (`population_max`) i o 0,0081 (`populationToCreateNewUnit`), przy
progu istotności równym dwóm odchyleniom szumu, czyli 0,0136 dla balansu i 0,0276 dla dynamizmu.
Żadna z różnic nie przekracza progu. Poszerzenie zakresu genów nie poprawiłoby wyniku.

**Wynik 2 — optimum jest płaskowyżem, a nie szczytem.** Trzynaście z czternastu konfiguracji mieści
się w przedziale 0,82–0,84, czyli w granicach szumu. Cały rejon `population_max` ≥ 100
i `populationToCreateNewUnit` ≤ 500 daje wyniki nierozróżnialne. To ważne dla wniosków projektowych:
zalecenie jest **odporne**, a nie wyostrzone — projektant nie musi trafić w punkt, wystarczy, że
wejdzie w ten obszar. Wyjaśnia to również, dlaczego NSGA-II przestał się poprawiać po 16 pokoleniach.

Skok następuje **poniżej** dolnej krawędzi płaskowyżu: przy `population_max` = 90 wszystkie surowe
metryki są wyraźnie gorsze (nierównowaga terytorialna 11,0 % wobec 8,0–8,9 % powyżej setki).
Zadeklarowana granica 100 wypadła więc dokładnie na początku płaskowyżu — algorytm napierał na nią
nie dlatego, że optimum leżało dalej, lecz dlatego, że płaskowyż zaczyna się właśnie tam.

**Wynik 3 — i to jest najmocniejszy argument — dalej nie ma dokąd rosnąć.** Sufit matematyczny
wyjścia systemu rozmytego wynosi **0,8667** dla balansu i **0,8605** dla dynamizmu; tyle daje
defuzyfikacja środkiem ciężkości przy regule WYSOKI odpalonej z pełną siłą. Rejon optimum osiąga
0,8365, czyli **96,5 % sufitu**. Mapa hipotetycznie idealna — o wszystkich trzech nierównowagach
równych zero — dostałaby zaledwie o **0,0302** więcej, czyli około czterech odchyleń szumu. Ten
argument zamyka temat lepiej niż sam przemiat, bo nie zależy od liczby zmierzonych punktów.

**Wynik 4 — funkcja przystosowania nadal reaguje, gdy powinna.** Konfiguracja
`populationToCreateNewUnit` = 800 daje załamanie dynamizmu z 0,83 do 0,5345. Nie jest to zadziałanie
bramki poprawności: wskaźnik podboju wynosi 88,6 % (próg 60 %), a długość gry 55,1 % (próg 15 %).
Przyczyna leży w bazie reguł — wskaźnik odbijania spada ze 130 % do 70,1 %, czyli ze zbioru WYSOKI
do ŚREDNI, przez co aktywuje się reguła o werdykcie ŚREDNI zamiast WYSOKI. Mechanizm jest czytelny:
drogie jednostki oznaczają mniej oddziałów na mapie, mniej kontaktu i nieruchomy front. To dobry,
jednoakapitowy dowód, że płaskowyż nie wynika z nieczułości metryki, tylko z rzeczywistego braku
różnic w badanym rejonie.

### Ograniczenie ujawnione przy okazji: jedno wejście dynamizmu przestaje działać w optimum

Rzecz do opisania uczciwie, bo wychodzi wprost z danych. W całym rejonie optimum uśrednione
Peak Differences przyjmują wartości 33–45 %, podczas gdy zbiór ŚREDNI — czyli wartość pożądana
(rozdz. 4.3) — obejmuje przedział 52,2–68,3 %. Metryka nigdy nie trafia w zbiór, dla którego została
zaprojektowana; jej przynależność do zbioru NISKI działa jako **ogranicznik** oceny dynamizmu, a nie
jako sygnał dramaturgii. Gdyby przy pozostałych wejściach z optimum piki trafiły w zbiór ŚREDNI,
ocena dynamizmu wzrosłaby z 0,8367 do 0,8483.

Podobnie zmiany prowadzenia w rejonie optimum wynoszą 3,8–5,2 na 100 tur, przy maksimum z pilotażu
równym 4,25 — zbiór WYSOKI jest tam w pełni nasycony i również przestaje różnicować.

Wniosek do rozdziału o ograniczeniach: **optimum znalezione przez NSGA-II leży poza zakresem, na
którym kalibrowano system rozmyty.** Nie unieważnia to wyniku, bo uporządkowanie ocen pozostaje
poprawne, ale oznacza, że w samym optimum system rozróżnia słabiej niż w środku rozkładu
pilotażowego. Naturalnym domknięciem byłaby powtórna kalibracja progów na rozkładzie z okolic
optimum — osobny eksperyment i dobry punkt do „dalszych badań".

---

## 9. Liczby, które warto mieć pod ręką

| Wielkość | Wartość |
|---|---|
| Rozegranych meczów w pilotażach | 2000 stare (2 × 50 × 20) + **3000 nowe** (50 × 60) |
| Meczów na ocenę jednej konfiguracji | **60** — tyle samo w pilotażu i w eksperymencie głównym |
| Aktualna kalibracja progów | `pilotaz_wyniki.json`, 50 konfiguracji LHS × 60 meczów |
| Wielkość mapy | 20 × 20, dokładnie 360 pól lądu i 40 wody |
| Rozkład populacji | 5 progów po 20% pól, suma na mapie stała |
| Rozpiętość ocen przed kalibracją | balans 0,19 · dynamizm 0,02 |
| Rozpiętość ocen po kalibracji | balans 0,134–0,834 · dynamizm 0,156–0,833 |
| Korelacja balans ↔ dynamizm | **+0,651** na aktualnej kalibracji (+0,54 do +0,65 historycznie) |
| Ta sama korelacja bez wejścia Peak Differences | **+0,586** — wynik nie jest artefaktem systemu |
| Korelacja Peak Differences z nierównowagą terytorialną | +0,930 wzorem (7); +0,893 starym wzorem |
| Korelacja Peak Differences z odbijaniem / bitwami | −0,304 / −0,399 — kierunek odwrotny niż w artykule |
| Współczynnik kuli śnieżnej (growth vs territorial) | 1,17 przy korelacji +0,94 |
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
| Przemiat poza granice genów | 14 konfiguracji × 60 meczów = 840 meczów, 17 min |
| Zysk poza zakresem `population_max` | +0,0045 przy progu istotności 0,0136 — brak |
| Zysk poza zakresem `populationToCreateNewUnit` | +0,0081 przy progu istotności 0,0136 — brak |
| Sufit oceny rozmytej (balans / dynamizm) | 0,8667 / 0,8605 |
| Ocena w optimum wobec sufitu | 0,8365 = 96,5 % sufitu; do ideału brakuje 0,0302 |
| Reconquering wg wzoru (6) z artykułu | 0,0006–0,0036 na turę, przy progu WYSOKI = 0,1 |
| Reguł w bazie balansu | 27 (komplet 3³) |
| Reguł w bazie dynamizmu | 18 (2 × 3 × 3); po wdrożeniu wariantu C — 6 (2 × 3) |
| Błąd średniej przy 20 meczach | terytorium ±2,0 · gospodarka ±2,5 · odbijanie ±9,7 |

**Progi funkcji przynależności — wersja aktualna** (kwantyl 25 % / mediana / kwantyl 75 % / maksimum,
wszystkie z `pilotaz_wyniki.json`):

| zmienna | q25 | mediana | q75 | maksimum |
|---|---:|---:|---:|---:|
| Territorial imbalance [%] | 12,14 | 14,25 | 17,05 | 23,33 |
| Growth imbalance [%] | 17,39 | 19,45 | 22,95 | 32,50 |
| Military imbalance [%] | 16,21 | 19,48 | 25,10 | 31,05 |
| Reconquering rate [% pól / 100 tur] | 12,34 | 18,39 | 27,39 | 34,30 |
| Zmiany prowadzenia [na 100 tur] | 2,10 | 2,62 | 3,01 | 4,91 |
| Peak differences [%, amplituda 0–200] | 72,27 | 81,13 | 87,22 | 101,30 |

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

- **Sprawdź, czy metryka nie mierzy przy okazji długości meczu.** Zmiany prowadzenia zostały
  znormalizowane na 100 tur, ale Reconquering Rate nie — szczegóły i test w rozdz. 11.3, punkt 1.

- **Opisz sufit i ściśnięcie skali ocen.** Wyjście systemu rozmytego nie sięga 1,0 — sufit to
  0,8667, bo defuzyfikacja środkiem ciężkości uśrednia po trójkącie WYSOKI. Prawie cała zdolność
  rozróżniania mieści się w wąskiej strefie przejścia: przy nierównowadze terytorialnej 14 % ocena
  wynosi 0,52, a przy 11 % już 0,82. Powyżej i poniżej tej strefy skala jest niemal płaska. Bez
  podania tej charakterystyki czytelnik odczyta różnicę 0,84 wobec 0,53 jako „o połowę lepszą mapę".

- **Rozdziel dwa źródła szumu.** Pierwsze to losowość samej symulacji przy ustalonej mapie
  (12–53 % zmienności, rozdz. 6.4). Drugie to losowanie planszy z rodziny opisanej chromosomem —
  konsekwencja tego, że genem jest przepis, a nie mapa (rozdz. 3). Drugie źródło nie występuje
  w artykule i jest specyficzne dla przyjętego genotypu.

- **Zachowaj surowe dane.** `pilotaz_wyniki.json` i katalog `Wyniki_Batch` to materiał dowodowy pod
  każdą liczbę w pracy. Warto je zarchiwizować razem z wersją kodu, która je wygenerowała.


---

## 11. Pełna lektura artykułu źródłowego — co z niej wynika

Przeczytany w całości: **Lara-Cabrera R., Cotta C., Fernández-Leiva A.J., *On Balance and Dynamism in
Procedural Content Generation with Self-Adaptive Evolutionary Algorithms*, Natural Computing, 2014.**

To jest właściwe źródło opisu eksperymentu. Tekst z IJIMAI 2015 (*Procedural Content Generation for
Real-Time Strategy Games*) jest artykułem przeglądowym i powtarza te same wnioski skrótowo.
W bibliografii warto podać oba, ale **metodę opisuj z Natural Computing i stamtąd bierz cytaty**.

### 11.1. Cytaty warte umieszczenia w pracy

| gdzie | cytat | do czego |
|---|---|---|
| rozdz. 3 | „In fact we discovered that these two properties are partially conflicting, thus suggesting the need for a multiobjective approach." | jedyne miejsce, w którym autorzy nazywają relację celów — słowo *partially* jest tu istotne |
| rozdz. 4.1 | „balance was achieved at the expense of complete inaction: both players sit on their home planets and do not attempt to conquer other planets" | mechanizm konfliktu na górnym końcu skali |
| rozdz. 4.1 | „the inability of balance —as a stand-alone property— to characterize interesting games" | uzasadnienie podejścia dwukryterialnego |
| rozdz. 5 | „a very imbalanced game ends very early and/or is likely to exhibit less comebacks from one of the players" | zbieżność celów na dolnym końcu — u nas dokładnie to samo |
| rozdz. 5 | „medium-high dynamism is compatible with medium balance" | ich własny wniosek o zgodności celów |
| rozdz. 5 | „the approach presented here can be easily generalized to other RTS scenarios since these often feature bases and resources to be conquered" | **autorzy sami deklarują przenośność metody, a ta praca jest jej pierwszym testem — cytat do wstępu i do wniosków** |
| rozdz. 5 | „further study will be dedicated to the actual characterization of balance and dynamic, and how adjusting these, influences the optimization process" | ta praca realizuje kierunek dalszych badań wskazany przez autorów |
| rozdz. 5 | „a point of caution is that the particular bots used in the experimentation exert an influence on these results" | to samo zastrzeżenie dotyczy nas — do rozdziału z ograniczeniami |

Numery stron trzeba dopisać z własnego egzemplarza PDF.

### 11.2. Czym ich eksperyment różni się od naszego

| aspekt | artykuł | ta praca |
|---|---|---|
| **przedmiot ewolucji** | chromosomem jest **sama mapa**: lista 15–30 planet, każda z współrzędnymi, rozmiarem i liczbą statków; długość chromosomu zmienna, liczba planet też podlega self-adaptacji | chromosomem są **trzy parametry generatora**; mapa powstaje losowo z tych parametrów |
| agenci | trzy **różne** boty z Google AI Challenge 2010 (Manwe, Flagscapper, fglider), wszystkie z pierwszej setki rankingu | dwa **identyczne** boty na drzewie priorytetów |
| algorytm | samoadaptacyjny EA (mutacja gaussowska i całkowitoliczbowa, operator cut-and-splice), populacja μ=10, λ=100 | NSGA-II z pymoo, populacja 20, 25 pokoleń |
| budżet | 10 000 ewaluacji na przebieg, 10 przebiegów | 428 ewaluacji, po 60 meczów każda |
| funkcje przynależności | ogólne, **nieskalibrowane**, rozpięte na teoretycznym zakresie zmiennej (rys. 2 w artykule) | kalibrowane na kwantylach rozkładów z pilotażu |
| bazy reguł | 3 reguły balansu, 7 reguł dynamizmu, **świadomie niekompletne** | 27 i 18 reguł, komplet |
| wejścia balansu | nierównowaga terytorialna, gospodarcza, militarna | te same trzy |
| wejścia dynamizmu | wskaźnik podboju, wskaźnik odbijania, długość gry oraz trzy punkty kulminacyjne | zmiany prowadzenia i wskaźnik odbijania; podbój i długość gry przeniesione do bramek, punkty kulminacyjne do metryk diagnostycznych |
| wyostrzanie | t-norma min, t-konorma max, modyfikator *very* jako x², środek ciężkości | skfuzzy: min/max, centroid |
| limit meczu | τmax = 400 tur | 500 tur |

Dwie rzeczy z tej tabeli warto wykorzystać wprost w pracy:

**Kalibracja jest naszym wkładem — potwierdzone.** Rysunek 2 w artykule pokazuje funkcje
przynależności rozpięte na teoretycznym zakresie zmiennych, bez związku z rozkładem empirycznym.
Zdanie z rozdz. 2 tej notatki („rozdział, którego w artykule w ogóle nie ma") jest więc prawdziwe
i można je bezpiecznie postawić w pracy.

**Niekompletność ich baz reguł nie jest błędem.** Autorzy piszą wprost: „we do not have to
exhaustively cover all possible combinations of input variables (…) this does not cause a problem
because the aforementioned rules can still be activated to some degree in this situation (there are
two input fuzzy sets overlapping the whole input domain)". Ich zbiory NISKI i WYSOKI pokrywają całą
dziedzinę, więc zawsze coś się aktywuje. U nas po kalibracji zbiory są węższe i przy trzech stanach
lingwistycznych pojawiły się dziury, stąd konieczność uzupełnienia baz do 27 i 18 reguł. **Opisz to
jako konsekwencję kalibracji, a nie jako poprawianie błędu autorów** — inaczej recenzent znający
artykuł wychwyci nadinterpretację.

### 11.3. Rozbieżności do poprawienia

**1. Reconquering Rate nie jest znormalizowany przez liczbę tur — POWAŻNE.**

Artykuł, wzór (6): `Z_i = (1/τ_i) · Σ_j ζ_ij / n_p`, czyli **średnia na turę**. Nasza wersja
(GDD 7.5.B): liczba przejęć podzielona przez liczbę pól lądowych, bez dzielenia przez liczbę tur.
Nasza metryka rośnie więc wprost proporcjonalnie do długości meczu.

Dlaczego to jest poważne: mecze trwają 250–450 tur, a odchylenie standardowe długości wynosi 84,6
tury. Mapa zbalansowana daje dłuższy mecz, dłuższy mecz daje więcej przejęć, więcej przejęć daje
wyższą ocenę dynamizmu. **Część korelacji balans–dynamizm (+0,54 do +0,60) może pochodzić z tego
artefaktu, a nie z mechaniki gry.** To jest pierwsza rzecz, o którą zapyta recenzent, który przeczyta
wzór (6).

**TEST ZOSTAŁ WYKONANY NA ISTNIEJĄCYCH DANYCH — wynik główny jest odporny.** Nie trzeba było
uruchamiać Unity, bo długość meczu jest zapisana razem z metrykami w `pilotaz_wyniki.json`
(50 konfiguracji). Policzono:

| sprawdzenie | wynik |
|---|---|
| korelacja Reconquering Rate z liczbą tur | **+0,72** — artefakt istnieje, metryka rzeczywiście mierzy też długość meczu |
| korelacja Reconquering Rate **na 100 tur** z liczbą tur | +0,54 — normalizacja tłumi go tylko częściowo, bo dłuższe mecze są też realnie bardziej falujące |
| korelacja ocen BALANS ↔ DYNAMIZM, wersja obecna | **+0,63** |
| korelacja ocen BALANS ↔ DYNAMIZM, reconquering na 100 tur i progi przekalibrowane na nowym rozkładzie | **+0,70** |
| korelacja dynamizmu starego z nowym | +0,98 |

Nowe progi po normalizacji, gdyby wersja z artykułu miała trafić do pracy: q25 = 14,6 · mediana =
20,2 · q75 = 27,4 · maksimum = 35,5 przejęcia na 100 tur.

**Wniosek: normalizacja nie osłabia wyniku głównego, tylko go wzmacnia.** Zbieżność balansu
i dynamizmu nie bierze się z tego, że dłuższe mecze mają więcej przejęć. To jest bardzo dobry
materiał na akapit w rozdziale z wynikami, bo uprzedza zarzut, zanim recenzent zdąży go postawić.

**Test przeliczony ponownie, tym razem funkcją `pipeline_fuzzy.score()` — wszystkie liczby się
potwierdziły** (odtworzenie było niezależną reimplementacją wnioskowania Mamdaniego, więc wymagało
kontroli):

| wielkość | reimplementacja | `score()` z projektu |
|---|---:|---:|
| korelacja reconquering × liczba tur | +0,72 | **+0,7227** |
| korelacja reconquering/100 tur × liczba tur | +0,54 | **+0,5373** |
| korelacja BALANS × DYNAMIZM, wersja obecna | +0,63 | **+0,6306** |
| korelacja BALANS × DYNAMIZM, po normalizacji | +0,70 | **+0,7103** |
| korelacja dynamizmu starego z nowym | +0,98 | **+0,9771** |
| nowe progi (q25 / med / q75 / max) | 14,6 / 20,2 / 27,4 / 35,5 | **14,63 / 20,21 / 27,41 / 35,45** |

Zakres ocen z `score()`: balans 0,1345–0,8393, dynamizm 0,1533–0,8365, zero odrzuceń przez bramki.
Liczby są gotowe do wpisania do pracy. Skrypt kontrolny leży poza repozytorium — wystarczy funkcja
czytająca `pilotaz_wyniki.json`, dopisująca `leadChangeRate = leadChanges / liczba_tur × 100`
i wołająca `pf.score()`.

Jedyne zastrzeżenie, które zostaje: test obejmuje 50 konfiguracji z **jednego** przebiegu
pilotażowego, czyli te same dane, na których kalibrowano progi. Warto powtórzyć go na drugim
przebiegu, jeśli jego surowy plik jeszcze istnieje.

Niezależnie od tego, jaką wersję metryki zostawisz w kodzie, **w pracy trzeba napisać, że nasz
Reconquering Rate nie jest znormalizowany przez liczbę tur, inaczej niż wzór (6) w artykule**,
i podać ten test jako dowód, że nie wpływa to na wnioski.

**2. Peak Differences liczone innym wzorem niż w artykule — POWAŻNE.** Pełny opis w rozdz. 4.3.
W skrócie: artykuł mierzy amplitudę wahnięcia (maksimum minus minimum różnicy ze znakiem, zakres
0–2), my mierzymy maksimum wartości bezwzględnej (zakres 0–1). Wniosek „u nas wysoki pik oznacza
dominację" może być skutkiem zmiany wzoru, a nie własnością gry.

**3. Growth Imbalance — przedefiniowanie było powrotem do artykułu, nie odejściem od niego.**
Opis w rozdz. 4.1.

**4. „W artykule balans i dynamizm były sprzeczne" — sformułowanie nieprawdziwe.** Poprawna wersja
w rozdz. 5. Dotyczy to również **GDD, rozdz. 7.3**, gdzie stoi zdanie „ponieważ maksymalny balans
(gdzie nikt nikogo nie może pokonać z powodu symetrii) naturalnie kłóci się z maksymalnym
dynamizmem". Jest ono sprzeczne zarówno z artykułem, jak i z naszym własnym wynikiem — do poprawienia
przy najbliższej aktualizacji GDD.

**5. Nie porównuj bezwzględnych wartości ocen z artykułem.** Autorzy raportują balans równy 1,0
jako medianę, podczas gdy nasz sufit wyjścia systemu wynosi 0,8667. Oznacza to, że mają inne zbiory
wyjściowe albo inną metodę wyostrzania. Porównywać można **uporządkowanie i relacje między ocenami**,
nigdy same liczby. Warto poświęcić temu jedno zdanie w rozdziale o systemie rozmytym.

**6. Military Imbalance zawiera garnizon bazy — i to jest ZGODNE z artykułem.** Wcześniejsza
wersja tej notatki twierdziła, że nierównowaga militarna z artykułu liczy wyłącznie siły polowe,
a doliczanie garnizonu jest
odstępstwem. To nieprawda i trzeba to odwrócić, zanim trafi do pracy. W Planet Wars **wszystkie
statki stoją na planetach albo lecą we flotach**, a definicja mówi o „the percentage of the total
number of ships (…) owned by player a" — statki stacjonujące na planecie macierzystej są więc
wliczane. Nasza metryka, sumująca armie tokenów i garnizon bazy, jest odpowiednikiem jeden do
jednego.

Zostaje natomiast obserwacja praktyczna, warta jednego zdania w ograniczeniach: nasz garnizon ma
sztywny sufit 700 i przez większość meczu stoi na nim, więc wnosi do metryki składnik prawie stały,
tłumiący wskazania. W Planet Wars liczba statków na planecie macierzystej rośnie bez ograniczeń,
więc tam takiego tłumienia nie ma. To różnica mechaniki, nie definicji metryki.

**7. Zmiany prowadzenia to nasza metryka, nie ich.** W artykule nie ma takiej zmiennej. Jest za to
definicja dynamizmu mówiąca o graczu, który „is at a disadvantage at a certain point can regain
their position", więc metryka jest wierną operacjonalizacją **ich definicji**, ale nie jednej z ich
siedmiu zmiennych. Zapis w GDD 7.5.C („odpowiada wprost definicji dynamizmu przyjętej w artykule")
jest poprawny, byle nie sugerować, że metryka pochodzi z artykułu.

### 11.4. Wynik, który jednak się przeniósł — warto to podkreślić

Nie wszystko wyszło rozbieżnie i uczciwy rozdział z dyskusją powinien to pokazać.

Wniosek autorów: „dynamic games seem to be related to maps featuring a larger number of planets,
widely scattered on the map (…) these features promote dynamism by providing ample stimuli to the
players to expand their empires, eventually clashing with each other".

Nasze optimum: `population_max` ≈ 99, `populationToCreateNewUnit` ≈ 420, czyli **bogaty świat i tanie
jednostki** — więcej zasobów wartych zajęcia i więcej oddziałów na mapie.

To jest ta sama zależność wyrażona w kategoriach innej mechaniki: **im więcej obiektów wartych
rywalizacji, tym więcej dzieje się w grze.** Warto napisać wprost, że ten akurat wniosek przeniósł
się bez zmian, mimo że metryki się nie przeniosły. Wzmacnia to wiarygodność całej pracy, bo pokazuje,
że rozbieżności nie biorą się z wadliwego odtworzenia metody.

### 11.5. Kontrargument, na który trzeba odpowiedzieć we wnioskach

Recenzent znający artykuł zauważy, że **ich chromosomem jest cała mapa, a naszym trzy liczby**.
Powie więc: front Pareto jest wąski nie dlatego, że cele kooperują, tylko dlatego, że
trzyparametrowa przestrzeń przeszukiwania jest za uboga, by wyrazić jakikolwiek kompromis. To jest
mocny zarzut i trzeba na niego odpowiedzieć wprost, a nie go przemilczeć.

Trzy argumenty, którymi dysponujemy, w kolejności od najmocniejszego:

1. **Korelacja +0,54 do +0,60 została zmierzona na 50 losowych konfiguracjach pokrywających całą
   przestrzeń genotypu, a nie na froncie.** Jest to własność danych, całkowicie niezależna od tego,
   jak działa NSGA-II i jak szeroki jest front. Nawet gdyby algorytm nie znalazł nic sensownego,
   ta korelacja pozostaje faktem.
2. **Szerokość frontu jest mniejsza od szumu pomiarowego** (0,0072 wobec odchylenia 0,0068 na 102
   chromosomach z tego samego rejonu). Front nie jest więc wąski — on w ogóle nie istnieje jako
   zbiór rozróżnialnych kompromisów.
3. **Mechanizm jest opisany i niezależny od parametryzacji**: wymuszona ekspansja, brak strategii
   pasywnej, współczynnik kuli śnieżnej 1,14, brak mechaniki powrotu do gry. Żadna wartość trzech
   genów tego nie zmienia.

Do tego dopisz uczciwe ograniczenie: **niska wymiarowość genotypu jest realnym ograniczeniem pracy**
i nie da się wykluczyć, że przy chromosomie kodującym mapę bezpośrednio — jak w artykule — udałoby
się skonstruować mapy leżące na jakimś kompromisie. Naturalny kierunek dalszych badań: rozszerzyć
genotyp o parametry topograficzne albo przejść na kodowanie mapy wprost i sprawdzić, czy front się
otworzy.

---

## 12. Weryfikacja rozdziału 11 — co potwierdzone, co poprawione, co nowego

Rozdział 11 powstał z lektury artykułu z Natural Computing. Ta sekcja jest jego niezależną
kontrolą: sprawdzeniem twierdzeń w kodzie Unity, w `pipeline_fuzzy.py` i w danych pilotażowych.

### 12.1. Potwierdzone w kodzie — obie „poważne" rozbieżności są prawdziwe

**Reconquering Rate.** [BotTurnManager.cs](Assets/Scripts/BotTurnManager.cs), linia 320:
`reconqRate = (currentMatchReconquers / totalLand) * 100`. Brak dzielenia przez liczbę tur —
dokładnie tak, jak opisano w rozdz. 11.3. Sam warunek zliczania przejęcia
(`prevOwner != 0 && ownerId != 0 && prevOwner != ownerId`) odpowiada natomiast definicji ζ z wzoru
(6) co do joty: liczymy tylko pola przechodzące między botami, z pominięciem zajmowania neutrali.
Rozbieżność dotyczy więc **wyłącznie normalizacji**, a nie tego, co jest liczone.

**Peak Differences.** Linie 245–246, 261 i 267: dla każdej z trzech wielkości zapamiętywane jest
`if (wartość > dotychczasowe_maksimum) maksimum = wartość`, gdzie wartość jest **modułem** różnicy.
Amplituda wahnięcia ze wzoru (7) nie jest liczona. Potwierdzone.

### 12.2. Poprawione — jedno twierdzenie z rozdz. 11.3 było błędne

Punkt 6 (Military Imbalance a garnizon bazy) został przepisany na miejscu. Skrót: doliczanie
garnizonu jest **zgodne** z artykułem, bo w Planet Wars wszystkie statki stoją na planetach,
łącznie z macierzystą, i wszystkie wchodzą do tej metryki. Nie było odstępstwa, więc nie ma czego
uzasadniać.

Dwa drobniejsze doprecyzowania do tabeli w rozdz. 11.2:

- „funkcje przynależności ogólne, **nieskalibrowane**" jest o pół kroku za mocne. Dla pięciu
  zmiennych — trzech nierównowag, długości gry i wskaźnika podboju — rzeczywiście rozpięto je na
  teoretycznym zakresie [0, 1], a dla punktów kulminacyjnych
  na [0, 2]. Ale dla Z autorzy **ścisnęli dziedzinę do 0,1**, uzasadniając to zdaniem „while this
  variable can theoretically range from 0 to 1, in practice it is more likely to take values closer
  to the lower end". To jest kalibracja — tyle że zrobiona z oka i tylko dla jednej zmiennej.
  Poprawne sformułowanie: **systematycznej kalibracji na rozkładach empirycznych w artykule nie ma**,
  jest jedno doraźne dopasowanie zakresu. Nasz wkład zostaje, ale opisany precyzyjnie.
- cytat „a very unbalanced game is likely going to be short or feature less alternation between the
  players" pochodzi z **artykułu przeglądowego IJIMAI**, nie z Natural Computing. Odpowiednik w NC
  brzmi „a very imbalanced game ends very early and/or is likely to exhibit less comebacks from one
  of the players" (rozdz. 5). Skoro rozdz. 11 zaleca cytować z NC, w rozdz. 5 trzeba podmienić
  wersję albo wprost oznaczyć źródło każdego cytatu.

### 12.3. Nowe — najtwardszy dowód, że progi z artykułu nie przenoszą się na inną grę

Rozdział 5 pracy (kalibracja) opierał się dotąd na argumencie „progi wzięte z sensu dawały rozpiętość
ocen 0,02". Teraz można pokazać coś mocniejszego: **funkcje przynależności z artykułu, użyte wprost,
uznałyby każdą naszą mapę za niedynamiczną.**

Wzór (6) daje Z jako średnią na turę w zakresie 0–1. Przeliczone na naszych danych pilotażowych
(50 konfiguracji, `pilotaz_wyniki.json`):

| | wartość |
|---|---|
| Z wg wzoru (6), zakres | **0,00057 – 0,00355** |
| Z, mediana | 0,00202 |
| początek zbioru WYSOKI w artykule (rys. 2c) | **0,1** |
| konfiguracji osiągających zbiór WYSOKI | **0 z 50** |

Wszystkie nasze mapy leżą **około trzydziestu razy poniżej** progu, od którego autorzy zaczynają
uznawać przejmowanie pól za częste. Wszystkie miałyby Z = NISKI, a wtedy odpala się reguła
dynamizmu nr 7 („if K is lo or Z is lo or T is very lo then dyn is lo") i **każda mapa dostaje
dynamizm NISKI, niezależnie od tego, co się na niej działo**.

Przyczyna jest czysto mechaniczna i warto ją nazwać: u autorów plansza ma 15–30 planet, więc jedna
zmiana właściciela to 3–7 % mapy. U nas plansza ma 360 pól, więc jedna zmiana to 0,28 %. Metryka
znormalizowana przez liczbę obiektów **nie jest przenośna między grami o różnej ziarnistości mapy**.

To jest gotowy, jednostronicowy dowód tezy całej pracy — mocniejszy od dotychczasowego, bo nie
mówi „progi były źle dobrane", tylko „progi z artykułu, przeniesione dosłownie, dają wynik
degenerujący". Wstaw go na początek rozdziału o kalibracji.

### 12.4. Nowe — co dokładnie da wdrożenie wzoru (7) i dlaczego warto to zrobić

Rozdz. 4.3 zostawia wybór między wdrożeniem wzoru z artykułu a uczciwym opisem uproszczenia.
Argument za wdrożeniem jest silniejszy, niż się tam wydaje, a wynik da się przewidzieć.

Każdy mecz zaczyna się symetrycznie: obaj boty mają po jednym polu, więc różnica ze znakiem
d = (φ¹−φ²)/(φ¹+φ²) wynosi w turze zerowej dokładnie 0. Stąd max_j(d) ≥ 0 i min_j(d) ≤ 0, czyli

    Δ_artykuł = max(d) − min(d) = max(d) + |min(d)|  ≥  max|d| = nasza obecna metryka

Obie metryki są **równe tylko wtedy, gdy prowadzenie ani razu nie przechodzi na drugą stronę**.
A my wiemy z pomiaru, że przechodzi: liczba zmian prowadzenia wynosi 9–20 na mecz. Dodatkowy
składnik |min(d)| jest więc zawsze niezerowy i mierzy dokładnie to, czego obecna metryka nie widzi:
**jak daleko zaszedł przeciwnik w swoim najlepszym momencie.**

Trzy konsekwencje:

1. Kierunek metryki najprawdopodobniej wróci do monotonicznego — wysoka wartość zacznie oznaczać
   mecz z wahnięciami, a nie dominację. Wtedy zbiorem pożądanym znów jest WYSOKI, tak jak
   w artykule, a obecne „świadome odstępstwo" znika i zastępuje je zgodność.
2. Odpadnie zarzut, że tercylowa tabela w rozdz. 4.3 jest niemal tautologią.
3. Pojawi się nowe pytanie do opisania: Δ ze wzoru (7) będzie **częściowo redundantny ze zmianami
   prowadzenia**, bo obie wielkości mierzą to samo zjawisko — pierwsza w sposób ciągły, druga
   dyskretnie. To nie jest problem, tylko materiał na akapit: nasza własna metryka okazała się
   dyskretnym przybliżeniem tej, którą autorzy zdefiniowali w sposób ciągły.

Koszt: kilkanaście linii w `BotTurnManager.cs` (zapamiętywać `min` i `max` różnicy **ze znakiem**
zamiast maksimum modułu, dla wszystkich trzech zasobów) plus jeden pilotaż. Przy tempie zmierzonym
w przemiacie granic (840 meczów w 17 minut) pilotaż 50 × 20 meczów to około 20 minut. **To jest
najlepszy stosunek zysku do kosztu ze wszystkiego, co zostało na liście.**

Uwaga przy okazji: obecne trzy piki są normalizowane **niejednolicie**. Pik terytorialny dzieli się
przez całą powierzchnię lądu (`ownedA/totalLand − ownedB/totalLand`), a gospodarczy i militarny przez
sumę stanu posiadania obu botów. Artykuł normalizuje wszystkie trzy tak samo, przez (φ¹+φ²).
Praktyczny wpływ jest niewielki, bo wskaźnik podboju wynosi 95–99 %, więc pod koniec meczu — gdy
piki zwykle padają — oba mianowniki są prawie identyczne. Mimo to przy przepisywaniu wzoru warto
ujednolicić, bo inaczej uśredniona wartość `peakAverage` miesza dwie skale.

### 12.5. Nowe — różnica w bazie reguł balansu, którą recenzent może wychwycić

Reguła 3 z artykułu brzmi: „if (Π is lo and Γ is hi) or T is lo then bal is **lo**" — w zapisie
autorów Π to nierównowaga terytorialna, Γ gospodarcza, T długość gry. Czyli równe
terytorium przy bardzo nierównej wartości pól to według autorów balans NISKI.

Nasza tabela w tym samym punkcie mówi co innego: kombinacja (terytorium NISKI, gospodarka WYSOKI,
wojsko NISKI) daje **ŚREDNI**, z uzasadnieniem „boty mają tyle samo pól i wojska, ale jeden
kontroluje wyraźnie bogatsze kafelki". Dopiero przy nierównowadze militarnej WYSOKI schodzimy
do NISKI.

Odstępstwo jest obronne — u nas doszła trzecia zmienna, która w regule autorów nie występuje, więc
werdykt NISKI został przesunięty o jeden poziom w głąb — ale **musi być wymienione w rozdziale
o zmianach względem artykułu**. Recenzent porównujący obie tabele znajdzie ten wiersz w minutę.

### 12.6. Cytat, którego brakuje w rozdz. 11.1 — a jest najważniejszy dla rozdz. 4.4

> „our definition of dynamism implicitly incorporates a component of balance via the peak-difference
> variables: if the game is very imbalanced and one player thoroughly dominates the other, these
> variables may take lower values than in games in which the dominated player makes a comeback"

Autorzy **sami stwierdzają, że ich miara dynamizmu zawiera w sobie składnik balansu**. To jest
bezpośrednie potwierdzenie wniosku z rozdz. 4.4 („w tej grze trudno znaleźć miarę dynamizmu
niezależną od balansu") — i pokazuje, że problem nie jest właściwością naszej gry ani naszych
metryk, tylko samej konstrukcji tych kryteriów. Wstaw ten cytat do rozdz. 4.4 i do dyskusji:
zamienia lokalną obserwację w argument ogólny.

### 12.7. Werdykt

Pełna lektura właściwego artykułu **niczego nie unieważnia w wyniku głównym**. Zbieżność balansu
i dynamizmu utrzymuje się po przeliczeniu kodem projektu (+0,6306), a po normalizacji reconquering
zgodnej z wzorem (6) rośnie do +0,7103. Rozdział 11 jest rzetelny; jedno twierdzenie (punkt 6)
było błędne i zostało poprawione, dwa wymagały doprecyzowania.

Zmienia się natomiast **rozłożenie akcentów**: najmocniejszym dowodem nieprzenośności metryk nie
jest już rozpiętość ocen przed kalibracją, tylko fakt, że próg 0,1 dla Z z artykułu leży trzydzieści
razy powyżej całego naszego rozkładu. A najpilniejszym zadaniem technicznym jest wdrożenie
wzoru (7), bo jest tanie i może zamienić jedno z „odstępstw od artykułu" w „zgodność z artykułem".
