import os
import json
import subprocess
import numpy as np
import skfuzzy as fuzz
from skfuzzy import control as ctrl

# ============================================================
# 1. KONFIGURACJA LOGIKI ROZMYTEJ (FUZZY SYSTEM)
# ============================================================

# Definiujemy wejścia (od 0% do 100%) oraz wyjścia (ocena od 0.0 do 1.0)
term_imbalance = ctrl.Antecedent(np.arange(0, 101, 1), 'term_imbalance')
conq_rate = ctrl.Antecedent(np.arange(0, 101, 1), 'conq_rate')

balance = ctrl.Consequent(np.arange(0, 1.01, 0.01), 'balance')
dynamism = ctrl.Consequent(np.arange(0, 1.01, 0.01), 'dynamism')

term_imbalance['low'] = fuzz.trimf(term_imbalance.universe, [0, 0, 25])
term_imbalance['medium'] = fuzz.trimf(term_imbalance.universe, [15, 35, 55])
term_imbalance['high'] = fuzz.trapmf(term_imbalance.universe, [45, 70, 100, 100])

conq_rate['low'] = fuzz.trimf(conq_rate.universe, [0, 0, 40])
conq_rate['medium'] = fuzz.trimf(conq_rate.universe, [30, 50, 70])
conq_rate['high'] = fuzz.trapmf(conq_rate.universe, [60, 80, 100, 100])

# Definicja poziomów wyjściowych dla ocen końcowych
balance['low'] = fuzz.trimf(balance.universe, [0, 0, 0.4])
balance['medium'] = fuzz.trimf(balance.universe, [0.3, 0.5, 0.7])
balance['high'] = fuzz.trimf(balance.universe, [0.6, 1.0, 1.0])

dynamism['low'] = fuzz.trimf(dynamism.universe, [0, 0, 0.4])
dynamism['medium'] = fuzz.trimf(dynamism.universe, [0.3, 0.5, 0.7])
dynamism['high'] = fuzz.trimf(dynamism.universe, [0.6, 1.0, 1.0])

# Baza reguł rozmytych z GDD
rule1 = ctrl.Rule(term_imbalance['low'], balance['high'])
rule2 = ctrl.Rule(term_imbalance['medium'], balance['medium'])
rule3 = ctrl.Rule(term_imbalance['high'], balance['low'])

rule4 = ctrl.Rule(conq_rate['high'], dynamism['high'])
rule5 = ctrl.Rule(conq_rate['medium'], dynamism['medium'])
rule6 = ctrl.Rule(conq_rate['low'], dynamism['low'])

# Budowa kontrolera rozmytego
balance_ctrl = ctrl.ControlSystem([rule1, rule2, rule3])
dynamism_ctrl = ctrl.ControlSystem([rule4, rule5, rule6])

balance_sim = ctrl.ControlSystemSimulation(balance_ctrl)
dynamism_sim = ctrl.ControlSystemSimulation(dynamism_ctrl)

# ============================================================
# 2. GENEROWANIE TESTOWEGO PRZEPISU MAPY (JSON INPUT)
# ============================================================

test_recipe = {
    "minSpawnDistance": 12,
    "population_max": 65,
    "populationToCreateNewUnit": 700
}

with open('map_input.json', 'w') as f:
    json.dump(test_recipe, f, indent=4)

print(f"--- [PYTHON] Zapisano przepis mapy do pliku JSON.")

# ============================================================
# 3. URUCHOMIENIE JEDNOSTKI SYMULACYJNEJ UNITY
# ============================================================

unity_com = r"D:\Unity\6000.2.14f1\Editor\Unity.com"
project_path = os.getcwd()

unity_cmd = [
    unity_com,
    "-batchmode",
    "-nographics",
    "-projectPath", project_path,
    "-logFile", "unity_batch_log.txt",       # POPRAWKA: Schowaj śmieci do pliku, wyczyść konsolę bota
    "-executeMethod", "BatchRunner.RunSim"
]

print("--- [PYTHON] Uruchamianie ukrytej symulacji w Unity (10 meczów)... Proszę czekać.")
subprocess.run(unity_cmd, check=True)
print("--- [PYTHON] Unity zakończyło pracę i przekazało kontrolę.")

# ============================================================
# 4. ODCZYT WYNIKÓW I WNIOSKOWANIE ROZMYTE (FUZZY INFERENCE)
# ============================================================

if os.path.exists('metrics_output.json'):
    with open('metrics_output.json', 'r') as f:
        metrics = json.load(f)

    print("\n--- [PYTHON] Surowe metryki odebrane z Unity:")
    print(json.dumps(metrics, indent=4))

    # Pobieramy dane i konwertujemy do formatu 0-100 dla systemu rozmytego
    raw_imbalance = metrics["avgTerritorialImbalance"] * 100.0  # zamiana stosunku 0-1 na %
    raw_conq_rate = metrics["conqueringRate"]
    raw_game_length = metrics["gameLength"]

    # Sprawdzenie reguły nadrzędnej (zbyt krótka gra dyskwalifikuje mapę)
    if raw_game_length < 15.0:
        final_balance = 0.0
        final_dynamism = 0.0
        print("\n=== [WYNIK OCENY] Gra zbyt krótka! Mapa zdyskwalifikowana (Efekt Kuli Śnieżnej). ===")
    else:
        # Wprowadzamy dane do systemów rozmytych
        balance_sim.input['term_imbalance'] = raw_imbalance
        balance_sim.compute()
        final_balance = balance_sim.output['balance']

        dynamism_sim.input['conq_rate'] = raw_conq_rate
        dynamism_sim.compute()
        final_dynamism = dynamism_sim.output['dynamism']

        print("\n==================================================")
        print(f" FINALNA OCENA GRYWALNOŚCI MAPY (LOGIKA ROZMYTA):")
        print(f" -> Ostateczny BALANS mapy:   {final_balance:.4f} / 1.0000")
        print(f" -> Ostateczny DYNAMIZM mapy: {final_dynamism:.4f} / 1.0000")
        print("==================================================")

else:
    print("\n[BŁĄD] Plik metrics_output.json nie został wygenerowany przez Unity!")