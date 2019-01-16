using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateAndSaveMazeRandomSeeds : MonoBehaviour
{

    private readonly int nMazes = 20;
    private readonly int nDifficulties = 2;
    private readonly float difficultySize = 0.40f;
    // 
    private readonly int divisions = 3;
    private readonly int nQuadrants = 0;
    //private readonly float[] difficultyLimits = { 0.1f, 0.3f }; // div3 nquadrants4
    private readonly float[] difficultyLimits = { 0.05f, 0.12f }; // div3 nquadrants0
    //
    private readonly Vector3 mazeScale = new Vector3(10, 10, 10);
    private readonly int mazeShape = 1; // 0 = square, 1 = icosahedron
    private readonly Vector3Int mazeSize = new Vector3Int(10, 1, 10);
    //
    private SeedData seedData;
    private string filePathBase = "/SeedData/mazeDifficultySeeds";

    // Use this for initialization
    void Start()
    {
        filePathBase = Application.streamingAssetsPath + filePathBase;
        StartCoroutine(CreateMazesFromSeeds());
    }


    private IEnumerator CreateMazesFromSeeds()
    {
        // Parse difficulty
        float[,] difficultyBounds = new float[nDifficulties, 2];
        float difficultyStartPad = difficultyLimits[0];
        float difficultyEndPad = 1 - difficultyLimits[1];
        float diffRange = 1 - (difficultyStartPad + difficultyEndPad);
        float currDiffSize = difficultySize * diffRange;
        float diffSpacing = (diffRange - (nDifficulties * currDiffSize)) / (nDifficulties - 1);
        if (diffSpacing < 0) { throw new System.Exception("Overlapping difficulties."); }
        for (int i = 0; i < (nDifficulties); i++)
        {
            difficultyBounds[i, 0] = difficultyStartPad + (currDiffSize + diffSpacing) * i;
            difficultyBounds[i, 1] = difficultyBounds[i, 0] + currDiffSize;
        }
        string boundsDisp = difficultyBounds[0, 0] + ">x<=" + difficultyBounds[0, 1];
        for (int i = 1; i < nDifficulties; i++)
        { boundsDisp = boundsDisp + " | " + difficultyBounds[i, 0] + ">x<=" + difficultyBounds[i, 1]; }
        Debug.Log(boundsDisp);

        //seedList[0] = new List<int>(nMazes); // difficulty <=.20
        //seedList[0] = new List<int>(nMazes); // difficulty >.20 <=.40
        //seedList[0] = new List<int>(nMazes); // difficulty >.40 <=.60
        //seedList[0] = new List<int>(nMazes); // difficulty >.60 <=80
        //seedList[0] = new List<int>(nMazes); // difficulty >.80 <=1

        // Set random seed
        int mainRandomSeed = System.Environment.TickCount;
        ConsistentRandom mainRandomGenenerator = new ConsistentRandom(mainRandomSeed);

        // Setup maze creators
        MazeFrameCreator mazeFrameCreator = null;
        switch (mazeShape)
        {
            case 0: { mazeFrameCreator = new MazeFrameCreatorSquare3D(mazeSize, nQuadrants); break; }
            case 1: { mazeFrameCreator = new MazeFrameCreatorMeshIcosahedron(divisions, nQuadrants); break; }
        }
        mazeFrameCreator.Scale = mazeScale; // Used in hunt and kill for determining  when to hunt

        // Create level from new seeds
        int ittMax = 1;
        if (nQuadrants != 0) { ittMax = nQuadrants; }
        for (int iQuadrant = 0; iQuadrant < ittMax; iQuadrant++)
        {
            // Setup seed/difficulty list
            List<List<int>> seedList = new List<List<int>>(nDifficulties);
            for (int i = 0; i < nDifficulties; i++)
            { seedList.Add(new List<int>(nMazes)); }
            List<List<float>> difficultyList = new List<List<float>>(nDifficulties);
            for (int i = 0; i < nDifficulties; i++)
            { difficultyList.Add(new List<float>(nMazes)); }

            bool done = false;
            int count = 0;
            while (!done)
            {
                // set new seed
                int mazeSeed = mainRandomGenenerator.Next();
                mazeFrameCreator.RandomSeed = mazeSeed;
                // get maze
                MazeFrame mazeFrame = mazeFrameCreator.GenerateEmptyMazeFrame();
                if (nQuadrants != 0) { mazeFrame.SetActiveQuadrant(iQuadrant); }
                mazeFrameCreator.GenerateMaze(ref mazeFrame);
                // Save according to difficulty
                float difficulty;
                if (nQuadrants == 0) { difficulty = mazeFrame.GetDifficulty()[0]; }
                else { difficulty = mazeFrame.GetDifficulty(iQuadrant)[0]; }
                for (int i = 0; i < nDifficulties; i++)
                {
                    if (difficulty > difficultyBounds[i, 0] && difficulty <= difficultyBounds[i, 1] && seedList[i].Count < nMazes)
                    {
                        seedList[i].Add(mazeSeed);
                        difficultyList[i].Add(difficulty);
                    }
                }

                // Check end condition
                done = true;
                for (int i = 0; i < nDifficulties; i++)
                { done = done && seedList[i].Count == nMazes; }

                // safety check
                count++;
                if (count == count * 100)
                { throw new System.Exception("Something went wrong."); }

                // Display progress
                string disp;
                if (nQuadrants == 0)
                { disp = count + ": " + seedList[0].Count; }
                else
                { disp = "quadrant" + (iQuadrant + 1) + "of" + nQuadrants + " | " + count + ": " + seedList[0].Count; }
                for (int i = 1; i < nDifficulties; i++)
                { disp = disp + ", " + seedList[i].Count; }
                Debug.Log(disp + "  | d = " + difficulty.ToString("0.0000"));

                // Yield
                WaitForSecondsRealtime wait = null;
                if (wait == null) { wait = new WaitForSecondsRealtime(0.0001f); }
                if ((count % 10) == 0)
                { yield return wait; }
            }

            // Convert to SeedData object
            if (nDifficulties == 5)
            {
                seedData = new SeedData(seedList[0].ToArray(), seedList[1].ToArray(), seedList[2].ToArray(), seedList[3].ToArray(), seedList[4].ToArray(),
                                       difficultyList[0].ToArray(), difficultyList[1].ToArray(), difficultyList[2].ToArray(), difficultyList[3].ToArray(), difficultyList[4].ToArray(),
                                       mazeSize, mazeFrameCreator.GetType().FullName);
            }
            else if (nDifficulties == 4)
            {// Convert to SeedData object
                seedData = new SeedData(seedList[0].ToArray(), seedList[1].ToArray(), seedList[2].ToArray(), seedList[3].ToArray(),
                                        difficultyList[0].ToArray(), difficultyList[1].ToArray(), difficultyList[2].ToArray(), difficultyList[3].ToArray(),
                                        mazeSize, mazeFrameCreator.GetType().FullName);
            }
            else if (nDifficulties == 3)
            {// Convert to SeedData object
                seedData = new SeedData(seedList[0].ToArray(), seedList[1].ToArray(), seedList[2].ToArray(),
                                        difficultyList[0].ToArray(), difficultyList[1].ToArray(), difficultyList[2].ToArray(),
                                        mazeSize, mazeFrameCreator.GetType().FullName);
            }
            else if (nDifficulties == 2)
            {// Convert to SeedData object
                seedData = new SeedData(seedList[0].ToArray(), seedList[1].ToArray(),
                                        difficultyList[0].ToArray(), difficultyList[1].ToArray(),
                                        mazeSize, mazeFrameCreator.GetType().FullName);
            }
            else
            { throw new System.Exception("Requested nDifficulty not implemented due to the stupid save part."); }

            // Save to disk
            string creatorName = "";
            string filePath;
            switch (mazeShape)
            {
                case 0: { creatorName = mazeFrameCreator.GetType().FullName + "-" + mazeSize.ToString() + "-" + mazeScale.ToString(); break; }
                case 1: { creatorName = mazeFrameCreator.GetType().FullName + "-" + divisions.ToString() + "-" + mazeScale.ToString(); break; }
            }
            if (nQuadrants != 0)
            { filePath = filePathBase + "-" + creatorName + "-" + "quadrant" + (iQuadrant + 1) + "of" + nQuadrants + ".json"; }
            else
            { filePath = filePathBase + "-" + creatorName + ".json"; }
            SeedDataController.SaveSeedData(seedData, filePath);
        }
        Debug.Log("Done!");
        Debug.Break();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

    }
}
