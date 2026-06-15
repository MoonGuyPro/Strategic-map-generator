using System.Collections.Generic;
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

    private System.Collections.IEnumerator Start()
    {
        if (botA == null || botB == null || mapGenerator == null)
        {
            Debug.LogError("BotTurnManager: przypisz botA, botB oraz mapGenerator!");
            yield break;
        }

        botA.enemyBot = botB;
        botB.enemyBot = botA;

        // Czekamy, aż generator mapy skończy generowanie pierwszej mapy
        while (!mapGenerator.IsGenerated)
            yield return null;

        if (executionMode == GameExecutionMode.RealTime)
        {
            StartSingleRealTimeGame();
        }
        else
        {
            // Odpalamy masową symulację wsadową
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
        // Klasyczna logika czasu rzeczywistego (tylko w trybie RealTime)
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

        // Zbieramy metryki na koniec każdej tury bota
        GameMetricsCollector.RecordTurnMetrics(botA, botB, mapGenerator);

        if (CheckGameOver() || currentGlobalTurnCount >= maxTurnsCap)
        {
            HandleGameEnd();
            return;
        }

        isATurn = !isATurn;
    }

    bool CheckGameOver()
    {
        int ownerA = mapGenerator.GetOwnerId(botA.SpawnPos);
        int ownerB = mapGenerator.GetOwnerId(botB.SpawnPos);

        if (ownerA != botA.botOwnerId) return true; // Bot B przejął bazę A
        if (ownerB != botB.botOwnerId) return true; // Bot A przejął bazę B
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
        
        // Zapis do pliku tekstowego za pomocą kolektora
        GameMetricsCollector.SaveGameReport(currentGlobalTurnCount, maxTurnsCap, botA, botB, winnerId);

        if (executionMode == GameExecutionMode.RealTime)
        {
            botA.enabled = false;
            botB.enabled = false;
            this.enabled = false;
            Debug.Log("Gra RealTime zakonczona, raport zapisany.");
        }
    }

    // Pętla dla szybkiej generacji masowej (FastSimulationBatch)
    System.Collections.IEnumerator ExecuteBatchSimulations()
    {
        Debug.LogWarning($"URUCHOMIONO TRYB MASOWEJ SYMULACJI: {batchSimulationCount} ROZGRYWEK.");

        for (currentBatchIndex = 1; currentBatchIndex <= batchSimulationCount; currentBatchIndex++)
        {
            // 1. Reset i generowanie nowej losowej mapy
            mapGenerator.SendMessage("Start"); // Wymuszamy ponowne Start() na generatorze mapy
            yield return null; // Czekamy klatkę na wygenerowanie struktur lądu i kopalń
            
            while (!mapGenerator.IsGenerated) yield return null;

            // 2. Reset botów do stanu początkowego
            botA.SendMessage("Start");
            botB.SendMessage("Start");
            yield return null;

            currentGlobalTurnCount = 0;
            gameOver = false;
            GameMetricsCollector.Reset(mapGenerator);
            isATurn = randomizeFirstBot ? (Random.Range(0, 2) == 0) : true;

            // 3. Pętla wykonująca grę bez czekania na czas rzeczywisty (Headless Loop)
            while (!gameOver && currentGlobalTurnCount < maxTurnsCap)
            {
                ExecuteSingleBotTurn();
            }

            Debug.Log($"Ukonczono symulacje meczu nr: {currentBatchIndex} / {batchSimulationCount}");
        }

        Debug.LogError("=== WSZYSTKIE SYMULACJE ZAKONCZONE! Pliki TXT sa gotowe w folderze projektu. ===");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}