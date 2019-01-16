using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

// FIXME to difficult to go from inner to outer and vice versa
// FIXME maybe speedup in longer pieces, acceleration is too binary right now
// FIXME when not being able to view the correct path from the beginning, it is more fun
//
// FIXME Ensure only one path to entry/start
// FIXME Ensure start path is oriented such that backwards -> inner space
// FIXME Think about overlap/triggers that are too close together
// FIXME increases difficulty button


public class LevelController : MonoBehaviour
{

    // Level parameters
    private readonly bool PlaceCubesOfDeath = true;
    //
    private readonly float mindWarpProbability = 1f;
    private readonly int mindWarpIntensity = 1;
    //
    private readonly Vector3Int mazeSize = new Vector3Int(10, 1, 10); // for square
    private readonly int nDivisions = 3; // for icosahedron
    private readonly int mazeShape = 1; // 0 = square, 1 = icosahedron
    private readonly int nQuadrants = 0;
    //
    private readonly Vector3 mazeScale = new Vector3(10, 10, 10);
    private readonly float mazeJitter = 0f;
    private readonly int nSections = 1;
    private readonly float shrinkFactor = 0.8f;
    private readonly int[] difficulties = { 1 };
    private readonly Vector3 mazeDirection = Vector3.forward;
    private readonly Vector3 objectScaling = new Vector3(2, .15f, 1); //singleMazeFrames
    //
    //private readonly float gravCloseByDistFactor = 1f;
    private readonly float gravJunctionDistFactor = 0.35f;
    private readonly float gravPlaneWidth = 1.9f;
    //
    private readonly float startOffsetFactor = -0.5f;
    private readonly float endOffsetFactor = -1.5f; // 1.5f;
    private readonly float playerPlacementFactor = -0.15f;
    private readonly float cubeOfDeathPlacementFactor = 1.5f;
    private readonly float mindWarpTriggerScaleFactor = 1f;
    //

    // Level elements
    private MazeFrame mazeFrame;
    private MazeFrameSplines mazeFrameSplines;
    private GameObject mazeObjects;
    private GameObject player;
    private GameObject cameraRig;
    private GameObject cubeOfDeath;
    private GameObject endPortal;
    private GameObject mindWarpTriggers;
    private MazePopulator mazePopulator;
    public GravityFrameController gravityFrameController;
    //

    SeedData[] seedData;
    public bool LevelIsPresent { get; private set; }
    public float LevelBuiltProgressPercentage { get; private set; }
    private bool mazeFrameMeshesArePresent;

    private void Awake()
    {
        // initialize
        mazePopulator = gameObject.AddComponent<MazePopulator>();

        // Obtain seeds
        string seedDataFilePathBase = Application.streamingAssetsPath + "/SeedData/mazeDifficultySeeds";
        string creatorName = "";
        switch (mazeShape)
        {
            case 0: { creatorName = (new MazeFrameCreatorSquare3D(mazeSize)).GetType().FullName + "-" + mazeSize.ToString() + "-" + mazeScale.ToString(); break; }
            case 1: { creatorName = (new MazeFrameCreatorMeshIcosahedron(nDivisions)).GetType().FullName + "-" + nDivisions.ToString() + "-" + mazeScale.ToString(); break; }
        }

        if (nQuadrants != 0)
        {
            seedData = new SeedData[nQuadrants];
            for (int i = 0; i < nQuadrants; i++)
            {
                string seedDataFilePath = seedDataFilePathBase + "-" + creatorName + "-" + "quadrant" + (i + 1) + "of" + nQuadrants + ".json";
                seedData[i] = SeedDataController.LoadSeedData(seedDataFilePath);
            }
        }
        else
        {
            string seedDataFilePath = seedDataFilePathBase + "-" + creatorName + ".json";
            seedData = new SeedData[1] { SeedDataController.LoadSeedData(seedDataFilePath) };
        }

        for (int i = 0; i < seedData.Length; i++)
        {
            foreach (int diff in difficulties)
            {
                if (diff > (seedData[i].seeds.Length - 1))
                { throw new System.Exception("Difficulty level not present in seeds"); }
            }
        }

    }

    public void BuildNewLevel()
    {
        DestroyCurrentLevel();
        PlacePlayerAndCamera();
        StartCoroutine(BuildNewMaze());
        StartCoroutine(CheckLevelBuiltStatus());
    }


    public void RestartLevel()
    {
        PlacePlayerAndCamera();
        PlaceCubeOfDeath();
    }


    private void PlaceCubeOfDeath()
    {
        if (!PlaceCubesOfDeath) return;
        //Vector3 notMazeZ = (mazeDirection.ComponentPlus(-2)).ComponentAbs();
        float scale = objectScaling.ComponentMax() * 1.2f;//Vector3.Scale(Vector3.Scale(mazeScale, mazeSize), notMazeZ).ComponentMax() * 2;
        Vector3 pos = mazeFrame.StartNode.Position + (Vector3.Scale(mazeDirection, mazeScale) * cubeOfDeathPlacementFactor);
        mazePopulator.PlaceCubeOfDeath(ref cubeOfDeath, pos, Vector3.one * scale, mazeFrame.Scale, mazeFrameSplines);
    }

    private void PlacePlayerAndCamera()
    {
        // Place player and camera last, as player controller will initialize gravity frame further
        Vector3 position;
        if (mazeFrame != null)
        { position = mazeFrame.StartNode.Position + (Vector3.Scale(mazeDirection, mazeScale) * playerPlacementFactor); }
        else { position = Vector3.zero; }
        mazePopulator.PlacePlayerAndCamera(ref player, ref cameraRig, position);
    }

    private void PlaceEndPortal()
    {
        // Place EndPortal
        mazePopulator.PlaceEndPortal(ref endPortal, mazeFrame.EndNode.Position);
    }

    private void PlaceMindWarpTriggers()
    {
        float scale = mindWarpTriggerScaleFactor * mazeFrame.Scale.ComponentMin();
        mazePopulator.PlaceMindWarpTriggers(ref mindWarpTriggers, mazeFrame, scale, mindWarpProbability, mindWarpIntensity);
    }

    private IEnumerator BuildNewMaze()
    {
        float mazeFrameElementsBuilt = 0;
        mazeFrameMeshesArePresent = false;
        // Initialize maze creator
        MazeFrameCreator mazeFrameCreator = null;
        switch (mazeShape)
        {
            case 0:
                {
                    mazeFrameCreator = new MazeFrameCreatorSquare3D(mazeSize, nQuadrants)
                    { Scale = mazeScale, Jitter = mazeJitter }; break;
                }
            case 1:
                {
                    mazeFrameCreator = new MazeFrameCreatorMeshIcosahedron(nDivisions, nQuadrants)
                    { Scale = mazeScale, Jitter = mazeJitter }; break;
                }
        }
        // Set random seed selector
        System.Random randGen = new ConsistentRandom();
        // Randomize order of difficulties
        int[] randomQuadrantIndices = Utilities.RandomIndices(nQuadrants);
        // Generate maze!
        if (nQuadrants != 0 && nQuadrants != difficulties.Length) { throw new System.ArgumentException("When using quadrants, nQuadrants and nDifficulties should be equal."); }
        List<MazeFrame> singleMazeFrames = new List<MazeFrame>(difficulties.Length);
        for (int iDifficulty = 0; iDifficulty < difficulties.Length; iDifficulty++)
        {
            List<MazeFrame> singleFrameSections = new List<MazeFrame>(nSections);
            for (int iSections = 0; iSections < nSections; iSections++)
            {
                // Create sections
                int quadrantInd;
                //if (nQuadrants != 0) { quadrantInd = iDifficulty; }
                if (nQuadrants != 0) { quadrantInd = randomQuadrantIndices[iDifficulty]; }
                else { quadrantInd = 0; }
                MazeFrame currSection = null;
                int currDifficulty = difficulties[iDifficulty];
                int currSeedInd = randGen.Next(seedData[quadrantInd].seeds[currDifficulty].Length);
                mazeFrameCreator.RandomSeed = seedData[quadrantInd].seeds[currDifficulty][currSeedInd];
                //mazeFrameCreator.RandomSeed = seedData[quadrantInd].seeds[iDifficulty][iSections];
                if (nQuadrants == 0)
                { currSection = mazeFrameCreator.GenerateMaze(); }
                else
                {
                    currSection = mazeFrameCreator.GenerateEmptyMazeFrame();
                    mazeFrameCreator.GenerateMaze(ref currSection, quadrantInd);
                    currSection.SetOnShortestPath(currSection.Quadrants[quadrantInd], 0); // HACKY
                }
                //currSection.KeepOnlyShortestPathConnections();
                //currSection.AddPathSegments();
                // DEBUG
                if (nQuadrants != 0) { Debug.Log(currSection.GetNIntersectionsOnPath()[0] + "  " + currSection.GetDifficulty(quadrantInd)[0] + " - " + seedData[quadrantInd].difficulties[currDifficulty][currSeedInd]); }
                else { Debug.Log(currSection.GetNIntersectionsOnPath()[0] + "  " + currSection.GetDifficulty()[0]); }
                // DEBUG
                singleFrameSections.Add(currSection);
                mazeFrameElementsBuilt++;
                LevelBuiltProgressPercentage = (mazeFrameElementsBuilt / (difficulties.Length * nSections)) / 2;
                yield return null;
            }
            MazeFrame currSingleFrame;
            if (singleFrameSections.Count > 1)
            { //MazeFrame currSingleFrame = MazeFrame.Concatenate(singleFrameSections, mazeFrameCreator.Scale, mazeDirection);
                currSingleFrame = MazeFrame.CombineShrink(singleFrameSections, shrinkFactor);
            }
            else
            { currSingleFrame = singleFrameSections[0]; }
            currSingleFrame.AddOffsetStartNode(Vector3.Scale(mazeDirection, mazeScale) * startOffsetFactor, true);
            currSingleFrame.AddOffsetEndNode(Vector3.Scale(mazeDirection, mazeScale) * endOffsetFactor, true);
            for (int iInc = 0; iInc < iDifficulty; iInc++)
            { currSingleFrame.IncrementShortestPathIndices(); }
            singleMazeFrames.Add(currSingleFrame);
        }
        if (singleMazeFrames.Count > 1)
        { mazeFrame = MazeFrame.Merge(singleMazeFrames); }
        else
        { mazeFrame = singleMazeFrames[0]; }
        //mazeFrame.ConnectUnconnectedNodes();
        mazeFrame.AddPathSegments();
        yield return null;

        // Populate maze and get return splines and maze objects
        mazePopulator.PopulateWithSplineFittedBars(mazeFrame, ref mazeFrameSplines, ref mazeObjects, objectScaling);
        //mazePopulator.PopulateWithSplineFittedCylinders(mazeFrame, ref mazeFrameSplines, ref mazeObjects, objectScaling);

        // Wait till population is complete
        while (!mazeFrameMeshesArePresent)
        {
            if (mazePopulator.MazeObjectsPlaced == mazeFrameSplines.SplineSegments.Count)
            { mazeFrameMeshesArePresent = true; }
            yield return null;
        }

        // Create and Initialize gravity frame
        gravityFrameController = new GravityFrameController(mazeFrame, mazeFrameSplines, gravJunctionDistFactor, gravPlaneWidth);
        yield return null;


        // Place others
        PlacePlayerAndCamera();
        PlaceEndPortal();
        PlaceCubeOfDeath();
        PlaceMindWarpTriggers();
    }


    private IEnumerator CheckLevelBuiltStatus()
    {
        LevelIsPresent = false;
        LevelBuiltProgressPercentage = 0;

        while (!mazeFrameMeshesArePresent)
        {
            yield return null;
        }

        // Fetch spline fitted meshes from maze objects
        List<SplineFittedMesh> splineFittedMeshes = new List<SplineFittedMesh>(mazeObjects.transform.childCount);
        foreach (Transform child in mazeObjects.transform)
        {
            SplineFittedMesh childSFM = child.gameObject.GetComponent<SplineFittedMesh>();
            if (childSFM != null)
            { splineFittedMeshes.Add(childSFM); }
        }

        // Get progress
        int currBuilt = 0;
        while (currBuilt < splineFittedMeshes.Count)
        {
            currBuilt = 0;
            foreach (SplineFittedMesh sfm in splineFittedMeshes)
            { if (sfm.FitFinished) { currBuilt++; } }
            LevelBuiltProgressPercentage = 0.5f + (currBuilt / (float)splineFittedMeshes.Count / 2);
            yield return null;
        }
        // Finished!
        //// Combine junctions
        //Dictionary<string, List<GameObject>> junctionObjects = new Dictionary<string, List<GameObject>>();
        //foreach (Transform child in mazeObjects.transform)
        //{
        //    // Parse name
        //    string[] identifier = child.name.Split(new char[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries);
        //    if (identifier[0] == "junction")
        //    {
        //        if (!junctionObjects.ContainsKey(identifier[1])) { junctionObjects.Add(identifier[1], new List<GameObject>()); }
        //        junctionObjects[identifier[1]].Add(child.gameObject);
        //    }
        //}
        //foreach (string junction in junctionObjects.Keys)
        //{
        //    GameObject junctionGO = new GameObject("spline-junction-" + junction);
        //    junctionGO.layer = 10;
        //    junctionGO.transform.parent = mazeObjects.transform;
        //    junctionGO.AddComponent<MeshFilter>();
        //    junctionGO.AddComponent<MeshRenderer>();
        //    junctionGO.GetComponent<MeshRenderer>().material = junctionObjects[junction][0].GetComponent<MeshRenderer>().sharedMaterial;
        //    for (int i = 0; i < junctionObjects[junction].Count; i++)
        //    {
        //        junctionObjects[junction][i].transform.parent = junctionGO.transform;
        //        // Work around for "z-fighting"...
        //        //junctionObjects[junction][i].transform.position += Vector3.one * 0.001f * i;
        //    }
        //    Utilities.MergeChildrenOfParent(junctionGO, true, true, true);
        //}
        // add all sfm' mesh colliders...
        int count = 0;
        foreach (Transform child in mazeObjects.transform)
        {
            child.gameObject.AddComponent<MeshCollider>();
            count++;
            if ((count % 200) == 0) { yield return null; }
        }
        LevelIsPresent = true;
    }


    private void DestroyCurrentLevel()
    {
        if (!LevelIsPresent) { return; }
        mazeFrame = null;
        mazeFrameSplines = null;
        gravityFrameController = null;
        Destroy(mazeObjects);
        if (player != null) player.SetActive(false);
        if (cameraRig != null) cameraRig.SetActive(false);
        if (endPortal != null) endPortal.SetActive(false);
        if (cubeOfDeath != null) Destroy(cubeOfDeath);
        if (mindWarpTriggers != null) Destroy(mindWarpTriggers);
        //Destroy(player);
        //Destroy(cameraRig);
        //Destroy(cubeOfDeath);
        //Destroy(endPortal);
    }




    //private void OnDrawGizmos()
    //{
    //    if (mazeFrame != null)
    //    {
    //        foreach (MazeNode node in mazeFrame.Nodes)
    //        {

    //            Gizmos.color = Color.white;
    //            Gizmos.DrawSphere(node.Position, .05f * mazeFrame.Scale.ComponentMin());
    //        }
    //    }
    //}

}
