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

    // true => tura botA, false => tura botB
    private bool isATurn;

    private System.Collections.IEnumerator Start()
    {
        if (botA == null || botB == null)
        {
            Debug.LogError("BotTurnManager: przypisz botA i botB!");
            yield break;
        }

        yield return null; // mapa + boty koñcz¹ Start()

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

            if (isATurn)
            {
                botA.TakeTurn();
                ResolveTokenBattles(botA, botB); // walka po turze A
            }
            else
            {
                botB.TakeTurn();
                ResolveTokenBattles(botB, botA); // walka po turze B
            }

            // nastêpna tura: drugi bot
            isATurn = !isATurn;
        }
    }

    // attackerMoved = bot, który w³aœnie skoñczy³ turê
    void ResolveTokenBattles(BotController attackerMoved, BotController other)
    {
        // Iterujemy od koñca, bo bêdziemy usuwaæ tokeny
        for (int i = attackerMoved.TokenCount - 1; i >= 0; i--)
        {
            Vector3Int pos = attackerMoved.GetTokenPos(i);

            int j = other.FindTokenIndexAt(pos);
            if (j < 0) continue;

            ArmyToken aTok = attackerMoved.GetToken(i);
            ArmyToken bTok = other.GetToken(j);

            // Walka: wygrywa wiêksza armia
            if (aTok.armySize == bTok.armySize)
            {
                // Remis: obaj gin¹ (najprostsze i uczciwe – jeœli chcesz inaczej, zmienimy)
                attackerMoved.KillTokenPublic(i);
                other.KillTokenPublic(j);
                continue;
            }

            BotController winner = (aTok.armySize > bTok.armySize) ? attackerMoved : other;
            BotController loser = (winner == attackerMoved) ? other : attackerMoved;

            int winnerIndex = (winner == attackerMoved) ? i : j;
            int loserIndex = (winner == attackerMoved) ? j : i;

            ArmyToken wTok = winner.GetToken(winnerIndex);
            ArmyToken lTok = loser.GetToken(loserIndex);

            int loserArmy = lTok.armySize;

            // Zwyciêzca traci 0.8..1.2 armii pokonanego (wg ustawieñ zwyciêzcy)
            float mult = Random.Range(winner.winLossMin, winner.winLossMax);
            int loss = Mathf.RoundToInt(loserArmy * mult);

            // Zabij przegranego
            loser.KillTokenPublic(loserIndex);

            // Odejmij straty zwyciêzcy
            wTok.armySize -= loss;

            // Jeœli zwyciêzca te¿ spad³ do <=0, ginie
            if (wTok.armySize <= 0)
            {
                winner.KillTokenPublic(winnerIndex);
                continue;
            }

            // Zwyciêzca przejmuje pole (kolor/ownerId/garnizon)
            winner.ClaimTileAfterTokenBattle(pos);
        }
    }
}
