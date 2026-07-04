using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BotTurnManager : MonoBehaviour
{
    public enum GameExecutionMode { RealTime, FastSimulationBatch }

    [Header("Tryb wykonywania rozgrywek")]
    public GameExecutionMode executionMode = GameExecutionMode.RealTime;
    
    [Tooltip("Ile roznych gier (i nowych map) symulowac w trybie masowym?")]
    public int batchSimulationCount = 10;
    public int maxTurnsCap = 300; // Zabezpieczenie przed nieskończoną grą

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
    }

    [System.Serializable]
    private class PythonOutputMetrics
    {
        public float avgTerritorialImbalance;
        public float gameLength;
        public float conqueringRate;
    }

    // Listy do zbierania średnich wyników z całego pokolenia (batcha 10 gier)
    private List<float> batchTerritorialImbalances = new List<float>();
    private List<float> batchGameLengths = new List<float>();
    private List<float> batchConqueringRates = new List<float>();

    // Zmienne pomocnicze do liczenia tura po turze w obrębie jednego meczu
    private float currentMatchTerritorialImbalanceSum = 0f;
    private int currentMatchRecordedTurns = 0;

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
        currentGlobalTurnCount = 0;
        gameOver = false;
        GameMetricsCollector.Reset(mapGenerator);
        isATurn = randomizeFirstBot ? (Random.Range(0, 2) == 0) : true;
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
            botA.ResolveCollisionsWith(botB);
        }
        else
        {
            botB.TakeTurn();
            botB.ResolveCollisionsWith(botA);
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
        foreach (var cell in mapGenerator.DebugCells)
        {
            if (cell.isWater || !cell.passable) continue;
            totalLand++;
            if (cell.ownerId == botA.botOwnerId) ownedA++;
            else if (cell.ownerId == botB.botOwnerId) ownedB++;
        }

        if (totalLand > 0)
        {
            float pctA = (float)ownedA / totalLand;
            float pctB = (float)ownedB / totalLand;
            currentMatchTerritorialImbalanceSum += Mathf.Abs(pctA - pctB);
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

    int GetWinnerId()
    {
        int ownerA = mapGenerator.GetOwnerId(botA.SpawnPos);
        if (ownerA != botA.botOwnerId) return botB.botOwnerId;
        return botA.botOwnerId;
    }

    void HandleGameEnd()
    {
        gameOver = true;
        int winnerId = GetWinnerId();
        
        GameMetricsCollector.SaveGameReport(currentGlobalTurnCount, maxTurnsCap, botA, botB, winnerId);

        // Zapisz dane z pojedynczego meczu do list zbiorczych pod koniec gry
        float matchAvgTerritorialImbalance = currentMatchRecordedTurns > 0 ? currentMatchTerritorialImbalanceSum / currentMatchRecordedTurns : 0f;
        batchTerritorialImbalances.Add(matchAvgTerritorialImbalance);

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

        if (executionMode == GameExecutionMode.RealTime)
        {
            botA.enabled = false;
            botB.enabled = false;
            this.enabled = false;
            Debug.Log("Gra RealTime zakonczona, raport zapisany.");
        }
    }

    System.Collections.IEnumerator ExecuteBatchSimulations()
    {
        Debug.LogWarning("=== [UNITY] URUCHOMIONO TRYB MASOWEJ SYMULACJI BATCH ===");

        string inputPath = Path.Combine(Directory.GetCurrentDirectory(), "map_input.json");
        if (File.Exists(inputPath))
        {
            string jsonText = File.ReadAllText(inputPath);
            PythonInputRecipe recipe = JsonUtility.FromJson<PythonInputRecipe>(jsonText);

            mapGenerator.minSpawnDistance = recipe.minSpawnDistance;
            mapGenerator.population_max = recipe.population_max;
            botA.populationToCreateNewUnit = recipe.populationToCreateNewUnit;
            botB.populationToCreateNewUnit = recipe.populationToCreateNewUnit;

            Debug.LogWarning($"=== [UNITY SUCCESS] Wczytano JSON: SpawnsDist={recipe.minSpawnDistance}, PopMax={recipe.population_max}, UnitCost={recipe.populationToCreateNewUnit}");
        }

        batchTerritorialImbalances.Clear();
        batchGameLengths.Clear();
        batchConqueringRates.Clear();

        for (currentBatchIndex = 1; currentBatchIndex <= batchSimulationCount; currentBatchIndex++)
        {
            currentMatchTerritorialImbalanceSum = 0f;
            currentMatchRecordedTurns = 0;

            // FIX: Jawne wymuszenie czyszczenia i nowej generacji struktur lądu
            mapGenerator.RerunMapGeneration();
            yield return null; 

            // FIX: Jawne wyczyszczenie pamięci, list, liczników i usunięcie starych klonów jednostek bota
            botA.ResetBotState();
            botB.ResetBotState();
            yield return null;

            currentGlobalTurnCount = 0;
            gameOver = false;
            GameMetricsCollector.Reset(mapGenerator);
            isATurn = randomizeFirstBot ? (Random.Range(0, 2) == 0) : true;

            while (!gameOver && currentGlobalTurnCount < maxTurnsCap)
            {
                ExecuteSingleBotTurn();
            }

            Debug.Log($"Ukonczono symulacje meczu nr: {currentBatchIndex} / {batchSimulationCount}");
        }

        PythonOutputMetrics finalJsonReport = new PythonOutputMetrics();
        finalJsonReport.avgTerritorialImbalance = CalculateAverage(batchTerritorialImbalances);
        finalJsonReport.gameLength = CalculateAverage(batchGameLengths);
        finalJsonReport.conqueringRate = CalculateAverage(batchConqueringRates);

        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "metrics_output.json");
        string jsonOutputText = JsonUtility.ToJson(finalJsonReport, true);
        File.WriteAllText(outputPath, jsonOutputText);

        Debug.LogError("=== [UNITY SUCCESS] ZAPISANO PLIK METRICS_OUTPUT.JSON DLA PYTHONA ===");

        if (System.Environment.CommandLine.Contains("-batchmode"))
            UnityEditor.EditorApplication.Exit(0); 
        else
            UnityEditor.EditorApplication.isPlaying = false;
    }

    private float CalculateAverage(List<float> list)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (var v in list) sum += v;
        return sum / list.Count;
    }
}