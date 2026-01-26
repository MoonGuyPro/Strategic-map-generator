using UnityEngine;

public class BotTurnManager : MonoBehaviour
{
    [Header("Boty (przypisz w Inspectorze)")]
    public BotController botA;
    public BotController botB;

    [Header("Czas")]
    public float turnInterval = 1.5f;

    [Header("Kolejnoœæ startowa")]
    public bool randomizeFirstBot = true;

    [Header("Game Over")]
    public bool freezeTimeOnGameOver = true;

    private float timer;
    private bool initialized;
    private bool isATurn;
    private bool gameOver;

    private System.Collections.IEnumerator Start()
    {
        if (botA == null || botB == null)
        {
            Debug.LogError("BotTurnManager: przypisz botA i botB!");
            yield break;
        }

        // podpinamy wrogów (¿eby BotController mia³ referencje)
        botA.enemyBot = botB;
        botB.enemyBot = botA;

        // poczekaj a¿ boty i mapa siê zainicjalizuj¹
        yield return null;

        isATurn = randomizeFirstBot ? (Random.Range(0, 2) == 0) : true;

        timer = turnInterval;
        initialized = true;
    }

    void Update()
    {
        if (!initialized || gameOver) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = turnInterval;

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

        // po akcji sprawdŸ bazê
        if (CheckGameOver())
            return;

        isATurn = !isATurn;
    }

    bool CheckGameOver()
    {
        // bezpieczeñstwo
        if (botA.map == null || botB.map == null) return false;

        int ownerA = botA.map.GetOwnerId(botA.SpawnPos);
        int ownerB = botB.map.GetOwnerId(botB.SpawnPos);

        if (ownerA != botA.botOwnerId)
        {
            EndGame(winner: botB, loser: botA);
            return true;
        }

        if (ownerB != botB.botOwnerId)
        {
            EndGame(winner: botA, loser: botB);
            return true;
        }

        return false;
    }

    void EndGame(BotController winner, BotController loser)
    {
        gameOver = true;

        Debug.Log($"GAME OVER! Winner BotOwnerId={winner.botOwnerId} | Loser BotOwnerId={loser.botOwnerId}");

        // wy³¹cz sterowanie
        winner.enabled = false;
        loser.enabled = false;
        enabled = false;

        if (freezeTimeOnGameOver)
            Time.timeScale = 0f;

        Application.Quit();
    }
}
