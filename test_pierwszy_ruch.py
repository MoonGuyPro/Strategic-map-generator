# -*- coding: utf-8 -*-
"""
Pomiar przewagi pierwszego ruchu (First-Player Advantage).

Kazda mapa rozgrywana jest DWA RAZY - raz zaczyna bot 1, raz bot 2. Poniewaz teren,
rozklad populacji i pozycje baz sa w obu przebiegach identyczne, roznica w wynikach
pochodzi wylacznie z kolejnosci ruchu (plus szum losowych walk).

Jest to porownanie parowane: eliminuje zmiennosc map, ktora w normalnym pomiarze
jest 2,6 raza wieksza niz badany efekt.

Uruchomienie:  python test_pierwszy_ruch.py
"""
import json
import os
import glob
import re
import statistics as st
import time

import pipeline_fuzzy as pf

# Jedna konfiguracja srodkowa, powielona tak, by uzyskac zadana liczbe meczow.
# Unity rozgrywa MECZOW_NA_CHROMOSOM meczow na kazdy przepis, wiec 10 x 20 = 200 meczow = 100 par.
PRZEPIS = {"minSpawnDistance": 12, "population_max": 60, "populationToCreateNewUnit": 700}
POWTORZEN = 10

METRYKI = [
    ('teryt', r'Territorial Imbalance[^:]*:\s*([\d,\.]+)%', 'Territorial imbalance [%]'),
    ('growth', r'Growth Imbalance[^:]*:\s*([\d,\.]+)%', 'Growth imbalance [%]'),
    ('mil', r'Military Imbalance[^:]*:\s*([\d,\.]+)%', 'Military imbalance [%]'),
    # bez wymaganego znaku % na koncu - po wdrozeniu wzoru (6) raport podaje '% pol na 100 tur'
    ('reconq', r'Reconquering Rate[^:]*:\s*([\d,\.]+)', 'Reconquering rate [% pol/100 tur]'),
    ('conq', r'Conquering Rate[^:]*:\s*([\d,\.]+)%', 'Conquering rate [%]'),
    ('lead', r'Lead Changes na 100 tur:\s*([\d,\.]+)', 'Zmiany prowadzenia /100 tur'),
    ('bitwy', r'Field Battles \(Bitwy polowe token vs token\):\s*(\d+)', 'Bitwy polowe [szt]'),
    ('tury', r'Liczba rozegranych tur:\s*(\d+)', 'Liczba tur'),
]


def wczytaj_raport(sciezka):
    s = open(sciezka, encoding='utf-8', errors='replace').read()
    rek = {}
    for klucz, wzor, _ in METRYKI:
        m = re.search(wzor, s)
        if not m:
            return None
        rek[klucz] = float(m.group(1).replace(',', '.'))
    m = re.search(r'Pierwszy ruch wykonal: BOT (\d+)', s)
    if not m:
        return None
    rek['zaczynal'] = int(m.group(1))
    rek['remis'] = 'REMIS' in s
    z = re.search(r'ZWYCIEZCA: BOT (\d+)', s)
    rek['zwyciezca'] = int(z.group(1)) if z else 0
    return rek


def main():
    print('=' * 78)
    print(f'POMIAR PRZEWAGI PIERWSZEGO RUCHU')
    print(f'{POWTORZEN} x {pf.MECZOW_NA_CHROMOSOM} = {POWTORZEN * pf.MECZOW_NA_CHROMOSOM} meczow '
          f'= {POWTORZEN * pf.MECZOW_NA_CHROMOSOM // 2} par')
    print('Kazda mapa rozgrywana dwa razy, raz z kazda kolejnoscia.')
    print('=' * 78)
    print()

    with open(pf.INPUT_FILE, 'w') as f:
        json.dump({"recipes": [dict(PRZEPIS) for _ in range(POWTORZEN)],
                   "pairedFirstMove": True}, f, indent=4)

    if os.path.exists(pf.OUTPUT_FILE):
        os.remove(pf.OUTPUT_FILE)

    znacznik = time.time() - 1
    meczow = POWTORZEN * pf.MECZOW_NA_CHROMOSOM
    limit = pf.NARZUT_STARTU_S + 5 * meczow

    print(f'--- [PYTHON] Unity: {meczow} meczow, limit {limit} s. Prosze czekac.')
    import subprocess
    try:
        subprocess.run(pf.UNITY_CMD, check=True, timeout=limit)
    except subprocess.TimeoutExpired:
        raise SystemExit(f'Unity przekroczylo {limit} s. Sprawdz {pf.LOG_FILE}.')
    except subprocess.CalledProcessError as e:
        raise SystemExit(f'Unity zakonczylo sie kodem {e.returncode}. Sprawdz {pf.LOG_FILE}.')

    pliki = [f for f in glob.glob('Wyniki_Batch/*.txt') if os.path.getmtime(f) > znacznik]
    pliki.sort(key=os.path.getmtime)
    rek = [r for r in (wczytaj_raport(f) for f in pliki) if r]
    print(f'--- [PYTHON] Wczytano {len(rek)} raportow z tego przebiegu.')

    pary = []
    for i in range(0, len(rek) - 1, 2):
        a, b = rek[i], rek[i + 1]
        if a['zaczynal'] == b['zaczynal']:
            continue                       # niepoprawna para - pomijamy
        pierwszy = a if a['zaczynal'] == 1 else b
        drugi = b if a['zaczynal'] == 1 else a
        pary.append((pierwszy, drugi))
    print(f'--- [PYTHON] Poprawnych par: {len(pary)}\n')

    if not pary:
        raise SystemExit('Brak poprawnych par - sprawdz, czy tryb parowany sie wlaczyl.')

    print('=' * 92)
    print('CZY STATYSTYKI MAPY ZALEZA OD TEGO, KTO ZACZYNA?')
    print('(ta sama mapa, dwa przebiegi; roznica = wartosc gdy zaczynal bot 1 minus gdy bot 2)')
    print('=' * 92)
    print(f"{'metryka':30}{'zaczynal bot 1':>16}{'zaczynal bot 2':>16}{'roznica':>12}{'sigma':>9}  wniosek")
    print('-' * 92)
    for klucz, _, etykieta in METRYKI:
        a = [p[0][klucz] for p in pary]
        b = [p[1][klucz] for p in pary]
        roznice = [x - y for x, y in zip(a, b)]
        sr = st.mean(roznice)
        se = st.pstdev(roznice) / len(roznice) ** 0.5
        sig = abs(sr) / se if se else 0
        wn = 'ISTOTNA' if sig >= 2 else 'w granicach szumu'
        print(f'{etykieta:30}{st.mean(a):16.2f}{st.mean(b):16.2f}{sr:12.2f}{sig:9.1f}  {wn}')

    print()
    print('=' * 78)
    print('CZY BOT ZACZYNAJACY WYGRYWA CZESCIEJ?')
    print('=' * 78)
    wszystkie = [r for p in pary for r in p]
    rozstrzygniete = [r for r in wszystkie if not r['remis']]
    wygrane_zaczynajacego = sum(1 for r in rozstrzygniete if r['zwyciezca'] == r['zaczynal'])
    n = len(rozstrzygniete)
    print(f'  meczow rozstrzygnietych: {n} (remisow: {len(wszystkie) - n})')
    if n:
        p = wygrane_zaczynajacego / n
        se = (0.25 / n) ** 0.5
        print(f'  wygral bot zaczynajacy: {wygrane_zaczynajacego} ({100 * p:.1f}%)')
        print(f'  odchylenie od 50%: {abs(p - 0.5) / se:.1f} sigma '
              f'({"ISTOTNE" if abs(p - 0.5) / se >= 2 else "w granicach szumu"})')

    print()
    print('=' * 78)
    print('KONTROLA: czy bazy sa juz zbilansowane po poprawce?')
    print('=' * 78)
    b1 = sum(1 for r in rozstrzygniete if r['zwyciezca'] == 1)
    if n:
        p1 = b1 / n
        se = (0.25 / n) ** 0.5
        print(f'  wygral bot 1 (baza 1): {b1} ({100 * p1:.1f}%)')
        print(f'  odchylenie od 50%: {abs(p1 - 0.5) / se:.1f} sigma '
              f'({"nadal ISTOTNE" if abs(p1 - 0.5) / se >= 2 else "brak przewagi pozycyjnej"})')


if __name__ == '__main__':
    main()
