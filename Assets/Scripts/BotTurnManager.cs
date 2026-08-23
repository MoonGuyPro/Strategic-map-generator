using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BotTurnManager : MonoBehaviour
{
    public enum GameExecutionMode { RealTime, FastSimulationBatch }

    [Header("Tryb wykonywania rozgrywek")]
    public GameExecutionMode executionMode = GameExecutionMode.RealTime;
    
    [Tooltip("Ile roznych gier (i nowych map) symulowac w trybie masowym?")]
    public int batchSimulationCount = 60;
    public int maxTurnsCap = 500; // Zabezpieczenie przed nieskończoną grą; scena nadpisuje ta sama wartoscia (GDD 7.2)

    [Header("Boty (przypisz w Inspectorze)")]
    public BotController botA;
    public BotController botB;
    public HexMapGenerator mapGenerator;

    [Header("Czas (Dla RealTime)")]
    public float turnInterval = 0.5f;

    [Header("Kolejnosc startowa")]
    public bool randomizeFirstBot = true;

    private float timer;
    private bool initialized;
    private bool isATurn;
    private bool gameOver;
    private int currentGlobalTurnCount = 0;
    private int currentBatchIndex = 0;

    // Struktury do obsługi formatu JSON Pythona
    [System.Serializable]
    private class PythonInputRecipe
    {
        public int minSpawnDistance;
        public int population_max;
        public int populationToCreateNewUnit;
        public int mapMode;
    }

    [System.Serializable]
    private class PythonInputBatch
    {
        public PythonInputRecipe[] recipes;
        public bool pairedFirstMove;
    }

    [System.Serializable]
    private class PythonOutputBatch
    {
        public PythonOutputMetrics[] results;
    }

    [System.Serializable]
    private class PythonOutputMetrics
    {
        public float avgTerritorialImbalance;
        public float gameLength;
        public float conqueringRate;
        public float avgGrowthImbalance;
        public float avgMilitaryImbalance;
        public float reconqueringRate;
        public float peakDifferences;
        public float peakGrowthDiff;
        public float peakMilitaryDiff;
        public float peakAverage;
        public float fieldBattles;
        public float leadChanges;
        public float leadChangeRate;
    }

    // Listy do zbierania średnich wyników z całego pokolenia (batcha 10 gier)
    private List<float> batchTerritorialImbalances = new List<float>();
    private List<float> batchGameLengths = new List<float>();
    private List<float> batchConqueringRates = new List<float>();

    // Zmienne pomocnicze do liczenia tura po turze w obrębie jednego meczu
    private float currentMatchTerritorialImbalanceSum = 0f;
    private int currentMatchRecordedTurns = 0;
    
    // Listy dla nowych metryk pokoleniowych
    private List<float> batchGrowthImbalances = new List<float>();
    private List<float> batchMilitaryImbalances = new List<float>();
    private List<float> batchReconqueringRates = new List<float>();
    private List<float> batchPeakDifferences = new List<float>();
    private List<float> batchPeakGrowthDiffs = new List<float>();
    private List<float> batchPeakMilitaryDiffs = new List<float>();
    private List<float> batchPeakAverages = new List<float>();
    private List<float> batchFieldBattles = new List<float>();
    private List<float> batchLeadChanges = new List<float>();
    private List<float> batchLeadChangeRates = new List<float>();

    // Zmienne meczowe resetowane co grę
    private float currentMatchGrowthImbalanceSum = 0f;
    private float currentMatchMilitaryImbalanceSum = 0f;
    private int currentMatchReconquers = 0;
    private int currentMatchFieldBattles = 0;
    private int currentMatchLeadChanges = 0;
    private int currentMatchLastLead = 0;
    private int currentMatchStartingBotId = 0;
    private bool pairedFirstMove = false;
    // Peak Differences wg wzoru (7) z artykulu zrodlowego (Lara-Cabrera i in., Natural Computing 2014):
    // amplituda wahniecia przewagi, czyli max(d) - min(d) przy roznicy ZE ZNAKIEM d = (f1-f2)/(f1+f2).
    // Mecz startuje symetrycznie (d = 0), wiec min <= 0 <= max i inicjalizacja zerami jest poprawna.
    // Zakres 0-2, eksportowany jako 0-200%. Wczesniej liczono maksimum MODULU roznicy (zakres 0-1),
    // co mierzylo glebokosc dominacji zamiast amplitudy zwrotu akcji.
    private float currentMatchTerritorialDiffMin = 0f;
    private float currentMatchTerritorialDiffMax = 0f;
    private float currentMatchGrowthDiffMin = 0f;
    private float currentMatchGrowthDiffMax = 0f;
    private float currentMatchMilitaryDiffMin = 0f;
    private float currentMatchMilitaryDiffMax = 0f;
    private Dictionary<Vector3Int, int> previousCellOwners = new Dictionary<Vector3Int, int>();

    private System.Collections.IEnumerator Start()
    {
        if (botA == null || botB == null || mapGenerator == null)
        {
            Debug.LogError("BotTurnManager: przypisz botA, botB oraz mapGenerator!");
            yield break;
        }

        botA.enemyBot = botB;
        botB.enemyBot = botA;
        
        if (System.Environment.CommandLine.Contains("-batchmode"))
        {
            executionMode = GameExecutionMode.FastSimulationBatch;
        }

        while (!mapGenerator.IsGenerated)
            yield return null;

        if (executionMode == GameExecutionMode.RealTime)
        {
            StartSingleRealTimeGame();
        }
        else
        {
            StartCoroutine(ExecuteBatchSimulations());
        }
    }

    void StartSingleRealTimeGame()
    {
        ClearBatchLists();
        currentMatchTerritorialImbalanceSum = 0f;
        currentMatchRecordedTurns = 0;
        currentMatchGrowthImbalanceSum = 0f;
        currentMatchMilitaryImbalanceSum = 0f;
        currentMatchReconquers = 0;
        currentMatchFieldBattles = 0;
        currentMatchLeadChanges = 0;
        currentMatchLastLead = 0;
        currentMatchTerritorialDiffMin = 0f; currentMatchTerritorialDiffMax = 0f;
        currentMatchGrowthDiffMin = 0f; currentMatchGrowthDiffMax = 0f;
        currentMatchMilitaryDiffMin = 0f; currentMatchMilitaryDiffMax = 0f;
        previousCellOwners.Clear();

        currentGlobalTurnCount = 0;
        gameOver = false;
        GameMetricsCollector.Reset(mapGenerator);
        isATurn = randomizeFirstBot ? (Random.Range(0, 2) == 0) : true;
        currentMatchStartingBotId = isATurn ? botA.botOwnerId : botB.botOwnerId;
        timer = turnInterval;
        initialized = true;
    }

    void Update()
    {
        if (executionMode != GameExecutionMode.RealTime || !initialized || gameOver) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = turnInterval;
        ExecuteSingleBotTurn();
    }

    void ExecuteSingleBotTurn()
    {
        currentGlobalTurnCount++;

        if (isATurn)
        {
            botA.TakeTurn();
            currentMatchFieldBattles += botA.ResolveCollisionsWith(botB);
        }
        else
        {
            botB.TakeTurn();
            currentMatchFieldBattles += botB.ResolveCollisionsWith(botA);
        }

        // Lokalne zbieranie danych na potrzeby mostu z Pythonem
        RecordLocalTurnMetrics();

        // Standardowy kolektor statystyk tekstowych
        GameMetricsCollector.RecordTurnMetrics(botA, botB, mapGenerator);

        if (CheckGameOver() || currentGlobalTurnCount >= maxTurnsCap)
        {
            HandleGameEnd();
            return;
        }

        isATurn = !isATurn;
    }

    void RecordLocalTurnMetrics()
    {
        int ownedA = 0; int ownedB = 0; int totalLand = 0;
        int totalArmyA = 0; int totalArmyB = 0;
        int prodA = 0; int prodB = 0;   // zdolnosc produkcyjna = suma populationNumber posiadanych pol

        // 1. Zliczanie wojska Bota A (Tokeny + Baza)
        for (int i = 0; i < botA.TokenCount; i++)
            if (botA.GetToken(i) != null) totalArmyA += botA.GetToken(i).armySize;
        if (mapGenerator.TryGetCell(botA.SpawnPos, out var cellBaseA)) totalArmyA += cellBaseA.army;

        // 2. Zliczanie wojska Bota B (Tokeny + Baza)
        for (int i = 0; i < botB.TokenCount; i++)
            if (botB.GetToken(i) != null) totalArmyB += botB.GetToken(i).armySize;
        if (mapGenerator.TryGetCell(botB.SpawnPos, out var cellBaseB)) totalArmyB += cellBaseB.army;

        // 3. Analiza pól i detekcja starć zwrotnych (Reconquer)
        foreach (var cell in mapGenerator.DebugCells)
        {
            if (cell.isWater || !cell.passable) continue;
            totalLand++;
            if (cell.ownerId == botA.botOwnerId) { ownedA++; prodA += Mathf.Max(0, cell.populationNumber); }
            else if (cell.ownerId == botB.botOwnerId) { ownedB++; prodB += Mathf.Max(0, cell.populationNumber); }

            if (previousCellOwners.TryGetValue(cell.coord, out int prevOwner))
            {
                if (prevOwner != 0 && cell.ownerId != 0 && prevOwner != cell.ownerId)
                    currentMatchReconquers++;
            }
            previousCellOwners[cell.coord] = cell.ownerId;
        }

        if (totalLand > 0)
        {
            float pctA = (float)ownedA / totalLand;
            float pctB = (float)ownedB / totalLand;
            float currentTermImbalance = Mathf.Abs(pctA - pctB);
            currentMatchTerritorialImbalanceSum += currentTermImbalance;

            // Peak wg wzoru (7): roznica ZE ZNAKIEM, znormalizowana przez stan posiadania obu botow.
            // (pctA-pctB)/(pctA+pctB) = (ownedA-ownedB)/(ownedA+ownedB) - mianownik z calej planszy sie skraca,
            // dzieki czemu wszystkie trzy piki maja teraz identyczna normalizacje, tak jak w artykule.
            int ownedSum = ownedA + ownedB;
            if (ownedSum > 0)
            {
                float dTer = (float)(ownedA - ownedB) / ownedSum;
                if (dTer > currentMatchTerritorialDiffMax) currentMatchTerritorialDiffMax = dTer;
                if (dTer < currentMatchTerritorialDiffMin) currentMatchTerritorialDiffMin = dTer;
            }

            // Zmiana prowadzenia: remis nie liczy sie jako zwrot, tylko faktyczne przejecie prowadzenia
            int lead = ownedA > ownedB ? 1 : (ownedB > ownedA ? -1 : 0);
            if (lead != 0)
            {
                if (currentMatchLastLead != 0 && lead != currentMatchLastLead)
                    currentMatchLeadChanges++;
                currentMatchLastLead = lead;
            }

            // Growth Imbalance: procentowa dysproporcja ZDOLNOSCI PRODUKCYJNEJ terytorium.
            float totalProd = prodA + prodB;
            float growthImb = totalProd > 0 ? (Mathf.Abs((float)prodA - prodB) / totalProd) * 100f : 0f;
            currentMatchGrowthImbalanceSum += growthImb;
            if (totalProd > 0)
            {
                float dGro = ((float)prodA - prodB) / totalProd;
                if (dGro > currentMatchGrowthDiffMax) currentMatchGrowthDiffMax = dGro;
                if (dGro < currentMatchGrowthDiffMin) currentMatchGrowthDiffMin = dGro;
            }

            // Procentowa dysproporcja siły militarnej
            float totalArmy = totalArmyA + totalArmyB;
            float milImb = totalArmy > 0 ? (Mathf.Abs((float)totalArmyA - totalArmyB) / totalArmy) * 100f : 0f;
            currentMatchMilitaryImbalanceSum += milImb;
            if (totalArmy > 0)
            {
                float dMil = ((float)totalArmyA - totalArmyB) / totalArmy;
                if (dMil > currentMatchMilitaryDiffMax) currentMatchMilitaryDiffMax = dMil;
                if (dMil < currentMatchMilitaryDiffMin) currentMatchMilitaryDiffMin = dMil;
            }

            currentMatchRecordedTurns++;
        }
    }

    bool CheckGameOver()
    {
        int ownerA = mapGenerator.GetOwnerId(botA.SpawnPos);
        int ownerB = mapGenerator.GetOwnerId(botB.SpawnPos);

        if (ownerA != botA.botOwnerId) return true; 
        if (ownerB != botB.botOwnerId) return true; 
        return false;
    }

    // Zwraca 0, gdy zadna baza nie padla - mecz przerwany limitem tur jest remisem, nie wygrana
    int GetWinnerId()
    {
        if (mapGenerator.GetOwnerId(botA.SpawnPos) != botA.botOwnerId) return botB.botOwnerId;
        if (mapGenerator.GetOwnerId(botB.SpawnPos) != botB.botOwnerId) return botA.botOwnerId;
        return 0;
    }

    void HandleGameEnd()
    {
        gameOver = true;
        int winnerId = GetWinnerId();
        
        GameMetricsCollector.SaveGameReport(currentGlobalTurnCount, maxTurnsCap, botA, botB, winnerId, currentMatchFieldBattles, currentMatchLeadChanges, currentMatchStartingBotId);

        float matchAvgTerritorialImbalance = currentMatchRecordedTurns > 0 ? currentMatchTerritorialImbalanceSum / currentMatchRecordedTurns : 0f;
        batchTerritorialImbalances.Add(matchAvgTerritorialImbalance);

        float matchAvgGrowthImbalance = currentMatchRecordedTurns > 0 ? currentMatchGrowthImbalanceSum / currentMatchRecordedTurns : 0f;
        batchGrowthImbalances.Add(matchAvgGrowthImbalance);

        float matchAvgMilitaryImbalance = currentMatchRecordedTurns > 0 ? currentMatchMilitaryImbalanceSum / currentMatchRecordedTurns : 0f;
        batchMilitaryImbalances.Add(matchAvgMilitaryImbalance);

        float lengthPct = ((float)currentGlobalTurnCount / maxTurnsCap) * 100f;
        batchGameLengths.Add(lengthPct);

        int finalCaptured = 0; int totalLand = 0;
        foreach (var cell in mapGenerator.DebugCells)
        {
            if (cell.isWater || !cell.passable) continue;
            totalLand++;
            if (cell.ownerId != 0) finalCaptured++;
        }
        float conqRate = totalLand > 0 ? ((float)finalCaptured / totalLand) * 100f : 0f;
        batchConqueringRates.Add(conqRate);

        // Reconquering Rate wg wzoru (6): Z = (1/tau) * suma(zeta_j / n_p), czyli SREDNIA NA TURE.
        // Eksportowane jako "procent pol zmieniajacych wlasciciela na 100 tur" (Z * 10000), zeby liczby
        // byly czytelne. Bez dzielenia przez tury metryka rosla wprost z dlugoscia meczu.
        float reconqRate = (totalLand > 0 && currentMatchRecordedTurns > 0)
            ? ((float)currentMatchReconquers / totalLand) * 100f / currentMatchRecordedTurns * 100f
            : 0f;
        batchReconqueringRates.Add(reconqRate);

        // Amplituda wahniecia przewagi (wzor 7), zakres 0-2 przeskalowany na 0-200%
        float peakTerPct = (currentMatchTerritorialDiffMax - currentMatchTerritorialDiffMin) * 100f;
        float peakGroPct = (currentMatchGrowthDiffMax - currentMatchGrowthDiffMin) * 100f;
        float peakMilPct = (currentMatchMilitaryDiffMax - currentMatchMilitaryDiffMin) * 100f;
        batchPeakDifferences.Add(peakTerPct);
        batchPeakGrowthDiffs.Add(peakGroPct);
        batchPeakMilitaryDiffs.Add(peakMilPct);
        batchPeakAverages.Add((peakTerPct + peakGroPct + peakMilPct) / 3f);
        batchFieldBattles.Add(currentMatchFieldBattles);
        batchLeadChanges.Add(currentMatchLeadChanges);
        batchLeadChangeRates.Add(currentGlobalTurnCount > 0 ? currentMatchLeadChanges * 100f / currentGlobalTurnCount : 0f);

        if (executionMode == GameExecutionMode.RealTime)
        {
            // Osobny plik, zeby podglad w edytorze nie nadpisal wynikow petli Pythona
            SaveMetricsJson("metrics_output_realtime.json");

            botA.enabled = false;
            botB.enabled = false;
            this.enabled = false;
            Debug.Log("Gra RealTime zakonczona, raport zapisany.");
        }
    }

    System.Collections.IEnumerator ExecuteBatchSimulations()
    {
        Debug.LogWarning("=== [UNITY] URUCHOMIONO TRYB MASOWEJ SYMULACJI BATCH ===");

        PythonInputRecipe[] recipes = LoadRecipes();
        PythonOutputMetrics[] results = new PythonOutputMetrics[recipes.Length];

        for (int r = 0; r < recipes.Length; r++)
        {
            ApplyRecipe(recipes[r]);
            Debug.LogWarning($"=== [UNITY] Chromosom {r + 1}/{recipes.Length}: SpawnsDist={recipes[r].minSpawnDistance}, PopMax={recipes[r].population_max}, UnitCost={recipes[r].populationToCreateNewUnit}");

            ClearBatchLists();

            for (currentBatchIndex = 1; currentBatchIndex <= batchSimulationCount; currentBatchIndex++)
            {
                currentMatchTerritorialImbalanceSum = 0f;
                currentMatchRecordedTurns = 0;
                currentMatchGrowthImbalanceSum = 0f;
                currentMatchMilitaryImbalanceSum = 0f;
                currentMatchReconquers = 0;
                currentMatchFieldBattles = 0;
                currentMatchLeadChanges = 0;
                currentMatchLastLead = 0;
                currentMatchTerritorialDiffMin = 0f; currentMatchTerritorialDiffMax = 0f;
                currentMatchGrowthDiffMin = 0f; currentMatchGrowthDiffMax = 0f;
                currentMatchMilitaryDiffMin = 0f; currentMatchMilitaryDiffMax = 0f;
                previousCellOwners.Clear();

                // W trybie parowanym kazda mapa rozgrywana jest dwa razy - raz z kazda kolejnoscia.
                // Nieparzysty mecz generuje nowa mape, parzysty powtarza ja z odwrocona kolejnoscia.
                bool nowaMapa = !pairedFirstMove || (currentBatchIndex % 2 == 1);
                if (nowaMapa)
                    mapGenerator.RerunMapGeneration();
                else
                    mapGenerator.ResetOwnershipKeepLayout();
                yield return null;

                botA.ResetBotState();
                botB.ResetBotState();
                yield return null;

                currentGlobalTurnCount = 0;
                gameOver = false;
                GameMetricsCollector.Reset(mapGenerator);
                // Sztywna naprzemiennosc zamiast losowania: dokladnie polowa meczow na kazda kolejnosc
                isATurn = (currentBatchIndex % 2 == 0);
                currentMatchStartingBotId = isATurn ? botA.botOwnerId : botB.botOwnerId;

                while (!gameOver && currentGlobalTurnCount < maxTurnsCap)
                {
                    ExecuteSingleBotTurn();
                }

                Debug.Log($"Chromosom {r + 1}/{recipes.Length}, mecz {currentBatchIndex}/{batchSimulationCount}");
            }

            results[r] = BuildMetrics();
        }

        SaveMetricsBatch("metrics_output.json", results);

        if (System.Environment.CommandLine.Contains("-batchmode"))
            UnityEditor.EditorApplication.Exit(0); 
        else
            UnityEditor.EditorApplication.isPlaying = false;
    }

    void ClearBatchLists()
    {
        batchTerritorialImbalances.Clear();
        batchGameLengths.Clear();
        batchConqueringRates.Clear();
        batchGrowthImbalances.Clear();
        batchMilitaryImbalances.Clear();
        batchReconqueringRates.Clear();
        batchPeakDifferences.Clear();
        batchPeakGrowthDiffs.Clear();
        batchPeakMilitaryDiffs.Clear();
        batchPeakAverages.Clear();
        batchFieldBattles.Clear();
        batchLeadChanges.Clear();
        batchLeadChangeRates.Clear();
    }

    PythonOutputMetrics BuildMetrics()
    {
        PythonOutputMetrics report = new PythonOutputMetrics();
        report.avgTerritorialImbalance = CalculateAverage(batchTerritorialImbalances);
        report.gameLength = CalculateAverage(batchGameLengths);
        report.conqueringRate = CalculateAverage(batchConqueringRates);
        report.avgGrowthImbalance = CalculateAverage(batchGrowthImbalances);
        report.avgMilitaryImbalance = CalculateAverage(batchMilitaryImbalances);
        report.reconqueringRate = CalculateAverage(batchReconqueringRates);
        report.peakDifferences = CalculateAverage(batchPeakDifferences);
        report.peakGrowthDiff = CalculateAverage(batchPeakGrowthDiffs);
        report.peakMilitaryDiff = CalculateAverage(batchPeakMilitaryDiffs);
        report.peakAverage = CalculateAverage(batchPeakAverages);
        report.fieldBattles = CalculateAverage(batchFieldBattles);
        report.leadChanges = CalculateAverage(batchLeadChanges);
        report.leadChangeRate = CalculateAverage(batchLeadChangeRates);
        return report;
    }

    void SaveMetricsJson(string fileName)
    {
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        File.WriteAllText(outputPath, JsonUtility.ToJson(BuildMetrics(), true));
        Debug.LogError($"=== [UNITY SUCCESS] ZAPISANO PLIK {fileName.ToUpper()} ===");
    }

    void SaveMetricsBatch(string fileName, PythonOutputMetrics[] results)
    {
        PythonOutputBatch batch = new PythonOutputBatch { results = results };
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        File.WriteAllText(outputPath, JsonUtility.ToJson(batch, true));
        Debug.LogError($"=== [UNITY SUCCESS] ZAPISANO {results.Length} WYNIKOW DO {fileName.ToUpper()} ===");
    }

    void ApplyRecipe(PythonInputRecipe recipe)
    {
        mapGenerator.mapMode = recipe.mapMode;
        mapGenerator.minSpawnDistance = recipe.minSpawnDistance;
        mapGenerator.population_max = recipe.population_max;
        botA.populationToCreateNewUnit = recipe.populationToCreateNewUnit;
        botB.populationToCreateNewUnit = recipe.populationToCreateNewUnit;
    }

    PythonInputRecipe[] LoadRecipes()
    {
        string inputPath = Path.Combine(Directory.GetCurrentDirectory(), "map_input.json");
        if (!File.Exists(inputPath))
        {
            Debug.LogWarning("=== [UNITY] Brak map_input.json - uzywam wartosci z Inspectora ===");
            return new[] { CurrentRecipeFromInspector() };
        }

        string jsonText = File.ReadAllText(inputPath);

        // Format docelowy: {"recipes":[...]}. Pojedynczy obiekt obslugiwany dla zgodnosci wstecz.
        PythonInputBatch batch = JsonUtility.FromJson<PythonInputBatch>(jsonText);
        if (batch != null && batch.recipes != null && batch.recipes.Length > 0)
        {
            pairedFirstMove = batch.pairedFirstMove;
            if (pairedFirstMove)
                Debug.LogWarning("=== [UNITY] TRYB PAROWANY: kazda mapa rozgrywana dwa razy, raz z kazda kolejnoscia ===");
            return batch.recipes;
        }

        PythonInputRecipe single = JsonUtility.FromJson<PythonInputRecipe>(jsonText);
        if (single != null && single.populationToCreateNewUnit > 0)
            return new[] { single };

        Debug.LogError("=== [UNITY] map_input.json ma nieznany format - uzywam wartosci z Inspectora ===");
        return new[] { CurrentRecipeFromInspector() };
    }

    PythonInputRecipe CurrentRecipeFromInspector()
    {
        return new PythonInputRecipe
        {
            minSpawnDistance = mapGenerator.minSpawnDistance,
            population_max = mapGenerator.population_max,
            populationToCreateNewUnit = botA.populationToCreateNewUnit
        };
    }

    private float CalculateAverage(List<float> list)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (var v in list) sum += v;
        return sum / list.Count;
    }
}