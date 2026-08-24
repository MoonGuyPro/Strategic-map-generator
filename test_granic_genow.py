# -*- coding: utf-8 -*-
"""
Sprawdzenie, czy zakresy genow nie obcialy optimum.

NSGA-II zbiegl do krawedzi dozwolonej przestrzeni: population_max = 99 przy gornej granicy 100,
a populationToCreateNewUnit ponizej 450 przy dolnej granicy 400. Nie wiadomo wiec, czy poza tymi
granicami ocena rosnie dalej, czy sie nasyca.

Skrypt wykonuje dwa jednowymiarowe przemiaty wokol znalezionego optimum, siegajac poza
zadeklarowany zakres genotypu. To NIE jest czesc glownego eksperymentu, tylko kontrola
metodologiczna - odpowiedz na pytanie "czy zakres byl wystarczajaco szeroki".

Uruchomienie:  python test_granic_genow.py
Wynik:         granice_genow_wyniki.json
"""
import json
import os
import subprocess

import pipeline_fuzzy as pf

# Optimum znalezione przez NSGA-II
OPT_SPAWN = 10
OPT_POPMAX = 99
OPT_UNITCOST = 420

# Granice zadeklarowanego genotypu (GDD 7.1)
GRANICA_POPMAX = 100
GRANICA_UNITCOST = 400

PRZEMIAT_POPMAX = [90, 100, 120, 140, 160, 180, 200]
PRZEMIAT_UNITCOST = [150, 250, 350, 420, 500, 650, 800]

PLIK_WYNIKOW = 'granice_genow_wyniki.json'

# Zmierzony szum oceny przy 60 meczach: odchylenie standardowe ocen 275 chromosomow z rejonu
# optimum (popMax >= 88, unitCost <= 470, spawn <= 13) z przebiegu NSGA-II na aktualnym systemie.
# Roznica mniejsza niz dwa bledy standardowe to nie jest realna poprawa.
SZUM_BALANS = 0.0102
SZUM_DYNAMIZM = 0.0111


def main():
    przepisy = []
    opis = []

    # Punkt odniesienia (OPT_POPMAX) nalezy do grupy "w zakresie" i musi byc w niej uwzgledniony,
    # inaczej porownujemy najlepszy punkt spoza zakresu z konfiguracjami gorszymi od znalezionego
    # optimum, co samo z siebie wytwarza pozorna przewage.
    for pm in sorted(set(PRZEMIAT_POPMAX) | {OPT_POPMAX}):
        przepisy.append({'minSpawnDistance': OPT_SPAWN, 'population_max': pm,
                         'populationToCreateNewUnit': OPT_UNITCOST, 'mapMode': 0})
        opis.append(('population_max', pm))

    for uc in PRZEMIAT_UNITCOST:
        przepisy.append({'minSpawnDistance': OPT_SPAWN, 'population_max': OPT_POPMAX,
                         'populationToCreateNewUnit': uc, 'mapMode': 0})
        opis.append(('populationToCreateNewUnit', uc))

    meczow = len(przepisy) * pf.MECZOW_NA_CHROMOSOM
    print('=' * 78)
    print('PRZEMIAT POZA GRANICE ZAKRESOW GENOW')
    print('=' * 78)
    print(f'  punkt odniesienia: spawn={OPT_SPAWN}, popMax={OPT_POPMAX}, unitCost={OPT_UNITCOST}')
    print(f'  population_max:            {PRZEMIAT_POPMAX}   (granica GDD: {GRANICA_POPMAX})')
    print(f'  populationToCreateNewUnit: {PRZEMIAT_UNITCOST}   (granica GDD: {GRANICA_UNITCOST})')
    print(f'  konfiguracji: {len(przepisy)} x {pf.MECZOW_NA_CHROMOSOM} meczow = {meczow} meczow')
    print(f'  szacowany czas: ~{meczow * 1.2 / 60:.0f} min')
    print('=' * 78)
    print()

    with open(pf.INPUT_FILE, 'w') as f:
        json.dump({'recipes': przepisy, 'pairedFirstMove': False}, f, indent=4)
    if os.path.exists(pf.OUTPUT_FILE):
        os.remove(pf.OUTPUT_FILE)

    limit = pf.NARZUT_STARTU_S + 5 * meczow
    print(f'--- [PYTHON] Unity: {meczow} meczow, limit {limit} s. Prosze czekac.')
    try:
        subprocess.run(pf.UNITY_CMD, check=True, timeout=limit)
    except subprocess.TimeoutExpired:
        raise SystemExit(f'Unity przekroczylo {limit} s. Sprawdz {pf.LOG_FILE}.')
    except subprocess.CalledProcessError as e:
        raise SystemExit(f'Unity zakonczylo sie kodem {e.returncode}. Sprawdz {pf.LOG_FILE}.')
    if not os.path.exists(pf.OUTPUT_FILE):
        raise SystemExit(f'Brak {pf.OUTPUT_FILE}. Sprawdz {pf.LOG_FILE}.')

    wyniki = json.load(open(pf.OUTPUT_FILE))['results']
    if len(wyniki) != len(przepisy):
        raise SystemExit(f'Unity zwrocilo {len(wyniki)} wynikow zamiast {len(przepisy)}.')

    oceny = [pf.score(m) for m in wyniki]
    with open(PLIK_WYNIKOW, 'w', encoding='utf-8') as f:
        json.dump({'przepisy': przepisy, 'wyniki': wyniki,
                   'oceny': [{'balans': b, 'dynamizm': d} for b, d in oceny]}, f, indent=1)

    def tabela(nazwa_genu, wartosci, granica, kierunek):
        print()
        print('=' * 100)
        print(f'PRZEMIAT: {nazwa_genu}   (granica zadeklarowana: {granica})')
        print('=' * 100)
        print(f"{'wartosc':>10}{'':4}{'BALANS':>9}{'DYNAMIZM':>10}{'teryt%':>8}{'growth%':>9}"
              f"{'reconq%':>9}{'peaks%':>8}{'conq%':>7}{'dlugosc%':>10}")
        print('-' * 100)
        wiersze = []
        for (gen, wart), m, (b, d) in zip(opis, wyniki, oceny):
            if gen != nazwa_genu:
                continue
            poza = (wart > granica) if kierunek == 'gora' else (wart < granica)
            znak = ' <-' if poza else '   '
            print(f'{wart:>10}{znak:>4}{b:>9.4f}{d:>10.4f}'
                  f"{m['avgTerritorialImbalance'] * 100:>8.1f}{m['avgGrowthImbalance']:>9.1f}"
                  f"{m['reconqueringRate']:>9.1f}{m['peakAverage']:>8.1f}"
                  f"{m['conqueringRate']:>7.1f}{m['gameLength']:>10.1f}")
            wiersze.append((wart, b, d, poza))
        print('-' * 100)
        print('  strzalka oznacza wartosci POZA zadeklarowanym zakresem genotypu')

        w_zakresie = [(b, d) for wart, b, d, poza in wiersze if not poza]
        poza_zakresem = [(b, d) for wart, b, d, poza in wiersze if poza]
        if not w_zakresie or not poza_zakresem:
            return
        # Porownujemy SREDNIE, a nie maksima. Maksimum z wiekszej liczby probek jest z definicji
        # wyzsze nawet przy identycznych rozkladach, wiec zestawianie max z 5 punktow z max z 3
        # zawyzalo przewage grupy liczniejszej o okolo 0,6 odchylenia standardowego.
        n_w, n_poza = len(w_zakresie), len(poza_zakresem)
        sr_w = (sum(b for b, _ in w_zakresie) / n_w, sum(d for _, d in w_zakresie) / n_w)
        sr_poza = (sum(b for b, _ in poza_zakresem) / n_poza, sum(d for _, d in poza_zakresem) / n_poza)
        db = sr_poza[0] - sr_w[0]
        dd = sr_poza[1] - sr_w[1]
        se_b = SZUM_BALANS * (1 / n_w + 1 / n_poza) ** 0.5
        se_d = SZUM_DYNAMIZM * (1 / n_w + 1 / n_poza) ** 0.5
        print()
        print(f'  srednia W zakresie    ({n_w} konfiguracji): balans {sr_w[0]:.4f}   dynamizm {sr_w[1]:.4f}')
        print(f'  srednia POZA zakresem ({n_poza} konfiguracji): balans {sr_poza[0]:.4f}   dynamizm {sr_poza[1]:.4f}')
        print(f'  roznica: balans {db:+.4f} ({db / se_b:+.2f} sigma)   dynamizm {dd:+.4f} ({dd / se_d:+.2f} sigma)')
        print(f'  blad standardowy roznicy: balans {se_b:.4f}, dynamizm {se_d:.4f}')
        istotna = db > 2 * se_b or dd > 2 * se_d
        if istotna:
            print('  WNIOSEK: poza zakresem jest ISTOTNIE lepiej - zakres genotypu obcial optimum.')
        else:
            print('  WNIOSEK: poza zakresem NIE jest istotnie lepiej.')
            print('           UWAGA: sprawdz kolumne DYNAMIZM - jesli dobija do sufitu 0,8667,')
            print('           to "brak poprawy" moze oznaczac "funkcja oceny juz nie rozroznia".')

    tabela('population_max', PRZEMIAT_POPMAX, GRANICA_POPMAX, 'gora')
    tabela('populationToCreateNewUnit', PRZEMIAT_UNITCOST, GRANICA_UNITCOST, 'dol')

    print()
    print(f'Surowe dane zapisano do {PLIK_WYNIKOW}')


if __name__ == '__main__':
    main()
