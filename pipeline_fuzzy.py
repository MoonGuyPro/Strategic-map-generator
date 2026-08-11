import os
import json
import subprocess
import numpy as np
import skfuzzy as fuzz
from skfuzzy import control as ctrl

# ============================================================
# 1. PEŁNA KONFIGURACJA LOGIKI ROZMYTEJ (MAPPING 1:1 Z GDD)
# ============================================================

# Progi wyznaczone z dwoch niezaleznych przebiegow pilotazowych (2 x 50 chromosomow x 20 meczow = 2000 meczow).
# Dla kazdej zmiennej: kwantyl 25% / mediana / kwantyl 75% zmierzonego rozkladu.
# Dzieki temu kazdy zbior lingwistyczny obejmuje mniej wiecej jedna trzecia realnych map.
PROGI = {
    'term_imbalance':     (11.9, 14.6, 17.1),
    'growth_imbalance':   (17.3, 20.5, 23.4),
    'military_imbalance': (16.0, 20.7, 24.5),
    'reconq_rate':        (41.4, 66.5, 100.9),
    'lead_rate':          (1.91, 2.66, 3.29),
    'peaks':              (52.2, 61.7, 68.3),
}

RECONQ_MAX = 200              # reconquering przekracza 100% (zmierzone maksimum 146)
LEAD_MAX = 20                 # zmiany prowadzenia na 100 tur; zmierzone maksimum 4,3

# WEJŚCIA DLA KRYTERIUM BALANSU
term_imbalance = ctrl.Antecedent(np.arange(0, 101, 1), 'term_imbalance')
growth_imbalance = ctrl.Antecedent(np.arange(0, 101, 1), 'growth_imbalance')
military_imbalance = ctrl.Antecedent(np.arange(0, 101, 1), 'military_imbalance')

# WEJŚCIA DLA KRYTERIUM DYNAMIZMU
# conqueringRate NIE jest juz wejsciem - pelni role bramki poprawnosci, jak gameLength
lead_rate = ctrl.Antecedent(np.arange(0, LEAD_MAX + 0.01, 0.01), 'lead_rate')
reconq_rate = ctrl.Antecedent(np.arange(0, RECONQ_MAX + 1, 1), 'reconq_rate')
peaks = ctrl.Antecedent(np.arange(0, 101, 1), 'peaks')

# WYJŚCIA SYSTEMU
balance = ctrl.Consequent(np.arange(0, 1.01, 0.01), 'balance')
dynamism = ctrl.Consequent(np.arange(0, 1.01, 0.01), 'dynamism')


def trojstanowa(zmienna, nazwa, gora):
    """Zbiory LOW/MEDIUM/HIGH zakotwiczone na kwantylach 25 / 50 / 75."""
    q25, med, q75 = PROGI[nazwa]
    zmienna['low'] = fuzz.trimf(zmienna.universe, [0, 0, med])
    zmienna['medium'] = fuzz.trimf(zmienna.universe, [q25, med, q75])
    zmienna['high'] = fuzz.trapmf(zmienna.universe, [med, q75, gora, gora])


# --- Metryki balansu: kazda kalibrowana na wlasnym rozkladzie ---
trojstanowa(term_imbalance, 'term_imbalance', 100)
trojstanowa(growth_imbalance, 'growth_imbalance', 100)
trojstanowa(military_imbalance, 'military_imbalance', 100)

# --- Metryki dynamizmu ---
trojstanowa(reconq_rate, 'reconq_rate', RECONQ_MAX)

# Peaks: zbior MEDIUM oznacza najlepsza dramaturgie (GDD 7.5.C - umiarkowanie wysoka wartosc).
# Zbior HIGH to jednostronna dominacja, LOW to mecz plaski.
trojstanowa(peaks, 'peaks', 100)

# Zmiany prowadzenia (na 100 tur) maja dwa stany: im wiecej odwrocen losow, tym dynamiczniej
_ql25, _qlmed, _ql75 = PROGI['lead_rate']
lead_rate['low'] = fuzz.trimf(lead_rate.universe, [0, 0, _ql75])
lead_rate['high'] = fuzz.trapmf(lead_rate.universe, [_ql25, _ql75, LEAD_MAX, LEAD_MAX])

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

# Tabela decyzyjna DYNAMIZMU: (Zmiany prowadzenia, Reconquering, Peaks) -> ocena. Komplet 18 kombinacji.
# Peaks SREDNI jest najlepszy: wysoki oznacza jednostronna dominacje, niski mecz bez zwrotow akcji.
DYNAMISM_TABLE = {
    ('L', 'L', 'L'): 'low',
    ('L', 'L', 'M'): 'low',
    ('L', 'L', 'H'): 'low',
    ('L', 'M', 'L'): 'medium',
    ('L', 'M', 'M'): 'medium',
    ('L', 'M', 'H'): 'low',
    ('L', 'H', 'L'): 'medium',
    ('L', 'H', 'M'): 'medium',
    ('L', 'H', 'H'): 'medium',

    ('H', 'L', 'L'): 'medium',
    ('H', 'L', 'M'): 'medium',
    ('H', 'L', 'H'): 'medium',
    ('H', 'M', 'L'): 'medium',
    ('H', 'M', 'M'): 'high',
    ('H', 'M', 'H'): 'medium',
    ('H', 'H', 'L'): 'high',
    ('H', 'H', 'M'): 'high',
    ('H', 'H', 'H'): 'medium',
}


def _build_rules(table, ant_a, ant_b, ant_c, consequent, expected_count):
    """Zamienia tabelę decyzyjną na listę reguł rozmytych i pilnuje jej kompletności."""
    if len(table) != expected_count:
        raise ValueError(f"Baza reguł niekompletna: {len(table)}/{expected_count} kombinacji.")
    return [
        ctrl.Rule(ant_a[LVL[a]] & ant_b[LVL[b]] & ant_c[LVL[c]], consequent[verdict])
        for (a, b, c), verdict in table.items()
    ]


# lead_rate ma dwa stany, reconq i peaks po trzy: 2 * 3 * 3 = 18 kombinacji
balance_rules = _build_rules(BALANCE_TABLE, term_imbalance, growth_imbalance, military_imbalance, balance, 27)
dynamism_rules = _build_rules(DYNAMISM_TABLE, lead_rate, reconq_rate, peaks, dynamism, 18)

# Kompilacja kontrolerów wnioskowania
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

MECZOW_NA_CHROMOSOM = 20      # musi odpowiadac batchSimulationCount w scenie Unity
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
    dynamism_sim.input['peaks'] = metrics["peakAverage"]
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
