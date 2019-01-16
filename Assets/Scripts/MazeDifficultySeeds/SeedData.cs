using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class containing random seeds to be used for generating maze frames.
/// </summary>
[System.Serializable]
public class SeedData
{
    public Vector3Int mazeSize;
    public string mazeFrameCreatorUsed; // from object.GetType().FullName


    public int[][] seeds;
    public float[][] difficulties;
    public int[] seeds1;
    public int[] seeds2;
    public int[] seeds3;
    public int[] seeds4;
    public int[] seeds5;
    public float[] difficulties1;
    public float[] difficulties2;
    public float[] difficulties3;
    public float[] difficulties4;
    public float[] difficulties5;
    public int nDifficulty;

    public SeedData(int[] seeds1, int[] seeds2, int[] seeds3, int[] seeds4, int[] seeds5, float[] difficulties1, float[] difficulties2, float[] difficulties3, float[] difficulties4, float[] difficulties5, Vector3Int mazeSize, string mazeFrameCreatorUsed)
    {
        this.seeds1 = seeds1;
        this.seeds2 = seeds2;
        this.seeds3 = seeds3;
        this.seeds4 = seeds4;
        this.seeds5 = seeds5;
        this.difficulties1 = difficulties1;
        this.difficulties2 = difficulties2;
        this.difficulties3 = difficulties3;
        this.difficulties4 = difficulties4;
        this.difficulties5 = difficulties5;
        this.nDifficulty = 5;
        this.mazeSize = mazeSize;
        this.mazeFrameCreatorUsed = mazeFrameCreatorUsed;
    }

    public SeedData(int[] seeds1, int[] seeds2, int[] seeds3, int[] seeds4, float[] difficulties1, float[] difficulties2, float[] difficulties3, float[] difficulties4, Vector3Int mazeSize, string mazeFrameCreatorUsed)
    {
        this.seeds1 = seeds1;
        this.seeds2 = seeds2;
        this.seeds3 = seeds3;
        this.seeds4 = seeds4;
        this.difficulties1 = difficulties1;
        this.difficulties2 = difficulties2;
        this.difficulties3 = difficulties3;
        this.difficulties4 = difficulties4;
        this.nDifficulty = 4;
        this.mazeSize = mazeSize;
        this.mazeFrameCreatorUsed = mazeFrameCreatorUsed;
    }

    public SeedData(int[] seeds1, int[] seeds2, int[] seeds3, float[] difficulties1, float[] difficulties2, float[] difficulties3, Vector3Int mazeSize, string mazeFrameCreatorUsed)
    {
        this.seeds1 = seeds1;
        this.seeds2 = seeds2;
        this.seeds3 = seeds3;
        this.difficulties1 = difficulties1;
        this.difficulties2 = difficulties2;
        this.difficulties3 = difficulties3;
        this.nDifficulty = 3;
        this.mazeSize = mazeSize;
        this.mazeFrameCreatorUsed = mazeFrameCreatorUsed;
    }


    public SeedData(int[] seeds1, int[] seeds2, float[] difficulties1, float[] difficulties2, Vector3Int mazeSize, string mazeFrameCreatorUsed)
    {
        this.seeds1 = seeds1;
        this.seeds2 = seeds2;
        this.difficulties1 = difficulties1;
        this.difficulties2 = difficulties2;
        this.nDifficulty = 3;
        this.mazeSize = mazeSize;
        this.mazeFrameCreatorUsed = mazeFrameCreatorUsed;
    }
}
