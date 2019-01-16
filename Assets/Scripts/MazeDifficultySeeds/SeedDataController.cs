using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;

/// <summary>
/// Class containing save/load methods for SeedData
/// </summary>
public static class SeedDataController
{
    public static void SaveSeedData(SeedData seedData, string filePath)
    {
        string dataAsJSON = JsonUtility.ToJson(seedData);
        File.WriteAllText(filePath, dataAsJSON);
    }

    public static SeedData LoadSeedData(string filePath)
    {
        SeedData seedData;
        if (File.Exists(filePath))
        {
            string dataAsJson = File.ReadAllText(filePath);
            seedData = JsonUtility.FromJson<SeedData>(dataAsJson);
        }
        else { throw new System.SystemException("SeedData file not found."); }

        // Parse into nested array
        seedData.seeds = new int[seedData.nDifficulty][];
        seedData.difficulties = new float[seedData.nDifficulty][];
        for (int i = 0; i < seedData.nDifficulty; i++)
        {
            string fieldName = "seeds" + (i + 1);
            seedData.seeds[i] = (int[])seedData.GetType().GetField(fieldName).GetValue(seedData);
            fieldName = "difficulties" + (i + 1);
            seedData.difficulties[i] = (float[])seedData.GetType().GetField(fieldName).GetValue(seedData);
        }
        return seedData;
    }

}
