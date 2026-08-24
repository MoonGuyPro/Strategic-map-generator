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
   11 godzin 45 minut na 408 chromosomów. Szersza przestrzeń wymagałaby większej populacji, a budżet
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

**Trzeci, niezależny dowód — z map kontrolnych (rozdz. 7).** Mapa z bazami postawionymi tuż obok
siebie ma jednocześnie **najniższą** średnią nierównowagę terytorialną w całym zestawieniu (0,9 %)
i **najwyższą** amplitudę wahnięcia (100,9 %, przy 81,6 % dla mapy idealnie symetrycznej). Mechanizm:
wzór (7) normalizuje różnicę przez sumę stanu posiadania obu graczy, więc gdy na początku meczu każdy
bot ma po jednym czy dwa pola, zdobycie jednego kafelka daje ogromną wartość względną. Na mapie,
gdzie rozgrywka kończy się po kilkudziesięciu turach, metryka mierzy praktycznie wyłącznie ten szum
z pierwszych tur. W Planet Wars ten sam efekt istnieje, ale mecze trwają dostatecznie długo, by faza
początkowa przestała dominować.

**Jak to sformułować w pracy.** To już nie jest „uproszczenie" ani pomyłka implementacyjna, tylko
**zweryfikowany wynik**: metryka zdefiniowana wzorem z artykułu, zaimplementowana dokładnie tak jak
tam, mierzy w tej grze co innego, niż zakładali autorzy. Dowód jest mocny, bo pochodzi z porównania
obu wersji na tym samym generatorze. Jest to zarazem **ilościowe rozwinięcie tego, co autorzy sami
napisali** — że ich definicja dynamizmu „implicitly incorporates a component of balance via the
peak-difference variables". My tę zawartość zmierzyliśmy: +0,93.

### 4.3.1. Problem, który to rodzi — i podjęta decyzja: punkty kulminacyjne wychodzą z systemu

Skoro pik koreluje z nierównowagą terytorialną na +0,93, a nierównowaga terytorialna jest wejściem
BALANSU, to trzymanie pika wśród wejść DYNAMIZMU sprawia, że **dynamizm częściowo mierzy balans**.
Korelacja obu ocen — czyli wynik główny pracy — byłaby wtedy po części wytworzona przez konstrukcję
systemu, a nie przez mechanikę gry. Recenzent postawi ten zarzut i trzeba mieć na niego liczby.

Rozważono trzy możliwości i policzono każdą na nowym pilotażu, przy tych samych progach:

| co zrobić z punktami kulminacyjnymi | baza reguł | zakres ocen | korelacja z BALANSEM |
|---|---|---|---:|
| **zostawić jako wejście, wartość pożądana ŚREDNIA** — tak było dotychczas | 18 reguł (2 × 3 × 3) | 0,156–0,833 | +0,651 |
| **zostawić jako wejście, ale odwrócić kierunek na NISKI** — zgodnie z tym, co pokazał pomiar | 18 reguł (2 × 3 × 3) | 0,156–0,833 | **+0,834** |
| **usunąć z wejść dynamizmu** i przenieść do metryk diagnostycznych, zostawiając zmiany prowadzenia i wskaźnik odbijania | 6 reguł (2 × 3) | 0,147–0,865 | **+0,586** |

**Najważniejsze: wynik główny przeżywa usunięcie pika.** Nawet w wersji, w której dynamizm nie ma
żadnego wspólnego wejścia z balansem i nie zawiera niczego, co mierzyłoby nierównowagę, korelacja
wynosi **+0,586**. Zbieżność celów nie jest artefaktem konstrukcji systemu rozmytego. To jest
odpowiedź na najgroźniejszy zarzut wobec całej pracy i trzeba ją umieścić w rozdziale z wynikami.

Druga możliwość — odwrócenie kierunku metryki — odpada właśnie dlatego, że daje najwyższą wartość:
+0,834 bierze się stąd, że pik z odwróconym kierunkiem staje się po prostu czwartą metryką balansu.
Byłoby to mierzenie balansu dwa razy i nazywanie tego zbieżnością celów.

**Wdrożono trzecią możliwość: punkty kulminacyjne przestały być wejściem kryterium dynamizmu
i pełnią odtąd rolę metryki diagnostycznej — nadal są liczone i raportowane, ale nie wpływają na
ocenę.** Decyzję oparto na trzech niezależnych przesłankach: korelacji +0,930 z nierównowagą
terytorialną, ujemnych korelacjach z odbijaniem i bitwami oraz paradoksie mapy kontrolnej z bazami
obok siebie (rozdz. 7). Uzasadnienie jest dokładnie tej samej natury co przy Conquering Rate
(rozdz. 4.2): metryka nie jest własnością metody, tylko relacją między metodą a mechaniką gry.
W Planet Wars pik mierzył zwroty akcji, bo tam powroty się zdarzały. Tutaj mierzy dominację, więc
jako wejście dynamizmu jest nie tylko bezużyteczny, ale wręcz szkodliwy — zaciera granicę między
dwoma kryteriami. Przenosimy go do metryk diagnostycznych, obok bitew polowych, i **raportujemy
w pracy jako mierzony, lecz nieużywany**.

Cena tej decyzji: dynamizm zostaje z dwoma wejściami i bazą 6 reguł zamiast 18. Opisz to uczciwie
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

- **Bitwy polowe** (starcia token vs token): korelacja **+0,985** ze wskaźnikiem odbijania. Oba
  zdarzenia są skutkiem tego samego — czasu spędzonego przez oddziały na froncie. Przy tak wysokiej
  korelacji metryka nie wnosi niczego nowego i dlatego pozostaje diagnostyczna.
- **Zmiany prowadzenia**: wyraźnie mniej redundantne (**+0,616** z odbijaniem, czyli około 38 %
  wspólnej wariancji), ale korelują ujemnie z metrykami balansu (**−0,707** z militarną). I to nie
  jest wada pomiaru — **lider może się zmienić tylko wtedy, gdy boty są blisko siebie**, więc
  metryka z definicji mierzy również wyrównanie.

Liczby przeliczone na nowym pilotażu (50 × 60 meczów) i aktualnych definicjach metryk. Wcześniejsze
wersje tej notatki podawały +0,96 / +0,55 / −0,63 — wartości sprzed wdrożenia wzoru (6).

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

Zestawienie uwzględnia decyzję z rozdz. 4.3.1, czyli przeniesienie punktów kulminacyjnych do metryk
diagnostycznych. To jest tabela, którą recenzent przeczyta najuważniej, więc każda pozycja ma podane
uzasadnienie i miejsce, gdzie stoi dowód.

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
| **Zmiany prowadzenia na 100 tur** | liczba przejęć prowadzenia terytorialnego, znormalizowana do długości meczu | w artykule nie ma takiej zmiennej. Jest za to **słowna definicja dynamizmu** — gracz, który „is at a disadvantage at a certain point can regain their position" — którą autorzy zoperacjonalizowali przez piki. My operacjonalizujemy ją wprost. Po przeniesieniu punktów kulminacyjnych do diagnostyki jest to **jedyne wejście dynamizmu niezależne od balansu** |
| **Bitwy polowe** | liczba starć token vs token | metryka diagnostyczna; pokazuje, że dynamizm da się mierzyć również zdarzeniami militarnymi (korelacja +0,985 z odbijaniem, czyli praktycznie ta sama informacja — i to też jest wynik) |
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
Sprawdzone na kilku niezależnych konfiguracjach — wynik jest stabilny. **Na aktualnym systemie
(wzory (6) i (7) z artykułu, punkty kulminacyjne poza systemem oceny, progi z pilotażu
50 × 60 meczów) wynosi +0,586** i to jest
liczba do podania w pracy. Wartość +0,651 pochodzi z wersji, w której punkty kulminacyjne były
jeszcze wejściem dynamizmu — patrz rozdz. 4.3.1.

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
Tego w literaturze nie ma, a stoi za tym 408 ocenionych konfiguracji i ponad dwadzieścia cztery
tysiące rozegranych meczów w samym tylko głównym przebiegu. Łącznie w całym projekcie rozegrano
**około 58 000 meczów**.

**Kontrargument, na który trzeba odpowiedzieć w tym samym rozdziale** — patrz rozdz. 11.5.

### Konsekwencja praktyczna, o której trzeba napisać

Skoro cele są zbieżne na całej przestrzeni, **front Pareto jest wąski** — 5 rozwiązań, o rozpiętości
zaledwie 1,2–1,4 odchylenia standardowego szumu pomiarowego (rozdz. 8). Nie ukrywaj tego, tylko
wyjaśnij: jest to bezpośrednie następstwo zbieżności celów.

**Ale nie pisz już, że front zapadł się do punktu.** Po poprawieniu metryk (wzory 6 i 7 z artykułu,
oraz przeniesieniu punktów kulminacyjnych do diagnostyki) front zaczął wykazywać uporządkowanie:
korelacja obu ocen na samym froncie wynosi −0,574,
rozwiązanie o najlepszym balansie ma najsłabszy dynamizm i odwrotnie. Poprawne sformułowanie brzmi
więc: **cele kooperują globalnie, a wymieniają się dopiero lokalnie, w samym rejonie optimum**, gdzie
oba są już blisko swoich maksimów. Podejście wielokryterialne nie degeneruje się całkowicie — daje
wąski, lecz uporządkowany zbiór kompromisów, dokładnie takiego kształtu, jaki opisali autorzy
artykułu. Szczegóły i liczby w rozdz. 8.

---

## 6. Poprawność eksperymentu — cztery problemy wykryte analizą statystyczną

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
rozkłady. Pierwszy pomiar kontrolny dał **48,4 % przy odchyleniu 0,4 sigma**.

**Późniejszy, znacznie liczniejszy pomiar każe jednak osłabić to twierdzenie.** Kontrola wykonana
przy okazji testu przewagi pierwszego ruchu (300 par, 579 rozstrzygniętych meczów) dała **54,7 %
zwycięstw bota 1 przy odchyleniu 1,85 sigma**. Formalnie mieści się to w granicach szumu, ale:

- efekt idzie w tę samą stronę co pierwotna wada i jest tylko nieco mniejszy od granicy
  wykrywalności, która przy 300 parach wynosi 55,1 %;
- pierwotne 48,4 % pochodziło z próby na tyle małej, że jest zgodne zarówno z 50 %, jak i z 55 %.

**Do pracy napisz to ostrożnie:** poprawka usunęła większą część przewagi pozycyjnej — z 55,8 %
przy 4,7 sigma zeszliśmy do wartości nieistotnej statystycznie — ale **przy obecnej liczbie meczów
nie da się wykluczyć resztkowej przewagi rzędu kilku punktów procentowych.** Rozstrzygnięcie
wymagałoby około 900 par, czyli 1800 meczów. Nie wpływa to na wyniki rozmyte, bo te w ogóle nie
patrzą na to, który bot wygrał, ale wpływa na każdą analizę skuteczności.

To jest gotowy przykład na to, że symetria rozstawienia nie bierze się sama z siebie — nawet przy
losowym generatorze trzeba jej pilnować, a raz wprowadzona poprawka wymaga kontroli na dużej próbie.

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
większa od badanego efektu. Pomiar powtórzono na aktualnych metrykach: **300 par, czyli 600 meczów**
(poprzednio 100 par).

**Wynik 1 — statystyki mapy nie zależą od tego, kto zaczyna.** Wszystkie osiem metryk mieści się
w granicach szumu, odchylenia 0,1–1,1 sigma:

| metryka | zaczynał bot 1 | zaczynał bot 2 | różnica | sigma |
|---|---:|---:|---:|---:|
| Territorial imbalance [%] | 15,74 | 15,32 | +0,42 | 0,7 |
| Growth imbalance [%] | 21,66 | 21,28 | +0,38 | 0,6 |
| Military imbalance [%] | 22,27 | 21,51 | +0,77 | 1,1 |
| Reconquering rate | 67,25 | 67,51 | −0,26 | 0,1 |
| Conquering rate [%] | 96,04 | 95,74 | +0,29 | 0,4 |
| Zmiany prowadzenia /100 tur | 2,54 | 2,74 | −0,20 | 0,7 |
| Bitwy polowe [szt] | 34,78 | 35,04 | −0,26 | 0,2 |
| Liczba tur | 316,82 | 316,32 | +0,50 | 0,1 |

> Drobne zastrzeżenie: wiersz „Reconquering rate" pochodzi z raportów tekstowych, w których
> `GameMetricsCollector` liczył jeszcze starą postać metryki (suma przejęć bez dzielenia przez liczbę
> tur). Nie wpływa to na wniosek, bo obie grupy mierzono identycznie, a wartość służy tu wyłącznie do
> porównania między nimi. Rozbieżność między raportem tekstowym a eksportem JSON została już
> usunięta w kodzie, więc kolejny przebieg poda tę metrykę w skali „% pól na 100 tur".

To uzasadnia, że przy ocenie chromosomów nie trzeba rozdzielać wyników według kolejności ruchu.

**Wynik 2 — nie wykryto przewagi pierwszego ruchu, i to znacznie ostrzej niż poprzednio.** Bot
zaczynający wygrał **51,2 %** (jednostką obserwacji jest para), odchylenie **0,74 sigma**.

| | poprzedni pomiar | **obecny** |
|---|---:|---:|
| par | 100 | **300** |
| odsetek zwycięstw bota zaczynającego | 53,6 % | **51,2 %** |
| odchylenie | 1,0 sigma | **0,74 sigma** |
| granica wykrywalności (2 sigma) | 57 % | **53,1 %** |

**Uczciwe sformułowanie do pracy:** nie wykryto przewagi pierwszego ruchu, a test wyklucza przewagę
większą niż **53,1 %**. Poprzednia wersja mogła wykluczyć dopiero 57 %, więc trzykrotne zwiększenie
próby realnie zawęziło wniosek. Nadal obowiązuje zasada, by pisać „nie wykryto przewagi", a nie
„przewagi nie ma".

### 6.3.1. Pułapka statystyczna, którą ten test ujawnił

Materiał na osobny akapit metodologiczny, bo pokazuje warsztat.

Skrypt liczył istotność, traktując wszystkie 579 rozstrzygniętych meczów jako **niezależne**
obserwacje. To założenie jest fałszywe: dwa mecze w parze rozgrywane są na **tej samej mapie**,
z **tymi samymi pozycjami baz**, więc ich wyniki są dodatnio skorelowane. Skala tej korelacji jest
zaskakująco duża:

| liczba zwycięstw bota 1 w parze | par | oczekiwane przy niezależności |
|---|---:|---:|
| 0 z 2 | 102 | 75 |
| 1 z 2 (podział) | 68 | 150 |
| 2 z 2 | 130 | 75 |

**W 232 parach z 300, czyli w 77 % przypadków, obie rozgrywki na tej samej mapie wygrał ten sam
bot** — przy 50 % oczekiwanych, gdyby o wyniku decydował przypadek. Mapa wraz z rozstawieniem baz
przesądza więc o zwycięzcy w zdecydowanej większości meczów, niezależnie od tego, kto zaczyna.

Konsekwencja rachunkowa: prawdziwy błąd standardowy dla przewagi bazy nr 1 wynosi **2,53 punktu**,
a nie 2,08 punktu przyjmowane przy założeniu niezależności. Istotność spada z pozornych **2,54 sigma
do 1,85 sigma** i wynik przestaje być istotny. Skrypt poprawiono.

Zwróć uwagę na asymetrię, którą warto opisać: **parowanie pomaga tylko temu pytaniu, dla którego
kontrast leży wewnątrz pary.** Dla przewagi pierwszego ruchu jeden mecz w parze zaczyna bot 1,
a drugi bot 2, więc mapa się skraca i błąd standardowy spada do 1,57 punktu. Dla przewagi bazy nr 1
obie połowy pary mają identyczne położenie baz, więc mapa się nie skraca, a korelacja błąd
standardowy **podnosi**. Ten sam zbiór danych daje więc dokładniejszą odpowiedź na jedno pytanie
i mniej dokładną na drugie.

### 6.4. Ile z wyniku pochodzi z mapy, a ile z losu

Eksperyment parowany pozwala rozłożyć zmienność na dwie części, bo dysponujemy dwoma przebiegami
na **identycznym** terenie.

**Najprostszy i najmocniejszy sposób pokazania tego** pochodzi z powtórzonego pomiaru na 300 parach:
**w 232 parach z 300, czyli w 77 % przypadków, obie rozgrywki na tej samej mapie wygrał ten sam
bot.** Gdyby o wyniku decydował wyłącznie przypadek, byłoby to 50 %. Jedno zdanie, jedna liczba,
zero rachunków — a mówi dokładnie to samo co tabela poniżej. Warto postawić je na początku
podrozdziału.

> Rozkład wariancji w tabeli poniżej pochodzi z wcześniejszego przebiegu, na 100 parach i przy
> poprzednich definicjach metryk. Wiersz „Reconquering rate" jest w starej skali. Wniosek — mapa
> wyjaśnia od kilkunastu do około połowy zmienności — nie zależy od jednostek, bo jest ilorazem
> dwóch odchyleń tej samej wielkości, i jest zgodny z wynikiem 77 % podanym wyżej.

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

### WERYFIKACJA ROZSTRZYGAJĄCA: mapa wzorcowa przy parametrach z optimum

**To jest najważniejszy wynik całego rozdziału 7 i najmocniejsza odpowiedź na uwagę promotora.**
Postaw go na początku rozdziału weryfikacyjnego w pracy.

Test powtórzono, zmieniając wyłącznie jedną rzecz: zamiast historycznych genów 12 / 60 / 700 użyto
**11 / 96 / 447**, czyli rozwiązania o najwyższym balansie z frontu Pareto. Mapy, tryby i liczba
meczów bez zmian. Dane: `mapy_kontrolne_optimum_wyniki.json`, uruchomienie
`python test_mapy_kontrolne.py optimum`.

| tryb mapy | teryt % | growth % | mil % | reconq | peaks % | conq % | BALANS | DYNAMIZM |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| symetria obrotowa 180° | **6,4** | **8,1** | **8,9** | **34,6** | 61,2 | 99,3 | **0,8475** | **0,8667** |
| generator normalny | 9,4 | 12,7 | 10,2 | 32,5 | 65,3 | 99,0 | 0,8320 | 0,8468 |
| baza 2 zepchnięta w róg | 12,6 | 20,2 | 11,4 | 20,1 | 65,7 | 94,9 | 0,6058 | 0,8324 |
| bogata strefa przy bazie 1 | 14,5 | **41,1** | **21,0** | 28,7 | 77,0 | 98,1 | 0,1474 | 0,5168 |
| bazy tuż obok siebie | 10,2 | 43,3 | 23,8 | 11,9 | **107,3** | **32,5** | **0,0000** | **0,0000** |

**Mapa wzorcowa osiąga 97,8 % sufitu balansu i dokładnie 100,0 % sufitu dynamizmu.** Jej surowe
nierównowagi — terytorialna 6,4 %, gospodarcza 8,1 %, militarna 8,9 % — są **najniższe, jakie
zaobserwowano gdziekolwiek w tym projekcie.**

#### Mapa wzorcowa bije wszystko, co znalazł algorytm

Porównanie przy identycznych genach 11 / 96 / 447:

| | BALANS | DYNAMIZM | teryt % | growth % | mil % |
|---|---:|---:|---:|---:|---:|
| najlepsze rozwiązanie z frontu NSGA-II | 0,8418 | 0,8504 | 6,8 | 10,4 | 9,2 |
| generator losowy, te same geny | 0,8320 | 0,8468 | 9,4 | 12,7 | 10,2 |
| **mapa wzorcowa, te same geny** | **0,8475** | **0,8667** | **6,4** | **8,1** | **8,9** |

Mapa skonstruowana tak, by być obiektywnie sprawiedliwa, wypada lepiej niż mapa losowa (+0,0155
balansu, 1,52 σ; +0,0199 dynamizmu, 1,79 σ) **oraz lepiej niż najlepsze rozwiązanie, jakie
w 408 ocenach znalazł NSGA-II**.

**Jak to uczciwie sformułować.** Same oceny rozmyte różnią się o 1,5–1,8 odchylenia, więc pojedynczo
są na granicy istotności. Rozstrzygające są jednak surowe metryki, obarczone znacznie mniejszym
błędem względnym: nierównowaga terytorialna spada z 9,4 % do 6,4 %, czyli o **jedną trzecią**,
gospodarcza z 12,7 % do 8,1 %, a wszystkie trzy zmieniają się w tę samą stronę. Poprawne zdanie do
pracy: **funkcja przystosowania przyznaje najwyższą ocenę mapie, o której z konstrukcji wiadomo, że
jest sprawiedliwa — i czyni to niezależnie od tego, że mapa ta nigdy nie brała udziału
w optymalizacji.** To jest dokładnie ten dowód, którego brakowało.

#### Poprzedni wynik był artefaktem parametrów, nie mapy

Wcześniejsza wersja testu dawała mapie wzorcowej zaledwie 0,4148 i nie dawało się rozstrzygnąć, czy
odpowiada za to plansza, czy dobór parametrów świata. Teraz wiadomo — **plansza jest ta sama, zmieniły
się wyłącznie geny**:

| tryb mapy | BALANS przy 12 / 60 / 700 | BALANS przy 11 / 96 / 447 | zmiana |
|---|---:|---:|---:|
| symetria obrotowa | 0,4148 | **0,8475** | **+0,4328** |
| generator normalny | 0,1644 | 0,8320 | +0,6676 |
| baza 2 w rogu | 0,1419 | 0,6058 | +0,4639 |
| bogata strefa | 0,1378 | 0,1474 | +0,0096 |
| bazy obok siebie | 0,0000 | 0,0000 | 0,0000 |

Wniosek do rozdziału z dyskusją: **dobór parametrów świata wpływa na sprawiedliwość rozgrywki
silniej niż geometria planszy.** Ta sama mapa symetryczna, ta sama liczba meczów — a ocena rośnie
z 0,41 do 0,85 wyłącznie dlatego, że świat stał się bogatszy, a jednostki tańsze.

#### Nowy wynik: asymetrię przestrzenną da się naprawić parametrami, zasobowej nie

Najciekawsza rzecz w całym zestawieniu i gotowy materiał na osobny akapit.

| zaburzenie | balans przy 12/60/700 | balans przy 11/96/447 | poprawa |
|---|---:|---:|---:|
| **przestrzenne** — baza 2 w rogu | 0,1419 | **0,6058** | **+0,4639** |
| **zasobowe** — bogata strefa przy bazie 1 | 0,1378 | **0,1474** | **+0,0096** |

Różnica jest prawie pięćdziesięciokrotna, a mechanizm czytelny:

- **Asymetria przestrzenna daje się skompensować.** Bogaty świat i tanie jednostki pozwalają botowi
  zepchniętemu w róg mimo wszystko się rozwinąć i walczyć — nierównowaga terytorialna spada z 22,6 %
  do 12,6 %, a dynamizm z 0,3480 skacze do 0,8324. Mapa przestaje być zepsuta, staje się tylko gorsza.
- **Asymetria zasobowa nie daje się skompensować.** Jeśli jeden bot ma po prostu lepsze pola, żadna
  ilość bogactwa w świecie tego nie wyrówna, bo skalowanie podnosi wartość obu stron proporcjonalnie.
  Nierównowaga gospodarcza spada tylko z 58,3 % do 41,1 % i pozostaje miażdżąca, a balans praktycznie
  nie drgnie.

Jest to wskazówka projektowa wykraczająca poza tę konkretną grę: **przy projektowaniu mapy
rozmieszczenie zasobów wymaga większej staranności niż rozmieszczenie pozycji startowych**, bo błędu
w tym pierwszym nie da się później naprawić strojeniem ekonomii.

#### Bramka poprawności nadal potrzebna

Mapa z bazami obok siebie przy dobrych parametrach rozwija się zauważalnie bardziej niż wcześniej —
wskaźnik podboju rośnie z 6,2 % do 32,5 %, a nierównowaga terytorialna z 0,9 % do 10,2 %, więc
złudzenie „idealnie zbalansowanej" mapy słabnie. Mimo to **podbój 32,5 % nadal leży poniżej progu
60 % i mapa zostaje odrzucona.** Bramka jest więc potrzebna także w rejonie optimum.

Ten tryb po raz kolejny daje też najwyższą amplitudę wahnięcia w całym zestawieniu — **107,3 %**,
przy 61,2 % dla mapy wzorcowej. To trzecie niezależne potwierdzenie, że punkty kulminacyjne mierzą
w tej grze zmienność fazy startowej, a nie dramaturgię (rozdz. 4.3).

---

### Wynik przy genach historycznych 12 / 60 / 700 — wersja poprzednia

Zestawienie poniżej zachowano, bo pokazuje zachowanie systemu przy parametrach dalekich od optimum
i to na nim opiera się kilka wniosków z dalszej części rozdziału.


Przebieg powtórzony na **poprawionym systemie**: wzory (6) i (7) z artykułu, punkty kulminacyjne
przeniesione do metryk diagnostycznych, progi z pilotażu 50 × 60 meczów. Pięć trybów × 60 meczów = 300 meczów. Dane: `mapy_kontrolne_wyniki.json`.

| tryb mapy | teryt % | growth % | mil % | reconq | peaks % | conq % | BALANS | DYNAMIZM |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| symetria obrotowa 180° | **14,0** | **17,1** | **20,0** | **24,5** | **81,6** | 99,4 | **0,4148** | **0,6457** |
| generator normalny | 18,5 | 24,5 | 25,4 | 19,7 | 88,6 | 98,9 | 0,1644 | 0,5000 |
| bogata strefa przy bazie 1 | 21,5 | **58,3** | **44,7** | 5,6 | 86,7 | 74,7 | 0,1378 | 0,1429 |
| baza 2 zepchnięta w róg | **22,6** | 29,6 | 27,7 | 13,8 | 89,4 | 93,1 | 0,1419 | 0,3480 |
| bazy tuż obok siebie | 0,9 | 28,4 | 4,1 | 9,8 | **100,9** | **6,2** | **0,0000** | **0,0000** |

**Oba warunki weryfikacji spełnione:** wzorzec (0,4148) ≥ mapa losowa (0,1644), a mapa losowa
> najlepsza z zepsutych (0,1419).

Każde zaburzenie wykryte przez tę metrykę, która miała je wykryć:

- **bogata strefa** podniosła Growth Imbalance z 24,5 % do **58,3 %**, czyli 2,4-krotnie — to
  bezpośrednie potwierdzenie zasadności przedefiniowania tej metryki (rozdz. 4.1);
- **baza w rogu** dała najwyższą nierównowagę terytorialną (22,6 % wobec 18,5 % na mapie losowej);
- **symetria** dała najniższe wszystkie trzy nierównowagi i najwyższy wskaźnik odbijania.

### Co się poprawiło po zmianie metryk — konkretna liczba

Stary system nie potrafił odróżnić dwóch map zepsutych na różne sposoby: „bogata strefa" dostawała
dynamizm 0,1540, a „baza w rogu" 0,1565. Różnica 0,0025 to zero informacji.

| | stary system | **nowy system** |
|---|---:|---:|
| dynamizm — bogata strefa | 0,1540 | 0,1429 |
| dynamizm — baza w rogu | 0,1565 | **0,3480** |
| odstęp między nimi | 0,0025 | **0,2051** |

Odstęp wzrósł **82-krotnie**. To ma sens merytoryczny: mapa z bazą w rogu jest niesprawiedliwa
przestrzennie, ale rozgrywka na niej faktycznie trwa i front się przesuwa (podbój 93,1 %,
odbijanie 13,8), podczas gdy mapa z bogatą strefą jest rozstrzygana jednostronnie i szybko
(podbój 74,7 %, odbijanie 5,6). Stary system tego nie widział, nowy widzi. **To jest najlepszy
dowód, że poprawka metryk zwiększyła rozdzielczość oceny, a nie tylko przesunęła liczby.**

### Podłoga nierównowagi zależy od parametrów świata, nie tylko od mapy

Zestawienie obu przebiegów daje wniosek, którego nie widać w żadnym z nich osobno.

Mapa o **idealnej symetrii obrotowej** — warunki startowe tożsame z konstrukcji, nie z przybliżenia —
daje różne nierównowagi w zależności od parametrów świata:

| geny | teryt % | growth % | mil % | BALANS |
|---|---:|---:|---:|---:|
| 12 / 60 / 700 (ubogi świat, drogie jednostki) | 14,0 | 17,1 | 20,0 | 0,4148 |
| **11 / 96 / 447 (optimum)** | **6,4** | **8,1** | **8,9** | **0,8475** |

Ta sama plansza, ta sama procedura, 60 meczów w obu przypadkach. Różnica bierze się wyłącznie
z reguł ekonomii i kosztu jednostek.

**Interpretacja do pracy.** Zmierzona nierównowaga ma podłogę wyznaczoną przez losowość symulacji —
mnożnik strat w walce 0,8–1,2 i zależność dalszych zdarzeń od pojedynczych rozstrzygnięć — ale
**wysokość tej podłogi nie jest stała: sama zależy od parametrów świata.** W świecie ubogim,
z drogimi jednostkami, pojedyncze przegrane starcie waży bardzo dużo, bo odtworzenie oddziału trwa
długo; rozbieżność narasta i nawet idealnie symetryczna plansza kończy z nierównowagą 14 %.
W świecie bogatym, z tanimi jednostkami, straty odbudowuje się szybko, pojedyncze zdarzenie nie
przesądza o przebiegu i ta sama plansza schodzi do 6,4 %.

To jest ta sama obserwacja co w rozdz. 6.4 („mapa wyjaśnia jedynie 12–53 % zmienności wyniku"),
pokazana od strony przyczyny: **udział mapy w wyniku rośnie wraz z bogactwem świata, bo maleje udział
przypadku.** Jest to zarazem wyjaśnienie, dlaczego NSGA-II zbiegł właśnie do bogatego świata i tanich
jednostek — takie parametry nie tylko dają lepsze mapy, ale też **czynią jakość mapy w ogóle
mierzalną**.

Uwaga metodologiczna wynikająca z powyższego: **progów systemu rozmytego nie da się interpretować
w oderwaniu od parametrów świata.** Kalibrowano je na losowej próbce z całej przestrzeni genotypu,
w której przeważają konfiguracje przeciętne, więc mediana rozkładu (terytorialna 14,25 %) odpowiada
mniej więcej temu, co daje świat ubogi. Dlatego mapa wzorcowa przy genach 12 / 60 / 700 wypada
„przeciętnie" — nie dlatego, że jest przeciętna, lecz dlatego, że skala jest ustawiona na przeciętne
warunki.

### Najciekawszy przypadek: bazy obok siebie

Ten tryb miał **najniższą** średnią nierównowagę terytorialną z całego zestawienia — **0,9 %** —
oraz najniższą militarną (4,1 %). Wyglądał więc na mapę idealnie zbalansowaną, bo gra kończy się,
zanim ktokolwiek zdąży zbudować przewagę.

Odrzuciła go dopiero **bramka poprawności**: wskaźnik podboju **6,2 %** wobec progu 60 %.

To jest empiryczne uzasadnienie decyzji o przeniesieniu Conquering Rate z wejść systemu do warunków
dopuszczenia wyniku (rozdz. 4.2). Gdyby pozostał zwykłym wejściem, mapa ta dostałaby wysoką ocenę,
a NSGA-II ewoluowałby w stronę map, na których rozgrywka kończy się po kilkudziesięciu turach.

**Ta sama mapa dostarcza drugiego, niezależnego dowodu — tym razem przeciwko punktom kulminacyjnym.**
Ma ona jednocześnie:

- **najniższą** średnią nierównowagę terytorialną w całym zestawieniu: 0,9 %,
- **najwyższą** amplitudę wahnięcia przewagi: 100,9 % (mapa wzorcowa ma 81,6 %).

Dwie metryki, które w zamyśle autorów artykułu miały opisywać powiązane zjawiska, wskazują tu
w przeciwne strony. Mechanizm jest prosty i wart opisania: wzór (7) normalizuje różnicę przez sumę
stanu posiadania obu graczy, więc **na początku meczu, gdy każdy bot ma po jednym czy dwa pola,
zdobycie jednego kafelka daje ogromną wartość względną.** Na mapie, na której rozgrywka kończy się
po kilkudziesięciu turach i nikt nie zdąży się rozwinąć, metryka mierzy praktycznie wyłącznie ten
szum z pierwszych tur. W Planet Wars problem też istnieje, ale tam mecze trwają dostatecznie długo,
by faza początkowa przestała dominować.

Jest to trzeci, niezależny argument za usunięciem tej metryki z wejść dynamizmu (rozdz. 4.3.1) —
obok korelacji +0,930
z nierównowagą terytorialną i ujemnych korelacji z odbijaniem oraz bitwami.

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

**To jest bardzo dobry fragment pracy** — pokazuje, że weryfikacja nie była formalnością, tylko
wykryła realną wadę kalibracji, którą naprawiono przed uruchomieniem optymalizacji. Poprawka
utrzymała się także po przeliczeniu progów na nowym pilotażu: na dnie skali jest nadal
1 konfiguracja z 50.

### Zastrzeżenie do zapisania

Skala ocen pozostaje **względna wobec rozkładu z pilotażu**. Mapa z bogatą strefą ma Growth
Imbalance 58,3 %, podczas gdy generator normalny wytwarza 12,0–32,5 % — jest poza zakresem
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

Przebieg wykonany na **poprawionym systemie metryk**: wzory (6) i (7) z artykułu, punkty
kulminacyjne poza systemem oceny, progi z pilotażu 50 × 60 meczów. Populacja 20, 25 pokoleń, 60 meczów na ocenę chromosomu,
**408 ocenionych konfiguracji**, 11 godzin 45 minut, zero odrzuceń przez bramki poprawności.
112 genotypów powtórzyło się i zostało pobranych z pamięci — sam ten fakt jest oznaką zbieżności.

### Znalezione optimum

| gen | na froncie Pareto | mediana wszystkich 408 ocen | dozwolony zakres |
|---|---|---|---|
| `population_max` | 90–99 | 91 | 20–100 |
| `populationToCreateNewUnit` | 412–447 | 439 | 400–1000 |
| `minSpawnDistance` | 10–13 | 11 | 8–18 |

Przepis na dobrą mapę pozostaje ten sam co przed poprawką metryk: **bogaty świat, tanie jednostki,
umiarkowany dystans startowy.** To jest ważny wynik sam w sobie — zmiana dwóch wzorów metryk,
przeliczenie wszystkich progów i usunięcie jednego wejścia z kryterium dynamizmu **nie przesunęły
optimum**. Wniosek projektowy jest więc odporny na szczegóły konstrukcji funkcji oceny.

### Front Pareto — 5 rozwiązań niezdominowanych

| # | spawnDist | popMax | unitCost | BALANS | DYNAMIZM | teryt % | growth % | mil % | reconq | lead/100 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 11 | 96 | 447 | **0,8418** | 0,8504 | 6,8 | 10,4 | 9,2 | 27,7 | 5,18 |
| 2 | 13 | 90 | 447 | 0,8403 | 0,8597 | 7,9 | 10,7 | 10,5 | 33,1 | 4,19 |
| 3 | 11 | 99 | 447 | 0,8402 | 0,8659 | 7,9 | 10,3 | 9,4 | 33,0 | 4,80 |
| 4 | 11 | 96 | 412 | 0,8379 | 0,8660 | 8,3 | 11,2 | 10,2 | 33,2 | 4,89 |
| 5 | 10 | 91 | 446 | 0,8292 | **0,8664** | 9,8 | 12,7 | 11,0 | 33,5 | 4,81 |

### NAJWAŻNIEJSZA ZMIANA: front ma teraz kształt

W poprzednim przebiegu front był płaski w dynamizmie — cztery rozwiązania różniły się dynamizmem
o 0,0016, przy szumie pomiarowym 0,0138. Rozpiętość wynosiła więc **0,12 odchylenia standardowego**,
czyli była czystym szumem. Po poprawce metryk sytuacja się zmieniła:

| | przebieg stary | **przebieg nowy** |
|---|---:|---:|
| rozpiętość frontu — BALANS | 0,0072 | **0,0126** |
| rozpiętość frontu — DYNAMIZM | 0,0016 | **0,0160** |
| odchylenie standardowe ocen w rejonie optimum — BALANS | 0,0068 | 0,0102 |
| odchylenie standardowe ocen w rejonie optimum — DYNAMIZM | 0,0138 | 0,0111 |
| rozpiętość / odchylenie — BALANS | 1,06× | **1,24×** |
| rozpiętość / odchylenie — DYNAMIZM | **0,12×** | **1,44×** |
| korelacja obu ocen na samym froncie | brak porządku | **−0,574** |

Front jest teraz **monotoniczny**: rozwiązanie nr 1 ma najlepszy balans i najsłabszy dynamizm,
rozwiązanie nr 5 odwrotnie, a korelacja obu ocen na froncie wynosi −0,574. Jest to dokładnie ten
kształt, który opisali autorzy artykułu: łagodny spadek dynamizmu przy rosnącym balansie.

**Jak to sformułować — uczciwie, bo efekt jest słaby.** Rozpiętość frontu przekracza szum
1,2–1,4-krotnie. To jest za mało, by mówić o wyraźnym kompromisie (przekonujące byłoby około
2 odchyleń), ale wystarczająco dużo, by przestać mówić o czystym szumie. Poprawne sformułowanie:
**po poprawieniu metryk front przestał być zdegenerowany i zaczął wykazywać uporządkowanie zgodne
z artykułem, choć rozpiętość pozostaje bliska granicy rozdzielczości pomiaru.**

Warto przy tym powiedzieć wprost, co ten wynik zmienia w tezie z rozdz. 5. **Nie unieważnia jej.**
Cele nadal kooperują globalnie — korelacja na 50 losowych konfiguracjach z całej przestrzeni
genotypu wynosi +0,586. Kompromis pojawia się dopiero **lokalnie, w samym rejonie optimum**, gdzie
oba kryteria są już blisko swoich maksimów i dalsza poprawa jednego kosztuje drugie. To jest
bogatszy i bardziej wiarygodny obraz niż poprzedni: zbieżność na całej przestrzeni, wymiana dopiero
na jej najlepszym skraju.

### Uwaga metodologiczna: dwie różne korelacje, dwa różne pytania

Na 408 chromosomach z tego przebiegu korelacja balansu z dynamizmem wynosi **+0,797**, a na
50 konfiguracjach z pilotażu **+0,586**. Różnica nie jest sprzecznością — to dwa różne pomiary:

- pilotaż losuje konfiguracje metodą Latin Hypercube z **całej** przestrzeni genotypu, więc daje
  nieobciążoną odpowiedź na pytanie „czy w tej grze mapy zbalansowane są też dynamiczne";
- NSGA-II koncentruje próbkowanie tam, gdzie jest dobrze, i zawiera potomstwo dobrych rodziców,
  więc jego populacja **nie jest losową próbą** przestrzeni.

**W pracy podawaj +0,586 jako wynik główny** i wspominaj +0,797 wyłącznie jako obserwację
z przebiegu optymalizacji, z tym zastrzeżeniem. Podanie wyższej liczby bez wyjaśnienia byłoby
metodologicznie nieuczciwe.

### Zbieżność — jeszcze szybsza niż poprzednio

Hiperobjętość (krzywa skumulowana, odtwarzalna z `nsga2_front.json`, klucz `historia`):

| pokolenie | ocen łącznie | hiperobjętość |
|---:|---:|---:|
| 1 | 20 | 0,7011 |
| 2 | 40 | 0,7227 |
| 9 | 173 | 0,7258 |
| 11 | 209 | 0,7279 |
| 15 | 275 | 0,7280 |
| 22 | 376 | 0,7293 |
| 25 | 408 | 0,7293 |

**99 % końcowej wartości osiągnięto już w pokoleniu 2.** Reszta przebiegu, czyli ponad
10 godzin obliczeń, poprawiła wynik o 0,0066. Wniosek do opisania jest mocniejszy niż poprzednio:
dla tej przestrzeni parametrów wystarcza **kilka pokoleń**, a nie kilkanaście. Wartości
hiperobjętości nie porównuj między przebiegami — system oceny się zmienił, więc skala też.

### Ograniczenie, które wyszło na jaw: dynamizm nasyca się u góry

Rzecz do opisania w rozdziale o ograniczeniach, bo jest widoczna w danych.

**118 z 408 chromosomów (29 %) osiągnęło dynamizm co najmniej 0,860**, przy matematycznym sufcie
0,8667. Dla porównania balans przekroczył 0,840 tylko 3 razy na 408. Przyczyna: oba wejścia
dynamizmu nasycają się w rejonie optimum. Zmiany prowadzenia dochodzą do 6,00 na 100 tur, przy
punkcie pełnego nasycenia zbioru WYSOKI ustawionym na 4,91 (maksimum z pilotażu), a wskaźnik
odbijania do 35,6 przy nasyceniu na 34,3.

Ma to dwie konsekwencje, obie warte jednego akapitu:

1. **Zmierzona rozpiętość frontu w dynamizmie jest wartością dolną.** Gdyby skala nie kończyła się
   tam, gdzie się kończy, kompromis mógłby okazać się szerszy. Wniosek „front ma kształt" jest więc
   bezpieczny, a wniosek „kształt jest słaby" — obciążony w stronę zaniżenia.
2. **Kalibracja progów pochodzi z innego rozkładu niż rejon optimum.** Progi wyznaczono na losowej
   próbce z całej przestrzeni genotypu, a NSGA-II pracuje w jej najlepszym zakątku, gdzie wartości
   wychodzą poza zmierzony wtedy rozkład. To ta sama uwaga, która dotyczyła punktów kulminacyjnych.
   Naturalne domknięcie: powtórna kalibracja progów dynamizmu na rozkładzie z okolic optimum,
   z punktem nasycenia przesuniętym na 6,0 i 35,6. Wymagałoby to kolejnego przebiegu, więc jest
   kandydatem na „dalsze badania", a nie zadaniem do tej pracy.

### Problem dwóch genów na krawędzi zakresu — w dużej mierze zniknął

Poprzedni przebieg miał `population_max` = 99 w 138 z 428 chromosomów i `populationToCreateNewUnit`
≤ 450 w 234 z 428, a front zawierał wartości 99, 99, 99 oraz 402. Optimum leżało na krawędzi
dozwolonej przestrzeni.

Teraz:

| | stary przebieg | **nowy przebieg** |
|---|---:|---:|
| `population_max` ≥ 99 | 138 / 428 | **67 / 408** |
| `population_max` na froncie | 98–99 | **90, 91, 96, 96, 99** |
| `populationToCreateNewUnit` na froncie | 402–619 | **412–447** |
| mediana `population_max` | 99 | **91** |

Optimum przesunęło się do **wnętrza** dozwolonej przestrzeni. Zarzut „zakres genotypu mógł obciąć
lepsze rozwiązania" traci więc znaczną część siły już na podstawie samego rozkładu wyników.

**Zastrzeżenie:** przemiat poza granice zakresów (opisany poniżej) wykonano na **poprzedniej** wersji
metryk. Jego wnioski jakościowe — nasycenie oceny poza zakresem i płaskowyż wokół optimum — pozostają
prawdopodobne, ale liczby nie są porównywalne z obecnym systemem. Jeśli chcesz mieć cały rozdział na
jednych metrykach, wystarczy powtórzyć `test_granic_genow.py`, około 17 minut.

### Przemiat poza granice zakresów genów — powtórzony na aktualnych metrykach

14 konfiguracji × 60 meczów = 840 meczów, 17 minut. Dwa jednowymiarowe przekroje wokół punktu
odniesienia (spawn 10, popMax 99, unitCost 420). Dane: `granice_genow_wyniki.json`.

**Przemiat `population_max`** (zadeklarowana granica: 100):

| wartość | BALANS | DYNAMIZM | teryt % | growth % | reconq | peaks % | conq % | długość % |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 90 | 0,8235 | 0,8430 | 10,3 | 14,6 | 28,3 | 66,8 | 98,5 | 64,9 |
| 99 (odniesienie) | 0,8395 | 0,8634 | 7,9 | 11,0 | 31,6 | 62,1 | 98,8 | 76,8 |
| 100 | 0,8219 | 0,8483 | 10,9 | 15,0 | 32,1 | 67,3 | 98,2 | 75,5 |
| 120 | 0,8305 | 0,8592 | 9,6 | 13,1 | 33,7 | 58,9 | 98,2 | 75,4 |
| 140 | 0,8359 | **0,8667** | 8,7 | 11,4 | 34,6 | 60,5 | 99,3 | 79,2 |
| 160 | 0,8331 | 0,8665 | 8,9 | 12,5 | 34,2 | 60,4 | 98,6 | 77,5 |
| 180 | **0,8410** | **0,8667** | 7,6 | 10,6 | 34,8 | 58,3 | 98,0 | 76,7 |
| 200 | 0,8297 | 0,8569 | 9,7 | 12,9 | 36,1 | 62,2 | 99,3 | 79,6 |

**Przemiat `populationToCreateNewUnit`** (zadeklarowana granica: 400):

| wartość | BALANS | DYNAMIZM | teryt % | growth % | reconq | conq % | długość % |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 150 | 0,8235 | 0,8508 | 10,7 | 14,0 | 32,7 | 99,1 | 76,8 |
| 250 | 0,8332 | 0,8658 | 9,1 | 12,0 | 33,0 | 98,9 | 74,0 |
| 350 | 0,8319 | 0,8632 | 9,3 | 12,8 | 31,6 | 98,0 | 74,1 |
| 420 | **0,8395** | 0,8634 | 7,9 | 11,0 | 31,6 | 98,8 | 76,8 |
| 500 | 0,8337 | 0,8574 | 8,8 | 12,4 | 29,5 | 97,2 | 66,2 |
| 650 | 0,8244 | 0,8427 | 10,6 | 14,4 | 29,2 | 97,5 | 77,5 |
| 800 | 0,8132 | 0,8323 | 11,6 | 16,8 | 23,9 | 91,1 | 68,2 |

### Skrypt wypisał „zakres obciął optimum" — i był to fałszywy alarm

Wynik wart opisania w pracy, bo pokazuje, jak łatwo o błędny wniosek statystyczny. Skrypt orzekł, że
poza zakresem `population_max` jest **istotnie** lepiej (+0,0174 balansu przy progu 0,0136). Werdykt
był nieprawdziwy z trzech niezależnych powodów, wszystkie już naprawione w kodzie:

1. **Nieaktualna stała szumu.** Skrypt używał wartości 0,0068 i 0,0138, zmierzonych na *poprzednim*
   systemie oceny. Na obecnym odchylenie standardowe ocen w rejonie optimum wynosi **0,0102 i 0,0111**
   (275 chromosomów z przebiegu NSGA-II). Właściwy próg to zatem 0,0204, a nie 0,0136.
2. **Punkt odniesienia nie trafił do grupy „w zakresie".** Konfiguracja `population_max` = 99 była
   oceniana w tym samym przebiegu — jako punkt odniesienia drugiego przekroju — ale w porównaniu
   uwzględniano wyłącznie wartości 90 i 100. Porównywano więc najlepszy punkt spoza zakresu
   z konfiguracjami *gorszymi od znalezionego optimum*. Po dołączeniu punktu 99 najlepszy wynik
   w zakresie rośnie z 0,8235 do **0,8395**, a różnica spada z +0,0174 do **+0,0014**.
3. **Porównywano maksimum z maksimum przy różnej liczbie próbek.** Maksimum z 5 losowań jest
   z definicji wyższe niż maksimum z 3, nawet gdy rozkłady są identyczne. Sam ten efekt daje
   przewagę około **+0,006** na czysto losowej podstawie.

Po poprawieniu wszystkich trzech, przy porównaniu **średnich** z właściwym błędem standardowym:

| przemiat | wielkość | w zakresie | poza zakresem | różnica | istotność |
|---|---|---:|---:|---:|---|
| `population_max` | BALANS | 0,8283 | 0,8340 | +0,0057 | 0,77 σ — nieistotne |
| `population_max` | DYNAMIZM | 0,8515 | 0,8632 | +0,0116 | 1,44 σ — nieistotne |
| `populationToCreateNewUnit` | BALANS | 0,8277 | 0,8296 | +0,0019 | 0,24 σ — nieistotne |
| `populationToCreateNewUnit` | DYNAMIZM | 0,8489 | 0,8599 | +0,0110 | 1,30 σ — nieistotne |

**Wniosek: zakres genotypu nie obciął optimum w sposób dający się wykazać.** Jest to zgodne z tym,
co widać w samym przebiegu NSGA-II: optimum weszło do wnętrza dozwolonej przestrzeni (front zawiera
`population_max` 90–99, a nie same 99).

### Ale trzeba dopisać zastrzeżenie, bo „brak poprawy" nie znaczy „nie ma poprawy"

Powyżej `population_max` ≈ 140 **funkcja oceny przestaje cokolwiek widzieć**:

| popMax | DYNAMIZM | reconquering |
|---:|---:|---:|
| 120 | 0,8592 | 33,7 |
| 140 | **0,8667 = sufit** | 34,6 |
| 160 | 0,8665 | 34,2 |
| 180 | **0,8667 = sufit** | 34,8 |
| 200 | 0,8569 | 36,1 |

Wskaźnik odbijania przekracza tam punkt pełnego nasycenia zbioru WYSOKI (34,30), a dynamizm dobija
do matematycznego sufitu 0,8667 co do czwartego miejsca po przecinku. Kryterium dynamizmu jest
w tym rejonie **ślepe z konstrukcji**.

Surowe metryki sugerują przy tym słaby, ale konsekwentny trend dalszej poprawy:

- korelacja `population_max` z nierównowagą terytorialną: **−0,360**, z gospodarczą **−0,443**
- średnia nierównowaga terytorialna: **10,6 %** dla 90–100 wobec **8,8 %** dla 120–200

Trend nie jest monotoniczny (wartość 200 wypada gorzej niż 180) i mieści się w granicach szumu, więc
nie da się na jego podstawie niczego rozstrzygnąć. Uczciwe sformułowanie do pracy brzmi:
**nie wykryto istotnej poprawy poza zadeklarowanym zakresem, przy czym powyżej `population_max` ≈ 140
funkcja przystosowania osiąga sufit i nie byłaby w stanie takiej poprawy wykryć, nawet gdyby
zachodziła.** To jest to samo ograniczenie, które opisano wyżej przy nasyceniu dynamizmu — tutaj
widać je bezpośrednio.

### Co pokazuje przemiat kosztu jednostki

Ten przekrój jest znacznie czytelniejszy i nadaje się na wykres do pracy. Poniżej granicy 400 nie ma
poprawy, powyżej 500 jest wyraźne pogorszenie, monotoniczne we wszystkich metrykach naraz:

- `unitCost` 800: balans **0,8132**, dynamizm 0,8323, nierównowaga terytorialna 11,6 %, odbijanie
  spada do 23,9, a wskaźnik podboju do 91,1 %
- optimum wypada na 420, dokładnie tam, gdzie umieścił je NSGA-II (front: 412–447)

**Dolna granica zakresu 400 była więc dobrana trafnie**, a górna 1000 z dużym zapasem — koszt
powyżej 650 nie ma sensu projektowego.

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
| **Korelacja balans ↔ dynamizm (aktualna)** | **+0,586** — dynamizm bez składnika mierzącego nierównowagę |
| Ta sama korelacja z punktami kulminacyjnymi w dynamizmie | +0,651 — wersja porzucona jako częściowo cyrkularna |
| Ta sama korelacja z odwróconym kierunkiem pików | +0,834 — wersja odrzucona, mierzy balans dwa razy |
| Korelacja Peak Differences z nierównowagą terytorialną | +0,930 wzorem (7); +0,893 starym wzorem |
| Korelacja Peak Differences z odbijaniem / bitwami | −0,304 / −0,399 — kierunek odwrotny niż w artykule |
| Współczynnik kuli śnieżnej (growth vs territorial) | 1,17 przy korelacji +0,94 |
| Udział mapy w zmienności wyniku | 12–53 % (reszta to losowość symulacji) |
| Przewaga pierwszego ruchu | 51,2 % zwycięstw, 0,74 sigma — nie wykryto (300 par) |
| Granica wykrywalności przewagi pierwszego ruchu | 53,1 % (poprzednio 57 % przy 100 parach) |
| Przewaga pozycyjna bazy nr 1 | 55,8 % przed poprawką (4,7 σ) → **54,7 % po (1,85 σ)** |
| Par, w których ten sam bot wygrał obie rozgrywki | **232 z 300 (77 %)**, przy 50 % oczekiwanych |
| Meczów kończących się remisem | 17,1 % |
| **Mapa wzorcowa przy genach z optimum** | **balans 0,8475 · dynamizm 0,8667 — 97,8 % i 100,0 % sufitu** |
| Mapa losowa przy tych samych genach | balans 0,8320 · dynamizm 0,8468 |
| Najlepsze rozwiązanie z frontu NSGA-II | balans 0,8418 · dynamizm 0,8504 — **gorsze od mapy wzorcowej** |
| Najniższe nierównowagi w projekcie (mapa wzorcowa) | terytorialna 6,4 % · gospodarcza 8,1 % · militarna 8,9 % |
| Ta sama mapa wzorcowa przy genach 12 / 60 / 700 | balans 0,4148 · nierównowaga terytorialna 14,0 % |
| Ocena mapy losowej przy genach 12 / 60 / 700 | balans 0,1644 · dynamizm 0,5000 |
| Naprawialność zaburzeń parametrami świata | przestrzenne +0,4639 balansu · zasobowe **+0,0096** |
| Rozdzielczość między dwiema mapami zepsutymi | 0,0025 przed poprawką metryk → **0,2051** po |
| Paradoks map kontrolnych | „bazy obok siebie”: nierównowaga 0,9 % przy amplitudzie 100,9 % |
| Nasycenie skali przed poprawką / po | 24 % / 2 % konfiguracji na dnie |
| NSGA-II: ocenionych konfiguracji | 408 (populacja 20, 25 pokoleń, 11 h 45 min, 112 genotypów z pamięci) |
| NSGA-II: hiperobjętość | 0,7011 → 0,7293; **99 % osiągnięte w pokoleniu 2** |
| Znalezione optimum | popMax 90–99 · unitCost 412–447 · spawnDist 10–13 (mediany 91 / 439 / 11) |
| Optimum przed i po poprawce metryk | ten sam rejon — wniosek projektowy odporny na zmianę oceny |
| Front Pareto: rozpiętość vs szum, BALANS | 0,0126 wobec sd 0,0102 — **1,24×** |
| Front Pareto: rozpiętość vs szum, DYNAMIZM | 0,0160 wobec sd 0,0111 — **1,44×** (poprzednio 0,12×) |
| Korelacja ocen na samym froncie | **−0,574** — widoczny kompromis |
| Korelacja ocen na 408 chromosomach z przebiegu | +0,797 (próba obciążona, nie podawać jako wynik główny) |
| Przemiat poza granice genów | 14 konfiguracji × 60 meczów, 17 min, na aktualnych metrykach |
| Zysk poza zakresem `population_max` | +0,0057 balansu (0,77 σ) · +0,0116 dynamizmu (1,44 σ) — brak |
| Zysk poza zakresem `populationToCreateNewUnit` | +0,0019 balansu (0,24 σ) · +0,0110 dynamizmu (1,30 σ) — brak |
| Szum oceny przy 60 meczach (aktualny system) | balans 0,0102 · dynamizm 0,0111 (275 chromosomów) |
| Ślepota funkcji oceny | powyżej `population_max` ≈ 140 dynamizm dobija do sufitu 0,8667 |
| Sufit oceny rozmytej (balans i dynamizm) | 0,8667 |
| Najlepsze osiągnięte wobec sufitu | balans 0,8418 = 97,1 % · dynamizm 0,8664 = **100,0 %** |
| Chromosomów z dynamizmem na sufcie | 118 z 408 (29 %) — kryterium nasyca się u góry |
| Chromosomów z balansem ≥ 0,840 | 3 z 408 |
| Reconquering wg wzoru (6) z artykułu | 0,0006–0,0036 na turę, przy progu WYSOKI = 0,1 |
| Reguł w bazie balansu | 27 (komplet 3³) |
| Reguł w bazie dynamizmu | 6 (2 × 3), po przeniesieniu punktów kulminacyjnych do diagnostyki |
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
| budżet | 10 000 ewaluacji na przebieg, 10 przebiegów | 408 ewaluacji, po 60 meczów każda |
| funkcje przynależności | ogólne, **nieskalibrowane**, rozpięte na teoretycznym zakresie zmiennej (rys. 2 w artykule) | kalibrowane na kwantylach rozkładów z pilotażu |
| bazy reguł | 3 reguły balansu, 7 reguł dynamizmu, **świadomie niekompletne** | 27 i 6 reguł, komplet |
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
lingwistycznych pojawiły się dziury, stąd konieczność uzupełnienia baz do kompletu — obecnie 27 reguł
balansu i 6 reguł dynamizmu. **Opisz to
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

> **Jak czytać ten rozdział.** Jest to zapis analizy, która doprowadziła do poprawienia metryk —
> powstał **przed** wdrożeniem wzorów (6) i (7) oraz przed przeniesieniem punktów kulminacyjnych do
> diagnostyki. Podawane tu liczby opisują stan sprzed tych zmian i **nie są aktualnymi wynikami
> pracy**; w szczególności korelacja +0,6306 dotyczy systemu, w którym pik był jeszcze wejściem
> dynamizmu, a obecna wartość to **+0,586** (rozdz. 5). Rozdział zachowano, ponieważ dokumentuje
> rozumowanie stojące za zmianami i zawiera argumenty, których nie ma nigdzie indziej — a jeden
> z jego wniosków okazał się później błędny, co samo w sobie jest materiałem na pracę (rozdz. 12.4).

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

### 12.4. Przewidywanie skutków wzoru (7) — i jak wypadło w zderzeniu z pomiarem

**Ten podrozdział zachowano celowo, bo postawiona tu prognoza okazała się w połowie błędna.**
Warto go opisać w pracy jako przykład rzetelnego postępowania: hipotezę sformułowano przed
eksperymentem, a potem uczciwie porównano z wynikiem. Wzór (7) został wdrożony, pilotaż powtórzony,
a rezultat opisano w rozdz. 4.3.

Argumentacja z chwili przed eksperymentem brzmiała następująco.

Każdy mecz zaczyna się symetrycznie: obaj boty mają po jednym polu, więc różnica ze znakiem
d = (φ¹−φ²)/(φ¹+φ²) wynosi w turze zerowej dokładnie 0. Stąd max_j(d) ≥ 0 i min_j(d) ≤ 0, czyli

    Δ_artykuł = max(d) − min(d) = max(d) + |min(d)|  ≥  max|d| = nasza obecna metryka

Obie metryki są **równe tylko wtedy, gdy prowadzenie ani razu nie przechodzi na drugą stronę**.
A my wiemy z pomiaru, że przechodzi: liczba zmian prowadzenia wynosi 9–20 na mecz. Dodatkowy
składnik |min(d)| jest więc zawsze niezerowy i mierzy dokładnie to, czego obecna metryka nie widzi:
**jak daleko zaszedł przeciwnik w swoim najlepszym momencie.**

Przewidywano trzy konsekwencje. Poniżej każda z nich zestawiona z tym, co faktycznie wyszło:

| przewidywanie | co się stało |
|---|---|
| 1. Kierunek metryki wróci do monotonicznego, wartością pożądaną znów będzie WYSOKI, a odstępstwo od artykułu zastąpi zgodność | **NIETRAFIONE.** Kierunek się nie odwrócił. Po wdrożeniu wzoru (7) korelacja pika z nierównowagą terytorialną **wzrosła** z +0,893 do +0,930, a korelacje z odbijaniem i bitwami pozostały ujemne. Metryka trafiła ostatecznie do diagnostyki (rozdz. 4.3.1) |
| 2. Odpadnie zarzut, że tabela tercylowa jest tautologią | **TRAFIONE.** Nowa tabela tercylowa liczona jest wzorem źródłowym, więc nie jest już tautologiczna — i mimo to pokazuje ten sam kierunek, co czyni wniosek znacznie mocniejszym |
| 3. Nowa metryka będzie częściowo redundantna ze zmianami prowadzenia | **NIEISTOTNE.** Pytanie straciło sens, bo pik przestał być wejściem systemu. Zmiany prowadzenia okazały się natomiast redundantne w umiarkowanym stopniu z odbijaniem (+0,616), co opisano w rozdz. 4.6 |

**Dlaczego prognoza zawiodła — to jest właśnie wartościowa część.** Rozumowanie zakładało, że skoro
mecz startuje symetrycznie, to składnik `|min(d)|` zmierzy „jak daleko zaszedł przeciwnik w swoim
najlepszym momencie". Założenie było poprawne formalnie, ale puste treściowo: w grze bez mechaniki
powrotu przegrany nigdy nigdzie nie zachodzi, więc `|min(d)|` mierzy wyłącznie zmienność pierwszych
kilkudziesięciu tur, kiedy obaj boci mają po kilka pól i jedno zdobyte pole daje ogromną wartość
względną. Potwierdziła to później mapa kontrolna z bazami obok siebie: najniższa nierównowaga
w zestawieniu przy najwyższej amplitudzie (rozdz. 7).

**Wniosek metodologiczny do pracy:** poprawne odtworzenie wzoru z literatury nie gwarantuje
odtworzenia jego znaczenia. Wzór przenosi się zawsze, interpretacja tylko wtedy, gdy mechanika gry
spełnia założenia, na których go zbudowano.

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
