using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GameMetricsCollector
{
    private static int totalReconquers = 0;
    private static int totalLandCells = 0;

    // Listy do gromadzenia próbek z każdej tury
    private static List<float> territorialImbalances = new();
    private static List<float> growthImbalances = new();
    private static List<float> militaryImbalances = new();
    private static List<float> bankImbalances = new();

    // Wyznaczanie punktów kulminacyjnych (Peak Differences)
    // Wzor (7) z artykulu zrodlowego: amplituda wahniecia max(d) - min(d) przy roznicy ZE ZNAKIEM.
    // Musi byc zgodne z BotTurnManager, ktory te same wielkosci zapisuje do metrics_output.json.
    private static float terDiffMin = 0f, terDiffMax = 0f;
    private static float groDiffMin = 0f, groDiffMax = 0f;
    private static float milDiffMin = 0f, milDiffMax = 0f;

    public static void Reset(HexMapGenerator map)
    {
        totalReconquers = 0;
        territorialImbalances.Clear();
        growthImbalances.Clear();
        militaryImbalances.Clear();
        bankImbalances.Clear();

        terDiffMin = 0f; terDiffMax = 0f;
        groDiffMin = 0f; groDiffMax = 0f;
        milDiffMin = 0f; milDiffMax = 0f;

        // Liczymy ile jest lądu na mapie, by wyznaczyć Conquering Rate
        totalLandCells = 0;
        if (map != null)
        {
            foreach (var cell in map.DebugCells)
            {
                if (!cell.isWater && cell.passable) totalLandCells++;
            }
        }
    }

    public static void RegisterReconquer()
    {
        totalReconquers++;
    }

    public static void RecordTurnMetrics(BotController botA, BotController botB, HexMapGenerator map)
    {
        if (map == null || totalLandCells == 0) return;

        int ownedByA = 0;
        int ownedByB = 0;
        int popA = botA.population;
        int popB = botB.population;
        int milA = 0;
        int milB = 0;
        int prodA = 0;
        int prodB = 0;

        // Skanowanie mapy dla terytoriów, zdolności produkcyjnej i armii na polach
        foreach (var cell in map.DebugCells)
        {
            if (cell.isWater || !cell.passable) continue;

            if (cell.ownerId == botA.botOwnerId)
            {
                ownedByA++;
                prodA += Mathf.Max(0, cell.populationNumber);
            }
            else if (cell.ownerId == botB.botOwnerId)
            {
                ownedByB++;
                prodB += Mathf.Max(0, cell.populationNumber);
            }
        }

        // Sila militarna wg GDD 7.4.C: tokeny polowe + garnizon bazy startowej.
        // Garnizony zwyklych pol NIE wchodza - inaczej metryka powielalaby terytorium.
        for (int i = 0; i < botA.TokenCount; i++) milA += botA.GetToken(i).armySize;
        for (int i = 0; i < botB.TokenCount; i++) milB += botB.GetToken(i).armySize;
        if (map.TryGetCell(botA.SpawnPos, out var baseA)) milA += baseA.army;
        if (map.TryGetCell(botB.SpawnPos, out var baseB)) milB += baseB.army;

        // 1. Territorial Imbalance (Różnica procentowa terytorium)
        float pctA = (float)ownedByA / totalLandCells;
        float pctB = (float)ownedByB / totalLandCells;
        float termDiff = Mathf.Abs(pctA - pctB);
        territorialImbalances.Add(termDiff);
        int ownedSum = ownedByA + ownedByB;
        if (ownedSum > 0)
        {
            float dTer = (float)(ownedByA - ownedByB) / ownedSum;
            if (dTer > terDiffMax) terDiffMax = dTer;
            if (dTer < terDiffMin) terDiffMin = dTer;
        }

        // 2. Growth Imbalance (procentowa różnica zdolności produkcyjnej terytorium)
        float totalProd = prodA + prodB;
        float growthDiff = totalProd > 0f ? (Mathf.Abs((float)prodA - prodB) / totalProd) * 100f : 0f;
        growthImbalances.Add(growthDiff);
        if (totalProd > 0f)
        {
            float dGro = ((float)prodA - prodB) / totalProd;
            if (dGro > groDiffMax) groDiffMax = dGro;
            if (dGro < groDiffMin) groDiffMin = dGro;
        }

        // 2b. Diagnostyka: dysproporcja stanu kont (wynik decyzji o wydatkach, nie własności mapy)
        float totalBank = popA + popB;
        bankImbalances.Add(totalBank > 0f ? (Mathf.Abs((float)popA - popB) / totalBank) * 100f : 0f);

        // 3. Military Imbalance (procentowa różnica siły militarnej)
        float totalMil = milA + milB;
        float milDiff = totalMil > 0f ? (Mathf.Abs((float)milA - milB) / totalMil) * 100f : 0f;
        militaryImbalances.Add(milDiff);
        if (totalMil > 0f)
        {
            float dMil = ((float)milA - milB) / totalMil;
            if (dMil > milDiffMax) milDiffMax = dMil;
            if (dMil < milDiffMin) milDiffMin = dMil;
        }
    }

    public static void SaveGameReport(int finalTurns, int maxTurns, BotController botA, BotController botB, int winnerId, int fieldBattles, int leadChanges, int startingBotId)
    {
        // Obliczanie średnich wartości dla metryk z artykułu
        float avgTerritorialImbalance = CalculateAverage(territorialImbalances);
        float avgGrowthImbalance = CalculateAverage(growthImbalances);
        float avgMilitaryImbalance = CalculateAverage(militaryImbalances);
        float avgBankImbalance = CalculateAverage(bankImbalances);

        float gameLengthPercentage = ((float)finalTurns / maxTurns) * 100f;

        // Ile pól łącznie podbito na koniec gry
        int totalCapturedAtEnd = 0;
        foreach (var cell in botA.map.DebugCells)
        {
            if (cell.ownerId != 0 && !cell.isWater) totalCapturedAtEnd++;
        }
        float conqueringRate = ((float)totalCapturedAtEnd / totalLandCells) * 100f;
        // Wzor (6) z artykulu zrodlowego: srednia na ture, eksportowana jako procent pol
        // zmieniajacych wlasciciela w przeliczeniu na 100 tur. Musi byc zgodne z BotTurnManager,
        // ktory ta sama wartosc zapisuje do metrics_output.json.
        float reconqueringRate = (totalLandCells > 0 && finalTurns > 0)
            ? ((float)totalReconquers / totalLandCells) * 100f / finalTurns * 100f
            : 0f;

        // W trybie wsadowym raporty ida poza Assets - inaczej setki plikow zamulaja import w Unity
        string folderPath = System.Environment.CommandLine.Contains("-batchmode")
            ? Path.Combine(Directory.GetCurrentDirectory(), "Wyniki_Batch")
            : Path.Combine(Application.dataPath, "Rozgrywki_Statystyki");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string fileName = $"Gra_{System.DateTime.Now:yyyyMMdd_HHmmss}_{Random.Range(100, 999)}.txt";
        string fullPath = Path.Combine(folderPath, fileName);

        using (StreamWriter writer = new StreamWriter(fullPath))
        {
            writer.WriteLine("==================================================");
            writer.WriteLine(winnerId == 0
                ? "RAPORT Z ROZGRYWKI - REMIS (limit tur, zadna baza nie padla)"
                : $"RAPORT Z ROZGRYWKI - ZWYCIEZCA: BOT {winnerId}");
            writer.WriteLine("==================================================");
            writer.WriteLine($"Liczba rozegranych tur: {finalTurns} / {maxTurns} ({gameLengthPercentage:F2}%)");
            writer.WriteLine($"Pierwszy ruch wykonal: BOT {startingBotId}");
            writer.WriteLine($"Conquering Rate (Podbite pola na koniec): {conqueringRate:F2}%");
            writer.WriteLine($"Reconquering Rate (% pol na 100 tur): {reconqueringRate:F2}");
            writer.WriteLine($"Field Battles (Bitwy polowe token vs token): {fieldBattles}");
            writer.WriteLine($"Field Battles na 100 tur: {(finalTurns > 0 ? fieldBattles * 100f / finalTurns : 0f):F2}");
            writer.WriteLine($"Lead Changes (Zmiany prowadzenia): {leadChanges}");
            writer.WriteLine($"Lead Changes na 100 tur: {(finalTurns > 0 ? leadChanges * 100f / finalTurns : 0f):F2}");
            writer.WriteLine();
            writer.WriteLine("METRYKI BALANSU I DYNAMIKI (SREDNIE Z CALEJ GRY):");
            writer.WriteLine($"- Territorial Imbalance (Liczba pol): {avgTerritorialImbalance * 100f:F2}%");
            writer.WriteLine($"- Growth Imbalance (Zdolnosc produkcyjna terytorium): {avgGrowthImbalance:F2}%");
            writer.WriteLine($"- Military Imbalance (Tokeny + baza): {avgMilitaryImbalance:F2}%");
            writer.WriteLine();
            writer.WriteLine("DIAGNOSTYKA (POZA FUNKCJA PRZYSTOSOWANIA):");
            writer.WriteLine($"- Dysproporcja stanu kont botow: {avgBankImbalance:F2}%");
            writer.WriteLine();
            writer.WriteLine("PUNKTY KULMINACYJNE PRZEWAGI (PEAK DIFFERENCES):");
            float peakTer = (terDiffMax - terDiffMin) * 100f;
            float peakGro = (groDiffMax - groDiffMin) * 100f;
            float peakMil = (milDiffMax - milDiffMin) * 100f;
            writer.WriteLine($"- Peak Territorial Difference: {peakTer:F2}%");
            writer.WriteLine($"- Peak Growth Difference: {peakGro:F2}%");
            writer.WriteLine($"- Peak Military Difference: {peakMil:F2}%");
            writer.WriteLine($"- Peak Average (srednia z trzech): {(peakTer + peakGro + peakMil) / 3f:F2}%");
            writer.WriteLine();
            writer.WriteLine("STATYSTYKI DECYZYJNE BOTOW (WYBORY PRIORYTETOW):");
            
            WriteBotStats(writer, botA);
            WriteBotStats(writer, botB);
        }

        Debug.LogWarning($"Zapisano statystyki rozgrywki do pliku: {fullPath}");
    }

    private static void WriteBotStats(StreamWriter writer, BotController bot)
    {
        writer.WriteLine($"--------------------------------------------------");
        writer.WriteLine($"BOT {bot.botOwnerId} (Spawn {bot.spawnNumber}):");
        writer.WriteLine($"Zgromadzona Populacja: {bot.population}");
        writer.WriteLine("Uzycie priorytetow decyzyjnych:");
        
        for (int i = 1; i <= 9; i++)
        {
            writer.WriteLine($"  -> {bot.GetPriorityName(i)}: {bot.PriorityCounters[i]} razy");
        }
        writer.WriteLine($"  -> {bot.GetPriorityName(0)}: {bot.PriorityCounters[0]} razy");
    }

    private static float CalculateAverage(List<float> list)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (var val in list) sum += val;
        return sum / list.Count;
    }
}