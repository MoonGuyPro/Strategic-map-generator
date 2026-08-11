# -*- coding: utf-8 -*-
"""
Badanie pilotazowe: przemiata przestrzen genotypu i zbiera rozklady wszystkich metryk.

Nie optymalizuje niczego. Sluzy wylacznie do wyznaczenia progow funkcji przynaleznosci
na podstawie tego, co gra faktycznie produkuje, zamiast wpisywac je na oko.

Uruchomienie:  python pilotaz.py
Wynik:         pilotaz_wyniki.json  (przepisy + surowe metryki, do dalszej analizy)
"""
import json
import os
import random
import statistics as st

import pipeline_fuzzy as pf

# ============================================================
# KONFIGURACJA
# ============================================================

LICZBA_CHROMOSOMOW = 50
ZIARNO = 20260809                 # staly seed - probka jest odtwarzalna
PLIK_WYNIKOW = 'pilotaz_wyniki.json'

# Zakresy genow wg GDD 7.1
ZAKRESY = {
    'minSpawnDistance': (8, 18),
    'population_max': (20, 100),
    'populationToCreateNewUnit': (400, 1000),
}

# Metryki do zbadania: klucz w JSON -> etykieta
METRYKI = [
    ('avgTerritorialImbalance', 'Territorial imbalance [%]', 100.0),
    ('avgGrowthImbalance', 'Growth imbalance [%]', 1.0),
    ('avgMilitaryImbalance', 'Military imbalance [%]', 1.0),
    ('reconqueringRate', 'Reconquering rate [%]', 1.0),
    ('leadChanges', 'Zmiany prowadzenia [szt]', 1.0),
    ('leadChangeRate', 'Zmiany prowadzenia /100 tur', 1.0),
    ('fieldBattles', 'Bitwy polowe [szt]', 1.0),
    ('peakDifferences', 'Peak terytorialny [%]', 1.0),
    ('peakGrowthDiff', 'Peak gospodarczy [%]', 1.0),
    ('peakMilitaryDiff', 'Peak militarny [%]', 1.0),
    ('peakAverage', 'Peak sredni z trzech [%]', 1.0),
    ('conqueringRate', 'Conquering rate [%]', 1.0),
    ('gameLength', 'Game length [%]', 1.0),
]


def probkuj_lhs(n, lo, hi, rng):
    """Latin Hypercube: jeden losowy punkt z kazdego z n rownych przedzialow."""
    krok = (hi - lo) / n
    punkty = [lo + krok * i + rng.random() * krok for i in range(n)]
    rng.shuffle(punkty)
    return [max(lo, min(hi, int(round(p)))) for p in punkty]


def zbuduj_populacje(n, ziarno):
    rng = random.Random(ziarno)
    kolumny = {gen: probkuj_lhs(n, lo, hi, rng) for gen, (lo, hi) in ZAKRESY.items()}
    return [{gen: kolumny[gen][i] for gen in ZAKRESY} for i in range(n)]


def kwantyl(dane, q):
    d = sorted(dane)
    if not d:
        return float('nan')
    poz = q * (len(d) - 1)
    dol = int(poz)
    gora = min(dol + 1, len(d) - 1)
    return d[dol] + (d[gora] - d[dol]) * (poz - dol)


def korelacja(a, b):
    ma, mb = st.mean(a), st.mean(b)
    licz = sum((x - ma) * (y - mb) for x, y in zip(a, b))
    mian = (sum((x - ma) ** 2 for x in a) * sum((y - mb) ** 2 for y in b)) ** 0.5
    return licz / mian if mian else float('nan')


def main():
    populacja = zbuduj_populacje(LICZBA_CHROMOSOMOW, ZIARNO)
    meczow = LICZBA_CHROMOSOMOW * pf.MECZOW_NA_CHROMOSOM

    print('=' * 78)
    print(f'PILOTAZ: {LICZBA_CHROMOSOMOW} chromosomow x {pf.MECZOW_NA_CHROMOSOM} meczow = {meczow} meczow')
    print(f'Szacowany czas: ok. {meczow * 1.2 / 60:.0f} min symulacji + start Unity')
    print(f'Raporty tekstowe trafia do katalogu Wyniki_Batch (poza Assets)')
    print('=' * 78)
    print()

    # Timeout z duzym zapasem: obserwowane ~1,2 s na mecz, przyjmujemy 5 s
    limit = pf.NARZUT_STARTU_S + 5 * meczow
    wyniki = pf.evaluate_population(populacja, timeout_s=limit)

    with open(PLIK_WYNIKOW, 'w', encoding='utf-8') as f:
        json.dump({'przepisy': populacja, 'wyniki': wyniki}, f, indent=2)
    print(f'\nZapisano surowe dane do {PLIK_WYNIKOW}')

    print()
    print('=' * 100)
    print('ROZKLADY METRYK  (podstawa do ustawienia progow funkcji przynaleznosci)')
    print('=' * 100)
    print(f"{'metryka':28s}{'min':>8}{'q10':>8}{'q25':>8}{'mediana':>9}{'q75':>8}{'q90':>8}{'max':>8}{'sd':>8}")
    print('-' * 100)
    kolumny = {}
    for klucz, etykieta, skala in METRYKI:
        d = [w[klucz] * skala for w in wyniki]
        kolumny[klucz] = d
        print(f'{etykieta:28s}{min(d):8.1f}{kwantyl(d,.10):8.1f}{kwantyl(d,.25):8.1f}'
              f'{kwantyl(d,.50):9.1f}{kwantyl(d,.75):8.1f}{kwantyl(d,.90):8.1f}'
              f'{max(d):8.1f}{st.pstdev(d):8.1f}')

    print()
    print('=' * 78)
    print('KORELACJE MIEDZY TRZEMA PIKAMI  (czy niosa rozna informacje?)')
    print('=' * 78)
    pary = [('peakDifferences', 'peakGrowthDiff', 'terytorialny <-> gospodarczy'),
            ('peakDifferences', 'peakMilitaryDiff', 'terytorialny <-> militarny  '),
            ('peakGrowthDiff', 'peakMilitaryDiff', 'gospodarczy  <-> militarny  ')]
    for a, b, opis in pary:
        print(f'  {opis}: {korelacja(kolumny[a], kolumny[b]):+.3f}')

    print()
    print('=' * 78)
    print('ZALEZNOSC METRYK OD GENOW  (czy gen w ogole na cos wplywa?)')
    print('=' * 78)
    for gen in ZAKRESY:
        g = [p[gen] for p in populacja]
        print(f'  {gen}:')
        for klucz, etykieta, _ in METRYKI[:6]:
            print(f'      {etykieta:28s} {korelacja(g, kolumny[klucz]):+.3f}')

    odrzucone = sum(1 for w in wyniki
                    if w['gameLength'] < pf.MIN_GAME_LENGTH_PCT or w['conqueringRate'] < pf.MIN_CONQUERING_PCT)
    print()
    print(f'Chromosomow odrzuconych przez bramki poprawnosci: {odrzucone}/{len(wyniki)}')


if __name__ == '__main__':
    main()
