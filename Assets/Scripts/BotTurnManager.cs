using UnityEngine;

public class BotTurnManager : MonoBehaviour
{
    [Header("Boty (przypisz w Inspectorze)")]
    public BotController botA;
    public BotController botB;

    [Header("Czas")]
    public float turnInterval = 5f;

    [Header("Kolejnoœæ startowa")]
    public bool randomizeFirstBot = true;

    private float timer;
    private bool initialized;

    // kto ma teraz turê
    private bool isATurn;

    private System.Collections.IEnumerator Start()
    {
        if (botA == null || botB == null)
        {
            Debug.LogError("BotTurnManager: przypisz botA i botB!");
            yield break;
        }

        yield return null; // poczekaj a¿ mapy/boty zakoñcz¹ Start()

        isATurn = randomizeFirstBot ? (Random.Range(0, 2) == 0) : true;

        timer = turnInterval;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = turnInterval;
            RunSingleTurn();
        }
    }

    void RunSingleTurn()
    {
        // Jedna "tura" = tylko jeden bot wykonuje swój ruch (i ruchy swoich tokenów)
        if (isATurn)
            botA.TakeTurn();
        else
            botB.TakeTurn();

        // prze³¹cz na nastêpnego
        isATurn = !isATurn;
    }
}
