import os
import json
import subprocess
import numpy as np
import skfuzzy as fuzz
from skfuzzy import control as ctrl

# ============================================================
# 1. PEŁNA KONFIGURACJA LOGIKI ROZMYTEJ (MAPPING 1:1 Z GDD)
# ============================================================

# Progi wyznaczone z badania pilotazowego: 50 konfiguracji Latin Hypercube x 60 meczow = 3000 meczow.
# Kolejno: kwantyl 25% / mediana / kwantyl 75% / wartosc maksymalna zmierzonego rozkladu.
# Kwantyle wyznaczaja granice zbiorow, a maksimum - punkt pelnego nasycenia zbioru WYSOKI.
# Nasycenie na kwantylu 75% powodowalo, ze 24% konfiguracji dostawalo identyczna ocene minimalna.
# Wszystkie liczby odtwarzalne z pliku pilotaz_wyniki.json (jeden przebieg, ta sama liczba meczow
# co w eksperymencie glownym). Przeliczone po wdrozeniu wzorow (6) i (7) z artykulu zrodlowego.
PROGI = {
    'term_imbalance':     (12.14, 14.25, 17.05, 23.33),
    'growth_imbalance':   (17.39, 19.45, 22.95, 32.50),
    'military_imbalance': (16.21, 19.48, 25.10, 31.05),
    'reconq_rate':        (12.34, 18.39, 27.39, 34.30),
    'lead_rate':          (2.10, 2.62, 3.01, 4.91),
    # 'peaks' zostawione jako zapis zmierzonego rozkladu; metryka jest diagnostyczna,
    # nie wchodzi do zadnego systemu wnioskowania
    'peaks':              (72.27, 81.13, 87.22, 101.30),
}

# Uniwersa rozwazan. Uwaga: po zmianie definicji stare pliki wynikow (pilotaz_wyniki_stara_metryka.json,
# mapy_kontrolne_wyniki.json, nsga2_front.json) maja reconqueringRate w starej skali 0-200 i beda
# przycinane przez RECONQ_MAX. Do analizy danych historycznych trzeba tymczasowo podniesc te stala.
RECONQ_MAX = 60               # "% pol zmieniajacych wlasciciela na 100 tur"; stary pilotaz dawal 5,7-35,5
PEAKS_MAX = 200               # amplituda wahniecia przewagi ma zakres 0-2, czyli 0-200%
LEAD_MAX = 20                 # zmiany prowadzenia na 100 tur; zmierzone maksimum 4,3

# WEJŚCIA DLA KRYTERIUM BALANSU
term_imbalance = ctrl.Antecedent(np.arange(0, 101, 1), 'term_imbalance')
growth_imbalance = ctrl.Antecedent(np.arange(0, 101, 1), 'growth_imbalance')
military_imbalance = ctrl.Antecedent(np.arange(0, 101, 1), 'military_imbalance')

# WEJŚCIA DLA KRYTERIUM DYNAMIZMU
# conqueringRate NIE jest juz wejsciem - pelni role bramki poprawnosci, jak gameLength
lead_rate = ctrl.Antecedent(np.arange(0, LEAD_MAX + 0.01, 0.01), 'lead_rate')
reconq_rate = ctrl.Antecedent(np.arange(0, RECONQ_MAX + 1, 1), 'reconq_rate')

# WYJŚCIA SYSTEMU
balance = ctrl.Consequent(np.arange(0, 1.01, 0.01), 'balance')
dynamism = ctrl.Consequent(np.arange(0, 1.01, 0.01), 'dynamism')


def threestate(variable, name, universe_max):
    q25, med, q75, maks = PROGI[name]
    variable['low'] = fuzz.trimf(variable.universe, [0, 0, med])
    variable['medium'] = fuzz.trimf(variable.universe, [q25, med, q75])
    variable['high'] = fuzz.trapmf(variable.universe, [med, maks, universe_max, universe_max])


# --- Metryki balansu --
threestate(term_imbalance, 'term_imbalance', 100)
threestate(growth_imbalance, 'growth_imbalance', 100)
threestate(military_imbalance, 'military_imbalance', 100)

# --- Metryki dynamizmu ---
threestate(reconq_rate, 'reconq_rate', RECONQ_MAX)

# Zmiany prowadzenia (na 100 tur) maja dwa stany
_ql25, _qlmed, _ql75, _qlmaks = PROGI['lead_rate']
lead_rate['low'] = fuzz.trimf(lead_rate.universe, [0, 0, _ql75])
lead_rate['high'] = fuzz.trapmf(lead_rate.universe, [_ql25, _qlmaks, LEAD_MAX, LEAD_MAX])

# --- Definicja Wyjść Oceny Grywalności ---
for out in [balance, dynamism]:
    out['low'] = fuzz.trimf(out.universe, [0, 0, 0.4])
    out['medium'] = fuzz.trimf(out.universe, [0.3, 0.5, 0.7])
    out['high'] = fuzz.trimf(out.universe, [0.6, 1.0, 1.0])

# ============================================================
# 2. IMPLEMENTACJA TRÓJWYMIAROWYCH BAZ REGUŁ DECYZYJNYCH
# ============================================================

LVL = {'L': 'low', 'M': 'medium', 'H': 'high'}

# Tabela decyzyjna BALANSU: (Territorial, Growth, Military) -> ocena. Komplet 27 kombinacji.
BALANCE_TABLE = {
    ('L', 'L', 'L'): 'high',
    ('L', 'M', 'L'): 'high',
    ('L', 'H', 'L'): 'medium',
    ('M', 'L', 'L'): 'medium',
    ('M', 'M', 'L'): 'medium',
    ('M', 'H', 'L'): 'medium',
    ('H', 'L', 'L'): 'medium',
    ('H', 'M', 'L'): 'medium',
    ('H', 'H', 'L'): 'low',

    ('L', 'L', 'M'): 'medium',
    ('L', 'M', 'M'): 'medium',
    ('L', 'H', 'M'): 'medium',
    ('M', 'L', 'M'): 'medium',
    ('M', 'M', 'M'): 'medium',
    ('M', 'H', 'M'): 'low',
    ('H', 'L', 'M'): 'medium',
    ('H', 'M', 'M'): 'low',
    ('H', 'H', 'M'): 'low',

    # Reguła nadrzędna: potężna dysproporcja wojskowa zawsze niszczy balans
    ('L', 'L', 'H'): 'low',
    ('L', 'M', 'H'): 'low',
    ('L', 'H', 'H'): 'low',
    ('M', 'L', 'H'): 'low',
    ('M', 'M', 'H'): 'low',
    ('M', 'H', 'H'): 'low',
    ('H', 'L', 'H'): 'low',
    ('H', 'M', 'H'): 'low',
    ('H', 'H', 'H'): 'low',
}

# Tabela decyzyjna DYNAMIZMU: (Zmiany prowadzenia, Reconquering) -> ocena. Komplet 6 kombinacji.
# Peak Differences NIE jest juz wejsciem - patrz komentarz przy PEAKS_DIAGNOSTYCZNE nizej.
DYNAMISM_TABLE = {
    ('L', 'L'): 'low',       # front stoi i prowadzenie nie zmienia rak - mecz bez tresci
    ('L', 'M'): 'medium',    # front sie przesuwa, ale przewaga nie drga
    ('L', 'H'): 'medium',    # intensywna wymiana pol bez odwrocen prowadzenia
    ('H', 'L'): 'medium',    # prowadzenie sie odwraca, ale linia frontu stoi
    ('H', 'M'): 'high',      # odwrocenia losow i ruchomy front
    ('H', 'H'): 'high',      # wzorcowy dynamizm: ciagla wymiana pol i zmiany prowadzenia
}

# Punkty kulminacyjne sa nadal liczone przez Unity i zapisywane w wynikach, ale NIE wchodza
# do systemu rozmytego. Powod jest zmierzony: po wdrozeniu wzoru (7) z artykulu metryka koreluje
# +0,930 z nierownowaga terytorialna i ujemnie z tym, co miala mierzyc (-0,304 z odbijaniem,
# -0,399 z bitwami polowymi). Trzymanie jej w dynamizmie oznaczaloby albo regule w kierunku
# sprzecznym z pomiarem, albo mierzenie balansu po raz drugi. Szczegoly: WSKAZOWKI rozdz. 4.3.
PEAKS_DIAGNOSTYCZNE = True


def _build_rules(table, antecedents, consequent, expected_count):
    if len(table) != expected_count:
        raise ValueError(f"Baza reguł niekompletna: {len(table)}/{expected_count} kombinacji.")
    rules = []
    for key, verdict in table.items():
        condition = antecedents[0][LVL[key[0]]]
        for ant, level in zip(antecedents[1:], key[1:]):
            condition = condition & ant[LVL[level]]
        rules.append(ctrl.Rule(condition, consequent[verdict]))
    return rules

balance_rules = _build_rules(BALANCE_TABLE, [term_imbalance, growth_imbalance, military_imbalance], balance, 27)
dynamism_rules = _build_rules(DYNAMISM_TABLE, [lead_rate, reconq_rate], dynamism, 6)

balance_ctrl = ctrl.ControlSystem(balance_rules)
dynamism_ctrl = ctrl.ControlSystem(dynamism_rules)

balance_sim = ctrl.ControlSystemSimulation(balance_ctrl)
dynamism_sim = ctrl.ControlSystemSimulation(dynamism_ctrl)

# ============================================================
# 3. URUCHOMIENIE UNITY - CALA POPULACJA W JEDNYM STARCIE
# ============================================================

UNITY_EXE = r"D:\Unity\6000.2.14f1\Editor\Unity.com"
PROJECT_PATH = os.getcwd()
INPUT_FILE = 'map_input.json'
OUTPUT_FILE = 'metrics_output.json'
LOG_FILE = 'unity_batch_log.txt'

MECZOW_NA_CHROMOSOM = 60      # musi odpowiadac batchSimulationCount w scenie Unity
SEKUND_NA_MECZ = 20           # zapas czasu przy wyznaczaniu timeoutu
NARZUT_STARTU_S = 300         # ladowanie edytora i import assetow
MIN_GAME_LENGTH_PCT = 15.0    # ponizej tej dlugosci mecz uznajemy za rozstrzygniety kula snieznej
MIN_CONQUERING_PCT = 60.0     # ponizej tego mapa nie zostala zagospodarowana - wynik odrzucamy

# Unity celowo NIE dostaje flagi -quit: BatchRunner.RunSim jedynie wlacza tryb gry i natychmiast
# wraca, wiec -quit zamknalby edytor jeszcze przed startem symulacji. Proces konczy sie przez
# EditorApplication.Exit(0) po zapisaniu wynikow, a timeout ponizej jest zabezpieczeniem.
UNITY_CMD = [
    UNITY_EXE,
    "-batchmode",
    "-nographics",
    "-projectPath", PROJECT_PATH,
    "-logFile", LOG_FILE,
    "-executeMethod", "BatchRunner.RunSim",
]


def evaluate_population(recipes, timeout_s=None):
    """Ocenia liste chromosomow w JEDNYM uruchomieniu Unity. Zwraca liste slownikow metryk."""
    if not recipes:
        return []

    with open(INPUT_FILE, 'w') as f:
        json.dump({"recipes": recipes}, f, indent=4)

    # Kasujemy poprzedni wynik. Bez tego awaria Unity zostawilaby stary plik,
    # ktory zostalby po cichu odczytany jako poprawny pomiar tej generacji.
    if os.path.exists(OUTPUT_FILE):
        os.remove(OUTPUT_FILE)

    if timeout_s is None:
        timeout_s = NARZUT_STARTU_S + SEKUND_NA_MECZ * MECZOW_NA_CHROMOSOM * len(recipes)

    print(f"--- [PYTHON] Unity: {len(recipes)} chromosomow x {MECZOW_NA_CHROMOSOM} meczow, "
          f"limit czasu {timeout_s} s. Prosze czekac.")

    try:
        subprocess.run(UNITY_CMD, check=True, timeout=timeout_s)
    except subprocess.TimeoutExpired:
        raise RuntimeError(f"Unity nie zakonczylo pracy w {timeout_s} s. Sprawdz {LOG_FILE}.")
    except subprocess.CalledProcessError as e:
        raise RuntimeError(f"Unity zakonczylo sie kodem {e.returncode}. Sprawdz {LOG_FILE}.")

    if not os.path.exists(OUTPUT_FILE):
        raise RuntimeError(f"Unity nie zapisalo {OUTPUT_FILE}. Sprawdz {LOG_FILE}.")

    with open(OUTPUT_FILE, 'r') as f:
        data = json.load(f)

    results = data.get("results")
    if results is None:
        raise RuntimeError(f"{OUTPUT_FILE} nie zawiera klucza 'results'.")
    if len(results) != len(recipes):
        raise RuntimeError(f"Unity zwrocilo {len(results)} wynikow zamiast {len(recipes)}.")

    print(f"--- [PYTHON] Odebrano {len(results)} wynikow.")
    return results


def score(metrics):
    """Zamienia surowe metryki na pare ocen (balans, dynamizm) w skali 0-1."""
    # Bramki poprawnosci. Krotka gra = rozstrzygniecie kula snieznej.
    # Niski conqueringRate = mapa nie zostala zagospodarowana (gra urwana albo teren odciety).
    if metrics["gameLength"] < MIN_GAME_LENGTH_PCT:
        return 0.0, 0.0
    if metrics["conqueringRate"] < MIN_CONQUERING_PCT:
        return 0.0, 0.0

    # avgTerritorialImbalance przychodzi jako ulamek 0-1, pozostale metryki juz jako procenty
    balance_sim.input['term_imbalance'] = metrics["avgTerritorialImbalance"] * 100.0
    balance_sim.input['growth_imbalance'] = metrics["avgGrowthImbalance"]
    balance_sim.input['military_imbalance'] = metrics["avgMilitaryImbalance"]
    balance_sim.compute()

    dynamism_sim.input['lead_rate'] = min(metrics["leadChangeRate"], LEAD_MAX)
    dynamism_sim.input['reconq_rate'] = min(metrics["reconqueringRate"], RECONQ_MAX)
    dynamism_sim.compute()

    return balance_sim.output['balance'], dynamism_sim.output['dynamism']


# ============================================================
# 4. PRZYKLADOWE WYWOLANIE (miejsce na petle NSGA-II)
# ============================================================

if __name__ == '__main__':
    populacja = [
        {"minSpawnDistance": 12, "population_max": 65, "populationToCreateNewUnit": 700},
        {"minSpawnDistance": 8, "population_max": 40, "populationToCreateNewUnit": 500},
        {"minSpawnDistance": 16, "population_max": 90, "populationToCreateNewUnit": 900},
    ]

    wyniki = evaluate_population(populacja)

    print()
    print("=" * 104)
    print(f"{'#':>2} {'spawnDist':>10} {'popMax':>7} {'unitCost':>9} "
          f"{'teryt%':>8} {'growth%':>8} {'mil%':>7} {'reconq%':>8} {'peaks%':>7} "
          f"{'zm/100t':>8} {'bitwy':>7} {'conq%':>7} {'BALANS':>8} {'DYNAMIZM':>9}")
    print("=" * 104)
    for i, (przepis, m) in enumerate(zip(populacja, wyniki), 1):
        b, d = score(m)
        print(f"{i:>2} {przepis['minSpawnDistance']:>10} {przepis['population_max']:>7} "
              f"{przepis['populationToCreateNewUnit']:>9} "
              f"{m['avgTerritorialImbalance'] * 100:>8.1f} {m['avgGrowthImbalance']:>8.1f} "
              f"{m['avgMilitaryImbalance']:>7.1f} {m['reconqueringRate']:>8.1f} "
              f"{m['peakAverage']:>7.1f} {m['leadChangeRate']:>8.2f} {m['fieldBattles']:>7.1f} "
              f"{m['conqueringRate']:>7.1f} {b:>8.4f} {d:>9.4f}")
    print("=" * 104)
    print("conq% to juz tylko bramka poprawnosci (prog " + str(MIN_CONQUERING_PCT) + "%), nie wejscie systemu")
