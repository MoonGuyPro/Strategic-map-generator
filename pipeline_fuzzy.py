import os
import json
import subprocess
import numpy as np
import skfuzzy as fuzz
from skfuzzy import control as ctrl

# ============================================================
# 1. PEŁNA KONFIGURACJA LOGIKI ROZMYTEJ (MAPPING 1:1 Z GDD)
# ============================================================

# WEJŚCIA DLA KRYTERIUM BALANSU
term_imbalance = ctrl.Antecedent(np.arange(0, 101, 1), 'term_imbalance')
growth_imbalance = ctrl.Antecedent(np.arange(0, 101, 1), 'growth_imbalance')
military_imbalance = ctrl.Antecedent(np.arange(0, 101, 1), 'military_imbalance')

# WEJŚCIA DLA KRYTERIUM DYNAMIZMU
conq_rate = ctrl.Antecedent(np.arange(0, 101, 1), 'conq_rate')
reconq_rate = ctrl.Antecedent(np.arange(0, 101, 1), 'reconq_rate')
peaks = ctrl.Antecedent(np.arange(0, 101, 1), 'peaks')

# WYJŚCIA SYSTEMU
balance = ctrl.Consequent(np.arange(0, 1.01, 0.01), 'balance')
dynamism = ctrl.Consequent(np.arange(0, 1.01, 0.01), 'dynamism')

# --- Definicja Funkcji Przynależności dla Metryk Balansu ---
for var in [term_imbalance, growth_imbalance, military_imbalance]:
    var['low'] = fuzz.trimf(var.universe, [0, 0, 30])
    var['medium'] = fuzz.trimf(var.universe, [15, 45, 75])
    var['high'] = fuzz.trapmf(var.universe, [60, 80, 100, 100])

# --- Definicja Funkcji Przynależności dla Metryk Dynamizmu ---
conq_rate['low'] = fuzz.trimf(conq_rate.universe, [0, 0, 55])
conq_rate['high'] = fuzz.trapmf(conq_rate.universe, [45, 75, 100, 100])

peaks['low'] = fuzz.trimf(peaks.universe, [0, 0, 55])
peaks['high'] = fuzz.trapmf(peaks.universe, [45, 75, 100, 100])

# Specjalne mapowanie dla Reconquering Rate (zgodnie z sekcją 8.1 Twojego GDD)
reconq_rate['low'] = fuzz.trimf(reconq_rate.universe, [0, 0, 15])
reconq_rate['medium'] = fuzz.trimf(reconq_rate.universe, [10, 30, 50])
reconq_rate['high'] = fuzz.trapmf(reconq_rate.universe, [40, 65, 100, 100])

# --- Definicja Wyjść Oceny Grywalności ---
for out in [balance, dynamism]:
    out['low'] = fuzz.trimf(out.universe, [0, 0, 0.4])
    out['medium'] = fuzz.trimf(out.universe, [0.3, 0.5, 0.7])
    out['high'] = fuzz.trimf(out.universe, [0.6, 1.0, 1.0])

# ============================================================
# 2. IMPLEMENTACJA TRÓJWYMIAROWYCH BAZ REGUŁ DECYZYJNYCH
# ============================================================

balance_rules = [
    # Stan idealny - wszystko równe
    ctrl.Rule(term_imbalance['low'] & growth_imbalance['low'] & military_imbalance['low'], balance['high']),

    # Stan lekko zachwiany (jeden element ucieka w medium) -> ocena wysoka/średnia
    ctrl.Rule(term_imbalance['low'] & growth_imbalance['low'] & military_imbalance['medium'], balance['medium']),
    ctrl.Rule(term_imbalance['low'] & growth_imbalance['medium'] & military_imbalance['low'], balance['high']),
    # terytorium ważniejsze niż eko!
    ctrl.Rule(term_imbalance['medium'] & growth_imbalance['low'] & military_imbalance['low'], balance['medium']),

    # Stan stabilnej asymetrii
    ctrl.Rule(term_imbalance['medium'] & growth_imbalance['medium'] & military_imbalance['medium'], balance['medium']),
    ctrl.Rule(term_imbalance['high'] & growth_imbalance['low'] & military_imbalance['low'], balance['medium']),

    # Krytyczne dysproporcje (dwa lub więcej elementów na HIGH) -> ocena LOW
    ctrl.Rule(term_imbalance['low'] & growth_imbalance['high'] & military_imbalance['high'], balance['low']),
    ctrl.Rule(term_imbalance['medium'] & growth_imbalance['high'] & military_imbalance['high'], balance['low']),
    ctrl.Rule(term_imbalance['high'] & growth_imbalance['high'] & military_imbalance['high'], balance['low']),

    # --- POPRAWKA GRADIENTU: Gdy gospodarka ucieka w HIGH, ale terytorium i wojsko są świetne (LOW)
    # Jeśli terytorium jest skrajnie niskie (bliskie 0), ocena powinna ciągnąć ku HIGH, a nie stać betonowo na 0.5
    ctrl.Rule(term_imbalance['low'] & growth_imbalance['high'] & military_imbalance['low'], balance['medium']),
    ctrl.Rule(term_imbalance['low'] & growth_imbalance['medium'] & military_imbalance['medium'], balance['medium']),

    # Reguła nadrzędna: totalna dominacja armii zawsze niszczy balans
    ctrl.Rule(military_imbalance['high'], balance['low'])
]

dynamism_rules = [
    # Całkowity paraliż
    ctrl.Rule(conq_rate['low'] & reconq_rate['low'] & peaks['low'], dynamism['low']),
    ctrl.Rule(conq_rate['low'] & reconq_rate['low'] & peaks['high'], dynamism['low']),

    # Wojna pozycyjna / Lokalne starcia
    ctrl.Rule(conq_rate['low'] & (reconq_rate['high'] | reconq_rate['medium']) & peaks['low'], dynamism['medium']),
    ctrl.Rule(conq_rate['low'] & (reconq_rate['high'] | reconq_rate['medium']) & peaks['high'], dynamism['medium']),

    # Pusty podbój lub szybki stomp
    ctrl.Rule(conq_rate['high'] & reconq_rate['low'] & peaks['low'], dynamism['low']),
    ctrl.Rule(conq_rate['high'] & reconq_rate['low'] & peaks['high'], dynamism['medium']),

    ctrl.Rule(conq_rate['high'] & reconq_rate['high'] & peaks['low'], dynamism['high']),
    ctrl.Rule(conq_rate['high'] & reconq_rate['medium'] & peaks['low'], dynamism['medium']),

    # Idealny dynamizm
    ctrl.Rule(conq_rate['high'] & (reconq_rate['high'] | reconq_rate['medium']) & peaks['high'], dynamism['high'])
]

# Kompilacja kontrolerów wnioskowania
balance_ctrl = ctrl.ControlSystem(balance_rules)
dynamism_ctrl = ctrl.ControlSystem(dynamism_rules)

balance_sim = ctrl.ControlSystemSimulation(balance_ctrl)
dynamism_sim = ctrl.ControlSystemSimulation(dynamism_ctrl)

# ============================================================
# 3. GENEROWANIE PRZEPISU MAPY I URUCHOMIENIE UNITY (BEZ ZMIAN)
# ============================================================

test_recipe = {
    "minSpawnDistance": 12,
    "population_max": 65,
    "populationToCreateNewUnit": 700
}

with open('map_input.json', 'w') as f:
    json.dump(test_recipe, f, indent=4)

print(f"--- [PYTHON] Zapisano przepis mapy do pliku JSON.")

unity_com = r"D:\Unity\6000.2.14f1\Editor\Unity.com"
project_path = os.getcwd()

unity_cmd = [
    unity_com,
    "-batchmode",
    "-nographics",
    "-projectPath", project_path,
    "-logFile", "unity_batch_log.txt",
    "-executeMethod", "BatchRunner.RunSim"
]

print("--- [PYTHON] Uruchamianie ukrytej symulacji w Unity (10 meczów)... Proszę czekać.")
subprocess.run(unity_cmd, check=True)
print("--- [PYTHON] Unity zakończyło pracę i przekazało kontrolę.")

# ============================================================
# 4. DYNAMICZNY ODCZYT I WNIOSKOWANIE ROZMYTE NA PEŁNYCH METRYKACH
# ============================================================

if os.path.exists('metrics_output.json'):
    with open('metrics_output.json', 'r') as f:
        metrics = json.load(f)

    print("\n--- [PYTHON] Surowe metryki odebrane z Unity:")
    print(json.dumps(metrics, indent=4))

    raw_game_length = metrics["gameLength"]

    # Sprawdzenie reguły nadrzędnej długości meczu (zabezpieczenie przed kuli śnieżnej)
    if raw_game_length < 15.0:
        final_balance = 0.0
        final_dynamism = 0.0
        print("\n=== [WYNIK OCENY] Gra zbyt krótka! Mapa zdyskwalifikowana (Efekt Kuli Śnieżnej). ===")
    else:
        # WSTRZYKNIĘCIE METRYK DO KONTROLERA BALANSU
        balance_sim.input['term_imbalance'] = metrics["avgTerritorialImbalance"] * 100.0
        balance_sim.input['growth_imbalance'] = metrics["avgGrowthImbalance"]
        balance_sim.input['military_imbalance'] = metrics["avgMilitaryImbalance"]
        balance_sim.compute()
        final_balance = balance_sim.output['balance']

        # WSTRZYKNIĘCIE METRYK DO KONTROLERA DYNAMIZMU
        dynamism_sim.input['conq_rate'] = metrics["conqueringRate"]
        dynamism_sim.input['reconq_rate'] = metrics["reconqueringRate"]
        dynamism_sim.input['peaks'] = metrics["peakDifferences"]
        dynamism_sim.compute()
        final_dynamism = dynamism_sim.output['dynamism']

        print("\n==================================================")
        print(f" FINALNA OCENA GRYWALNOŚCI MAPY (PEŁNY MODEL):")
        print(f" -> Ostateczny BALANS mapy:   {final_balance:.4f} / 1.0000")
        print(f" -> Ostateczny DYNAMIZM mapy: {final_dynamism:.4f} / 1.0000")
        print("==================================================")

else:
    print("\n[BŁĄD] Plik metrics_output.json nie został wygenerowany przez Unity!")