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
    private static float peakTerritorialDiff = 0f;
    private static float peakGrowthDiff = 0f;
    private static float peakMilitaryDiff = 0f;

    public static void Reset(HexMapGenerator map)
    {
        totalReconquers = 0;
        territorialImbalances.Clear();
        growthImbalances.Clear();
        militaryImbalances.Clear();
        bankImbalances.Clear();

        peakTerritorialDiff = 0f;
        peakGrowthDiff = 0f;
        peakMilitaryDiff = 0f;

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
                milA += cell.army;
                prodA += Mathf.Max(0, cell.populationNumber);
            }
            else if (cell.ownerId == botB.botOwnerId)
            {
                ownedByB++;
                milB += cell.army;
                prodB += Mathf.Max(0, cell.populationNumber);
            }
        }

        // Dodawanie wojska z aktywnych tokenów graczy
        for (int i = 0; i < botA.TokenCount; i++) milA += botA.GetToken(i).armySize;
        for (int i = 0; i < botB.TokenCount; i++) milB += botB.GetToken(i).armySize;

        // 1. Territorial Imbalance (Różnica procentowa terytorium)
        float pctA = (float)ownedByA / totalLandCells;
        float pctB = (float)ownedByB / totalLandCells;
        float termDiff = Mathf.Abs(pctA - pctB);
        territorialImbalances.Add(termDiff);
        if (termDiff > peakTerritorialDiff) peakTerritorialDiff = termDiff;

        // 2. Growth Imbalance (procentowa różnica zdolności produkcyjnej terytorium)
        float totalProd = prodA + prodB;
        float growthDiff = totalProd > 0f ? (Mathf.Abs((float)prodA - prodB) / totalProd) * 100f : 0f;
        growthImbalances.Add(growthDiff);
        if (growthDiff > peakGrowthDiff) peakGrowthDiff = growthDiff;

        // 2b. Diagnostyka: dysproporcja stanu kont (wynik decyzji o wydatkach, nie własności mapy)
        float totalBank = popA + popB;
        bankImbalances.Add(totalBank > 0f ? (Mathf.Abs((float)popA - popB) / totalBank) * 100f : 0f);

        // 3. Military Imbalance (Absolutna różnica siły militarnej)
        float milDiff = Mathf.Abs(milA - milB);
        militaryImbalances.Add(milDiff);
        if (milDiff > peakMilitaryDiff) peakMilitaryDiff = milDiff;
    }

    public static void SaveGameReport(int finalTurns, int maxTurns, BotController botA, BotController botB, int winnerId)
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
        float reconqueringRate = ((float)totalReconquers / totalLandCells) * 100f;

        // Budowanie pliku tekstowego
        string folderPath = Path.Combine(Application.dataPath, "Rozgrywki_Statystyki");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string fileName = $"Gra_{System.DateTime.Now:yyyyMMdd_HHmmss}_{Random.Range(100, 999)}.txt";
        string fullPath = Path.Combine(folderPath, fileName);

        using (StreamWriter writer = new StreamWriter(fullPath))
        {
            writer.WriteLine("==================================================");
            writer.WriteLine($"RAPORT Z ROZGRYWKI - ZWYCIEZCA: BOT {winnerId}");
            writer.WriteLine("==================================================");
            writer.WriteLine($"Liczba rozegranych tur: {finalTurns} / {maxTurns} ({gameLengthPercentage:F2}%)");
            writer.WriteLine($"Conquering Rate (Podbite pola na koniec): {conqueringRate:F2}%");
            writer.WriteLine($"Reconquering Rate (Wskaznik odbijania): {reconqueringRate:F2}%");
            writer.WriteLine();
            writer.WriteLine("METRYKI BALANSU I DYNAMIKI (SREDNIE Z CALEJ GRY):");
            writer.WriteLine($"- Territorial Imbalance (Liczba pol): {avgTerritorialImbalance * 100f:F2}%");
            writer.WriteLine($"- Growth Imbalance (Zdolnosc produkcyjna terytorium): {avgGrowthImbalance:F2}%");
            writer.WriteLine($"- Military Imbalance (Wojsko): {avgMilitaryImbalance:F2}");
            writer.WriteLine();
            writer.WriteLine("DIAGNOSTYKA (POZA FUNKCJA PRZYSTOSOWANIA):");
            writer.WriteLine($"- Dysproporcja stanu kont botow: {avgBankImbalance:F2}%");
            writer.WriteLine();
            writer.WriteLine("PUNKTY KULMINACYJNE PRZEWAGI (PEAK DIFFERENCES):");
            writer.WriteLine($"- Peak Territorial Difference: {peakTerritorialDiff * 100f:F2}%");
            writer.WriteLine($"- Peak Growth Difference: {peakGrowthDiff:F2}%");
            writer.WriteLine($"- Peak Military Difference: {peakMilitaryDiff:F2}");
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