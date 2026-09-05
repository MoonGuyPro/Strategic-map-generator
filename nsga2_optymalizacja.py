# -*- coding: utf-8 -*-
"""
Optymalizacja wielokryterialna NSGA-II — glowny eksperyment pracy.

Szuka zestawow parametrow generatora, ktore daja mapy jednoczesnie zbalansowane i dynamiczne.
Wynikiem nie jest jedna mapa ani jeden zestaw, tylko FRONT PARETO: zbior zestawow, ktorych
nie da sie poprawic pod jednym wzgledem bez pogorszenia drugiego.

Genotyp (3 zmienne calkowite):
    minSpawnDistance           8 - 18     dystans miedzy bazami
    population_max            20 - 100    zamoznosc swiata
    populationToCreateNewUnit 400 - 1000  koszt i sila oddzialu

Cele (oba maksymalizowane; pymoo minimalizuje, wiec podajemy wartosci ujemne):
    BALANS    z systemu rozmytego, 0-1
    DYNAMIZM  z systemu rozmytego, 0-1

Uruchomienie:
    python nsga2_optymalizacja.py test    - szybki przebieg kontrolny (kilkanascie minut)
    python nsga2_optymalizacja.py         - pelny przebieg

Wyniki:
    nsga2_postep.json    stan po kazdym pokoleniu (odporne na przerwanie)
    nsga2_front.json     koncowy front Pareto
    nsga2_front.csv      ten sam front w formie tabeli do pracy
"""
import json
import os
import sys
import time

import numpy as np
from pymoo.algorithms.moo.nsga2 import NSGA2
from pymoo.core.callback import Callback
from pymoo.core.problem import Problem
from pymoo.indicators.hv import HV
from pymoo.operators.crossover.sbx import SBX
from pymoo.operators.mutation.pm import PM
from pymoo.operators.repair.rounding import RoundingRepair
from pymoo.operators.sampling.rnd import IntegerRandomSampling
from pymoo.optimize import minimize

import pipeline_fuzzy as pf

# ============================================================
# KONFIGURACJA
# ============================================================

TRYB_TESTOWY = len(sys.argv) > 1 and sys.argv[1].lower() == 'test'

if TRYB_TESTOWY:
    POPULATION_SIZE = 8
    GENERATION_NUMBER = 3
else:
    POPULATION_SIZE = 20
    GENERATION_NUMBER = 25

SEED = 1

GENES = [
    ('minSpawnDistance', 8, 18),
    ('population_max', 20, 100),
    ('populationToCreateNewUnit', 400, 1000),
]

PLIK_POSTEPU = 'nsga2_postep.json'
PLIK_FRONTU = 'nsga2_front.json'
PLIK_CSV = 'nsga2_front.csv'

# Punkt odniesienia dla hiperobjetosci. Cele sa w [-1, 0], wiec (0, 0) jest zdominowany
# przez kazde rozwiazanie i hiperobjetosc rosnie wraz z jakoscia frontu.
PUNKT_ODNIESIENIA = np.array([0.0, 0.0])


def przepis_z_genow(klucz):
    return {nazwa: int(wartosc) for (nazwa, _, _), wartosc in zip(GENES, klucz)} | {'mapMode': 0}


class ProblemGeneratoraMap(Problem):
    """Ocena chromosomu = 60 meczow w Unity, cala populacja w JEDNYM uruchomieniu."""

    def __init__(self):
        super().__init__(
            n_var=len(GENES),
            n_obj=2,
            xl=np.array([lo for _, lo, _ in GENES]),
            xu=np.array([hi for _, _, hi in GENES]),
            vtype=int,
        )
        self.pamiec = {}          # klucz genow -> surowe metryki
        self.historia = []        # wszystkie ocenione chromosomy, do analizy w pracy
        self.pokolenie = 0

    def _evaluate(self, X, out, *args, **kwargs):
        klucze = [tuple(int(round(v)) for v in wiersz) for wiersz in X]

        do_oceny = {}
        for k in klucze:
            if k not in self.pamiec and k not in do_oceny:
                do_oceny[k] = przepis_z_genow(k)

        if do_oceny:
            self.pokolenie += 1
            print(f'\n--- [NSGA-II] pokolenie {self.pokolenie}: {len(do_oceny)} nowych chromosomow '
                  f'({len(klucze) - len(do_oceny)} z pamieci)')
            wyniki = pf.evaluate_population(list(do_oceny.values()))
            for k, metryki in zip(do_oceny.keys(), wyniki):
                self.pamiec[k] = metryki
                b, d = pf.score(metryki)
                self.historia.append({
                    'pokolenie': self.pokolenie,
                    'geny': przepis_z_genow(k),
                    'balans': b,
                    'dynamizm': d,
                    'metryki': metryki,
                })

        F = []
        for k in klucze:
            b, d = pf.score(self.pamiec[k])
            F.append([-b, -d])
        out['F'] = np.array(F)


class SaveProgress(Callback):
    """Po kazdym pokoleniu zapisuje stan, zeby przerwanie nie kosztowalo calego przebiegu."""

    def __init__(self, problem):
        super().__init__()
        self.problem = problem
        self.hv = HV(ref_point=PUNKT_ODNIESIENIA)
        self.start = time.time()

    def notify(self, algorithm):
        F = algorithm.pop.get('F')
        X = algorithm.pop.get('X')
        hiper = float(self.hv(F))
        minuty = (time.time() - self.start) / 60

        naj_bal = float(-F[:, 0].min())
        naj_dyn = float(-F[:, 1].min())
        print(f'--- [NSGA-II] po pokoleniu {algorithm.n_gen}: hiperobjetosc {hiper:.4f}, '
              f'najlepszy balans {naj_bal:.3f}, najlepszy dynamizm {naj_dyn:.3f}, '
              f'czas {minuty:.0f} min')

        stan = {
            'pokolenie': int(algorithm.n_gen),
            'hiperobjetosc': hiper,
            'minut_od_startu': minuty,
            'populacja': [
                {'geny': przepis_z_genow(tuple(int(round(v)) for v in x)),
                 'balans': float(-f[0]), 'dynamizm': float(-f[1])}
                for x, f in zip(X, F)
            ],
            'historia': self.problem.historia,
        }
        with open(PLIK_POSTEPU, 'w', encoding='utf-8') as f:
            json.dump(stan, f, indent=1)


def main():
    ocen_lacznie = POPULATION_SIZE * (GENERATION_NUMBER + 1)
    meczow = ocen_lacznie * pf.MECZOW_NA_CHROMOSOM
    godzin = meczow * 1.2 / 3600

    print('=' * 78)
    print('NSGA-II - OPTYMALIZACJA PARAMETROW GENERATORA MAP')
    if TRYB_TESTOWY:
        print('TRYB TESTOWY - sprawdzenie, czy caly lancuch dziala')
    print('=' * 78)
    print(f'  populacja:        {POPULATION_SIZE}')
    print(f'  pokolenia:        {GENERATION_NUMBER}')
    print(f'  meczow na ocene:  {pf.MECZOW_NA_CHROMOSOM}')
    print(f'  ocen maksymalnie: {ocen_lacznie}  (mniej, jesli powtorza sie genotypy)')
    print(f'  meczow lacznie:   ~{meczow}')
    print(f'  szacowany czas:   ~{godzin:.1f} h')
    print()
    for nazwa, lo, hi in GENES:
        print(f'  gen {nazwa:26} zakres {lo} - {hi}')
    print('=' * 78)

    issue = ProblemGeneratoraMap()

    algorithm = NSGA2(
        pop_size=POPULATION_SIZE,
        sampling=IntegerRandomSampling(),
        crossover=SBX(prob=0.9, eta=15, vtype=float, repair=RoundingRepair()),
        mutation=PM(prob=1.0 / len(GENES), eta=20, vtype=float, repair=RoundingRepair()),
        eliminate_duplicates=True,
    )

    summary = minimize(
        issue,
        algorithm,
        termination=('n_gen', GENERATION_NUMBER),
        seed=SEED,
        callback=SaveProgress(issue),
        verbose=False,
        save_history=False,
    )

    X = np.atleast_2d(summary.X)
    F = np.atleast_2d(summary.F)
    front = []
    for x, f in zip(X, F):
        klucz = tuple(int(round(v)) for v in x)
        front.append({
            'geny': przepis_z_genow(klucz),
            'balans': float(-f[0]),
            'dynamizm': float(-f[1]),
            'metryki': issue.pamiec[klucz],
        })
    front.sort(key=lambda r: -r['balans'])

    with open(PLIK_FRONTU, 'w', encoding='utf-8') as f:
        json.dump({'front': front, 'ocen_wykonanych': len(issue.pamiec),
                   'historia': issue.historia}, f, indent=1)

    with open(PLIK_CSV, 'w', encoding='utf-8', newline='') as f:
        f.write('minSpawnDistance;population_max;populationToCreateNewUnit;balans;dynamizm;'
                'teryt%;growth%;mil%;reconq%;peaks%;zmiany/100tur;conq%;dlugosc%\n')
        for r in front:
            g, m = r['geny'], r['metryki']
            f.write(f"{g['minSpawnDistance']};{g['population_max']};{g['populationToCreateNewUnit']};"
                    f"{r['balans']:.4f};{r['dynamizm']:.4f};"
                    f"{m['avgTerritorialImbalance'] * 100:.2f};{m['avgGrowthImbalance']:.2f};"
                    f"{m['avgMilitaryImbalance']:.2f};{m['reconqueringRate']:.2f};"
                    f"{m['peakAverage']:.2f};{m['leadChangeRate']:.2f};"
                    f"{m['conqueringRate']:.2f};{m['gameLength']:.2f}\n")

    print()
    print('=' * 96)
    print(f'FRONT PARETO — {len(front)} rozwiazan niezdominowanych '
          f'(ocenionych chromosomow: {len(issue.pamiec)})')
    print('=' * 96)
    print(f"{'#':>3} {'spawnDist':>10}{'popMax':>8}{'unitCost':>10}"
          f"{'BALANS':>9}{'DYNAMIZM':>10}{'teryt%':>8}{'reconq%':>9}")
    print('-' * 96)
    for i, r in enumerate(front, 1):
        g, m = r['geny'], r['metryki']
        print(f"{i:>3} {g['minSpawnDistance']:>10}{g['population_max']:>8}"
              f"{g['populationToCreateNewUnit']:>10}{r['balans']:>9.4f}{r['dynamizm']:>10.4f}"
              f"{m['avgTerritorialImbalance'] * 100:>8.1f}{m['reconqueringRate']:>9.1f}")
    print('=' * 96)
    print()
    print(f'Zapisano: {PLIK_FRONTU}, {PLIK_CSV}, {PLIK_POSTEPU}')
    print()
    print('Front posortowany malejaco wedlug balansu. Pierwszy wiersz to zestaw najbardziej')
    print('zbalansowany, ostatni - najbardziej dynamiczny. Wybor konkretnego zestawu nalezy')
    print('do projektanta i to wlasnie jest wynik metody wielokryterialnej.')


if __name__ == '__main__':
    main()
