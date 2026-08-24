# -*- coding: utf-8 -*-
"""
Weryfikacja funkcji przystosowania na mapach kontrolnych.

Sprawdza, czy system rozmyty odroznia mape dobra od zlej. Bez tego nie wiadomo, czy oceny
w ogole cokolwiek znacza - dzis wynik 0,13 mowi tylko "najgorsza z wygenerowanych", a nie
"mapa obiektywnie zla".

Porownywane sa piec trybow generatora. Tryb 0 to normalny generator uzywany przez NSGA-II,
pozostale to punkty odniesienia uruchamiane wylacznie na potrzeby tej weryfikacji.

Kryterium sukcesu: mapa symetryczna > mapa losowa > mapy zepsute.

Uruchomienie:  python test_mapy_kontrolne.py
"""
import json
import os
import subprocess
import sys

import pipeline_fuzzy as pf

# Dwa zestawy genow. Wszystkie tryby w jednym przebiegu dostaja ten sam zestaw, wiec porownanie
# miedzy trybami jest uczciwe; miedzy przebiegami - nie, bo zmieniaja sie parametry swiata.
#
#   domyslny   - historyczny zestaw, na ktorym wykonano pierwsza weryfikacje
#   optimum    - rozwiazanie o najwyzszym balansie z frontu Pareto (NSGA-II, przebieg na wzorach
#                6 i 7 z artykulu). Odpowiada na pytanie, czy mapa idealnie symetryczna wypada
#                lepiej niz losowa, gdy obie graja na najlepszych znanych parametrach swiata.
ZESTAWY = {
    'domyslny': {"minSpawnDistance": 12, "population_max": 60, "populationToCreateNewUnit": 700},
    'optimum': {"minSpawnDistance": 11, "population_max": 96, "populationToCreateNewUnit": 447},
}

_tryb_genow = sys.argv[1] if len(sys.argv) > 1 else 'domyslny'
if _tryb_genow not in ZESTAWY:
    raise SystemExit(f'Nieznany zestaw genow: {_tryb_genow}. Dostepne: {", ".join(ZESTAWY)}')
GENY = ZESTAWY[_tryb_genow]

TRYBY = [
    (4, 'symetria obrotowa 180 st.', 'wzorzec - warunki startowe identyczne z definicji'),
    (0, 'generator normalny', 'to, co optymalizuje NSGA-II'),
    (1, 'bogata strefa przy bazie 1', 'ZEPSUTA - asymetria zasobow'),
    (2, 'baza 2 zepchnieta w rog', 'ZEPSUTA - asymetria przestrzenna'),
    (3, 'bazy tuz obok siebie', 'ZEPSUTA - brak fazy rozwoju'),
]

PLIK_WYNIKOW = ('mapy_kontrolne_wyniki.json' if _tryb_genow == 'domyslny'
                else f'mapy_kontrolne_{_tryb_genow}_wyniki.json')


def main():
    meczow = len(TRYBY) * pf.MECZOW_NA_CHROMOSOM
    print('=' * 78)
    print(f'WERYFIKACJA NA MAPACH KONTROLNYCH')
    print(f'{len(TRYBY)} tryby x {pf.MECZOW_NA_CHROMOSOM} meczow = {meczow} meczow')
    print('=' * 78)
    print(f'  zestaw genow: {_tryb_genow} -> spawn={GENY["minSpawnDistance"]}, '
          f'popMax={GENY["population_max"]}, unitCost={GENY["populationToCreateNewUnit"]}')
    print(f'  wynik zapisze do: {PLIK_WYNIKOW}')
    print()
    for tryb, nazwa, opis in TRYBY:
        print(f'  [{tryb}] {nazwa:30} {opis}')
    print()

    przepisy = [dict(GENY, mapMode=tryb) for tryb, _, _ in TRYBY]

    with open(pf.INPUT_FILE, 'w') as f:
        json.dump({"recipes": przepisy, "pairedFirstMove": False}, f, indent=4)
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
    if len(wyniki) != len(TRYBY):
        raise SystemExit(f'Unity zwrocilo {len(wyniki)} wynikow zamiast {len(TRYBY)}.')

    with open(PLIK_WYNIKOW, 'w', encoding='utf-8') as f:
        json.dump({'tryby': [t[1] for t in TRYBY], 'zestaw_genow': _tryb_genow,
                   'geny': GENY, 'wyniki': wyniki}, f, indent=2)

    print()
    print('=' * 104)
    print(f"{'tryb mapy':32}{'teryt%':>8}{'growth%':>9}{'mil%':>7}{'reconq%':>9}"
          f"{'peaks%':>8}{'conq%':>7}{'BALANS':>9}{'DYNAM.':>9}")
    print('=' * 104)

    oceny = []
    for (tryb, nazwa, _), m in zip(TRYBY, wyniki):
        b, d = pf.score(m)
        oceny.append((tryb, nazwa, b, d))
        print(f'{nazwa:32}{m["avgTerritorialImbalance"] * 100:8.1f}{m["avgGrowthImbalance"]:9.1f}'
              f'{m["avgMilitaryImbalance"]:7.1f}{m["reconqueringRate"]:9.1f}'
              f'{m["peakAverage"]:8.1f}{m["conqueringRate"]:7.1f}{b:9.4f}{d:9.4f}')
    print('=' * 104)

    print()
    print('=' * 78)
    print('CZY SYSTEM ROZROZNIA MAPY? (kryterium: wzorzec > losowa > zepsute)')
    print('=' * 78)

    bal = {t: b for t, _, b, _ in oceny}
    wzorzec, losowa = bal[4], bal[0]
    zepsute = [bal[t] for t in (1, 2, 3)]
    najlepsza_zepsuta = max(zepsute)

    print(f'  balans mapy wzorcowej (symetria): {wzorzec:.4f}')
    print(f'  balans mapy losowej:              {losowa:.4f}')
    print(f'  najlepsza z map zepsutych:        {najlepsza_zepsuta:.4f}')
    print()

    ok1 = wzorzec >= losowa
    ok2 = losowa > najlepsza_zepsuta
    print(f'  wzorzec >= losowa:        {"TAK" if ok1 else "NIE"}')
    print(f'  losowa > najlepsza zepsuta: {"TAK" if ok2 else "NIE"}')
    print()
    if ok1 and ok2:
        print('  WYNIK: funkcja przystosowania poprawnie rozroznia jakosc map.')
    else:
        print('  WYNIK: UWAGA - kolejnosc niezgodna z oczekiwaniem.')
        print('  Metryki nie wykrywaja tego, co deklaruja. Nalezy to poprawic przed NSGA-II.')

    print()
    print(f'Surowe dane zapisano do {PLIK_WYNIKOW}')


if __name__ == '__main__':
    main()
