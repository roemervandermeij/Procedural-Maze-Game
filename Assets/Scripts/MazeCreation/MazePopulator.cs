using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

public class MazePopulator : MonoBehaviour
{
    public int MazeObjectsPlaced { get; private set; } = 0;


    public void PopulateWithSplineFittedCylinders(MazeFrame mazeFrame, ref MazeFrameSplines mazeFrameSplines, ref GameObject mazeObjects, Vector3 resize)
    {
        PopulateWithSplineFittedShape(mazeFrame, ref mazeFrameSplines, ref mazeObjects, resize, "MazeCylinder");
    }

    public void PopulateWithSplineFittedBars(MazeFrame mazeFrame, ref MazeFrameSplines mazeFrameSplines, ref GameObject mazeObjects, Vector3 resize)
    {
        PopulateWithSplineFittedShape(mazeFrame, ref mazeFrameSplines, ref mazeObjects, resize, "MazeBar");
    }

    public void PopulateWithSplineFittedShape(MazeFrame mazeFrame, ref MazeFrameSplines mazeFrameSplines, ref GameObject mazeObjects, Vector3 resize, string baseShape)
    {
        int objPerCurveSegment = 2;

        // First, fit splines to maze path segments
        mazeFrameSplines = new MazeFrameSplines(mazeFrame);

        // Set color list to use for paths
        List<Color> colorList = new List<Color>();
        GetColorBasedOnPathIndex(new List<int>(1), ref colorList);

        // Create parent object to hold maze objects
        mazeObjects = new GameObject("mazeObjects");
        mazeObjects.layer = 10;
        mazeObjects.transform.position = Vector3.zero;
        mazeObjects.transform.rotation = Quaternion.identity;
        // Load and resize prefab
        //string prefabObjName = baseShape + "/" + baseShape + "-20-" + objN;
        string prefabObjName = baseShape + "/" + baseShape + "-20-1";
        GameObject prefabInst = Instantiate(Resources.Load("Prefabs/" + prefabObjName) as GameObject);
        Vector3 mazeObjectPrefabSize = prefabInst.GetComponent<MeshRenderer>().bounds.size;
        if (!resize.ComponentsAreApproxEqualTo(mazeObjectPrefabSize))
        {
            Utilities.MeshResize(prefabInst, Vector3.Scale(resize, prefabInst.GetComponent<MeshRenderer>().bounds.size.ComponentInverse()));
            mazeObjectPrefabSize = prefabInst.GetComponent<MeshRenderer>().bounds.size;
        }
        // Create objects and add meshes to fit
        StartCoroutine(CreateSplineFittedMeshesCoroutine(mazeObjects, mazeFrameSplines, prefabInst, objPerCurveSegment, colorList));
        //CreateSplineFittedMeshesCoroutine(mazeObjects, mazeFrameSplines, prefabInst, objPerCurveSegment, colorList);

        //// Add lights to the sides
        //GameObject mazeLightObjects = new GameObject("mazeLightObjects");
        //mazeLightObjects.transform.position = Vector3.zero;
        //mazeLightObjects.transform.rotation = Quaternion.identity;
        //// Load prefab
        //string prefabLightName = "PathObjectLightBar";
        //GameObject prefabLightInst = Instantiate(Resources.Load("Prefabs/" + prefabLightName) as GameObject);
        //float spacing = 1f;
        //// Add lights
        //AddPathObjectLights(mazeLightObjects, mazeFrameSplines, prefabLightInst, spacing, mazeObjectPrefabSize, colorList);
    }



    private IEnumerator CreateSplineFittedMeshesCoroutine(GameObject mazeObjects, MazeFrameSplines mazeFrameSplines, GameObject prefabInst, int objPerCurveSegment, List<Color> colorList)
    {
        MazeObjectsPlaced = 0;
        // First, setup the shared materials that will be modified.
        Dictionary<int, Material> materials = new Dictionary<int, Material>();
        foreach (MazeFrameSplines.SplineSegment seg in mazeFrameSplines.SplineSegments)
        {
            if (seg.shortestPathInd.Count == 1)
            {
                if (materials.ContainsKey(seg.shortestPathInd[0])) { continue; }
                Material mat = Object.Instantiate(prefabInst.GetComponent<MeshRenderer>().material);
                mat.SetColor("_EmissionColor", GetColorBasedOnPathIndex(seg.shortestPathInd, ref colorList));
                materials.Add(seg.shortestPathInd[0], mat);
            }
        }
        Material materialBlack = Object.Instantiate(prefabInst.GetComponent<MeshRenderer>().material);
        materialBlack.SetColor("_EmissionColor", Color.black);
        //List<Color> colorList = new List<Color>();
        //Dictionary<List<int>, Material> materials = new Dictionary<List<int>, Material>();
        //foreach (MazeFrameSplines.SplineSegment seg in mazeFrameSplines.SplineSegments)
        //{
        //    if (materials.ContainsListKeyWithContents(seg.shortestPathInd)) { continue; }
        //    Material mat = Object.Instantiate(prefabInst.GetComponent<MeshRenderer>().material);
        //    mat.SetColor("_EmissionColor", GetColorBasedOnPathIndex(seg.shortestPathInd, ref colorList));
        //    materials.Add(seg.shortestPathInd, mat);
        //}

        // Per spline, create gameobject holding the mesh
        //Dictionary<string, GameObject> processedPrefabInstances = new Dictionary<string, GameObject>();
        int count = 0;
        foreach (MazeFrameSplines.SplineSegment segment in mazeFrameSplines.SplineSegments)
        {
            GameObject obj = Object.Instantiate(prefabInst, mazeObjects.transform) as GameObject;
            string junctionAffix = "";
            if (segment.isJunction)
            { junctionAffix = "junction_" + segment.JunctionID + "_"; }
            obj.name = junctionAffix + "spline_" + segment.spline.startPoint.ToString() + "_" + segment.spline.endPoint.ToString();
            obj.layer = 10;

            // Attached SplineFittedMesh script and initilaze with prefab
            obj.AddComponent(typeof(SplineFittedMesh));
            SplineFittedMesh splineMesh = obj.GetComponent<SplineFittedMesh>();
            splineMesh.SetSplineAxis(Vector3.forward);
            splineMesh.SetSpline(segment.spline);
            splineMesh.SetNMeshesPerSegment(objPerCurveSegment);
            Mesh[] meshes;
            if (mazeFrameSplines.ShortestPathInd.Count > 1)
            { meshes = new Mesh[] { Instantiate(obj.GetComponent<MeshFilter>().mesh), Instantiate(obj.GetComponent<MeshFilter>().mesh) }; }
            else
            { meshes = new Mesh[] { Instantiate(obj.GetComponent<MeshFilter>().mesh) }; }
            splineMesh.SetMeshBase(meshes);

            // Set segment color to first color if only one path, or determine multiple colors below 
            if (mazeFrameSplines.ShortestPathInd.Count == 1)
            { obj.GetComponent<MeshRenderer>().sharedMaterial = materials[0]; }
            else
            {
                // Determine segment color based on start/end neighbors if segment color itself is ambigious
                Material[] mats = new Material[2];
                if (segment.shortestPathInd.Count == 1)
                {
                    mats[0] = materials[segment.shortestPathInd[0]];
                    mats[1] = materials[segment.shortestPathInd[0]];
                }
                else
                {
                    // Start
                    if (segment.shortestPathIndAtStart.Count == 1)
                    { mats[0] = materials[segment.shortestPathIndAtStart[0]]; }
                    else
                    {
                        if (segment.shortestPathIndAtEnd.Count == 1)
                        { mats[0] = materials[segment.shortestPathIndAtEnd[0]]; }
                        else
                        { mats[0] = materialBlack; }
                    }
                    // End
                    if (segment.shortestPathIndAtEnd.Count == 1)
                    { mats[1] = materials[segment.shortestPathIndAtEnd[0]]; }
                    else
                    {
                        if (segment.shortestPathIndAtStart.Count == 1)
                        { mats[1] = materials[segment.shortestPathIndAtStart[0]]; }
                        else
                        { mats[1] = materialBlack; }
                    }
                }
                if (mats[0] == materialBlack && mats[1] == materialBlack &&
                    segment.shortestPathIndAtStart.EqualContents(segment.shortestPathIndAtEnd) &&
                    segment.shortestPathIndAtStart.Count == 2 && segment.shortestPathIndAtEnd.Count == 2)
                {
                    mats[0] = materials[segment.shortestPathInd[0]];
                    mats[1] = materials[segment.shortestPathInd[1]];
                }
                // Assign to object
                obj.GetComponent<MeshRenderer>().sharedMaterials = mats;
            }

            // Increment global counter
            MazeObjectsPlaced++;

            // Yield
            count++;
            if ((count % 200) == 0) { yield return null; }
        }
        // Destroy processed prefab instances
        Object.Destroy(prefabInst);
    }


    private void AddPathObjectLights(GameObject mazeLightObjects, MazeFrameSplines mazeFrameSplines, GameObject prefabInst, float spacing, Vector3 mazeObjectPrefabSize, List<Color> colorList)
    {
        // First, setup the shared materials that will be modified.
        Dictionary<int, Material> materials = new Dictionary<int, Material>();
        foreach (MazeFrameSplines.SplineSegment seg in mazeFrameSplines.SplineSegments)
        {
            if (seg.shortestPathInd.Count == 1)
            {
                if (materials.ContainsKey(seg.shortestPathInd[0])) { continue; }
                Material mat = Object.Instantiate(prefabInst.GetComponent<MeshRenderer>().material);
                mat.SetColor("_EmissionColor", GetColorBasedOnPathIndex(seg.shortestPathInd, ref colorList));
                materials.Add(seg.shortestPathInd[0], mat);
            }
        }
        Material materialBlack = Object.Instantiate(Resources.Load("Materials/" + "PathObjectLight")) as Material;
        materialBlack.SetColor("_EmissionColor", Color.black);

        // Per spline, create lights
        Vector3 lightSize = prefabInst.GetComponent<MeshRenderer>().bounds.size;
        int count = 0;
        foreach (MazeFrameSplines.SplineSegment segment in mazeFrameSplines.SplineSegments)
        {
            // Create parent object
            GameObject parent = new GameObject("segment" + count);
            parent.transform.parent = mazeLightObjects.transform;
            parent.transform.position = Vector3.zero;
            parent.transform.rotation = Quaternion.identity;
            parent.AddComponent<MeshFilter>();
            parent.AddComponent<MeshRenderer>();
            // Set color based on shortest path ind.
            if (segment.shortestPathInd.Count == 1)
            { parent.GetComponent<MeshRenderer>().sharedMaterial = materials[segment.shortestPathInd[0]]; }
            else
            { parent.GetComponent<MeshRenderer>().sharedMaterial = materialBlack; }


            // Determine total number of lights to place, and place them equally spaced
            int nLights = Mathf.RoundToInt(segment.spline.SplineTotalDistance / (lightSize.z + spacing));
            float currSpacing = (segment.spline.SplineTotalDistance - (lightSize.z * nLights)) / nLights;

            // Place lights
            for (int iLight = 0; iLight < nLights; iLight++)
            {
                // Get current spline vectors and set base position/rotation of light
                float currDist = ((lightSize.z + currSpacing) / 2f) + iLight * (lightSize.z + currSpacing);
                float currT = segment.spline.GetTAtDistance(currDist);
                Vector3 currTangent = segment.spline.GetTangentToPointOnSpline(currT);
                Vector3 currNormal = segment.spline.DefaultGetNormalAtT(currT);
                Vector3 currReference = Vector3.Cross(currNormal, currTangent).normalized;
                Quaternion baseRotation = Quaternion.LookRotation(currTangent);
                Vector3 basePosition = segment.spline.GetPointOnSpline(currT);
                Vector3 offset = ((mazeObjectPrefabSize.x / 2f) + (lightSize.x / 2f)) * currReference;

                // Place light object left/right
                foreach (Vector3 currOffset in new Vector3[] { -offset, offset })
                {
                    GameObject obj = Object.Instantiate(prefabInst, basePosition + currOffset, baseRotation, parent.transform) as GameObject;

                    // Set color based on shortest path ind.
                    if (segment.shortestPathInd.Count == 1)
                    { obj.GetComponent<MeshRenderer>().sharedMaterial = materials[segment.shortestPathInd[0]]; }
                    else
                    { obj.GetComponent<MeshRenderer>().sharedMaterial = materialBlack; }

                    // Increment global counter
                    MazeObjectsPlaced++;
                }
            }
            // Merge into one object
            Utilities.MergeChildrenOfParent(parent, true, true, true);
            count++;
        }
        // Destroy processed prefab instances
        Object.Destroy(prefabInst);
    }






    public Dictionary<string, Vector3> PopulateWithCylinders(MazeFrame mazeFrame, float fracConnSpace, int nNodesPerConnectionObj)
    {
        return PopulateWithShape(mazeFrame, fracConnSpace, nNodesPerConnectionObj, "MazeCylinder", "MazeCylinderConn");
    }
    public Dictionary<string, Vector3> PopulateWithBars(MazeFrame mazeFrame, float fracConnSpace, int nNodesPerConnectionObj)
    {
        return PopulateWithShape(mazeFrame, fracConnSpace, nNodesPerConnectionObj, "MazeBar", "MazeBarConn");
    }

    private Dictionary<string, Vector3> PopulateWithShape(MazeFrame mazeFrame, float fracConnSpace, int nNodesPerConnectionObj, string baseShape, string connShape)
    {
        // Set space allowed for connection as fraction of average connection length between nodes
        fracConnSpace = Mathf.Clamp01(fracConnSpace);
        float connSpace = (mazeFrame.Scale[0] + mazeFrame.Scale[1] + mazeFrame.Scale[2]) / 3 * fracConnSpace;

        // Prep storing of object center line nodes
        //int nNodesPerConnectionObj = 3;
        int nNodesPerBaseObj = 2;
        int nNodes = (mazeFrame.NumberOfConnections() * nNodesPerBaseObj) + (mazeFrame.NumberOfUniquePairwiseConnections() * nNodesPerConnectionObj);
        Dictionary<string, Vector3> objCenterLineNodes = new Dictionary<string, Vector3>(nNodes);


        // Load prefabs and set parent for game objects
        GameObject prefabCyl = Resources.Load("Prefabs/" + baseShape) as GameObject;
        GameObject prefabCylConn = Resources.Load("Prefabs/" + connShape) as GameObject;
        GameObject mazeObjects = new GameObject("mazeObjects");
        mazeObjects.transform.position = Vector3.zero;
        mazeObjects.transform.rotation = Quaternion.identity;
        mazeObjects.AddComponent<MeshFilter>();
        mazeObjects.AddComponent<MeshRenderer>();
        mazeObjects.GetComponent<MeshRenderer>().material = prefabCyl.GetComponent<MeshRenderer>().sharedMaterial;

        // First, go through each node and built appropriately bended connections
        foreach (MazeNode node in mazeFrame.Nodes)
        {
            // Check whether we're at a dead end
            if (node.ConnectedNeighbors.Count <= 1) { continue; }
            // Place connection object and pass center line nodes to main dictionary
            Dictionary<string, Vector3> newCenterLineNodes = PlaceShapeConnectionAtJunction(mazeObjects, node, prefabCylConn, connSpace, nNodesPerConnectionObj);
            Dictionary<string, Vector3>.KeyCollection keys = newCenterLineNodes.Keys;
            foreach (string key in keys)
            { objCenterLineNodes.Add(key, newCenterLineNodes[key]); }

            // Increment global counter
            MazeObjectsPlaced++;
        }


        // Then, build the base connections (store visit record)
        Dictionary<string, bool> isVisited = new Dictionary<string, bool>(mazeFrame.Nodes.Count);
        foreach (MazeNode nd in mazeFrame.Nodes) { isVisited.Add(nd.Identifier, false); }
        foreach (MazeNode node in mazeFrame.Nodes)
        {
            foreach (MazeNode neighbor in node.ConnectedNeighbors)
            {
                // Check whether all connections involving this neighbor have been built already
                if (isVisited[neighbor.Identifier]) { continue; }

                // set object properties
                Vector3 position = (node.Position + neighbor.Position) / 2;
                float scale = Vector3.Distance(node.Position, neighbor.Position);
                scale = scale - connSpace;
                Quaternion rot = prefabCyl.transform.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(neighbor.Position - node.Position));

                // Check dead end ends/start/end
                if (node.ConnectedNeighbors.Count == 1 || node.Identifier == "start" || node.Identifier == "end")
                {
                    position = position + ((node.Position - neighbor.Position).normalized * (connSpace / 4));
                    scale = scale + (connSpace / 2);
                }
                if (neighbor.ConnectedNeighbors.Count == 1 || neighbor.Identifier == "start" || neighbor.Identifier == "end")
                {
                    position = position + ((neighbor.Position - node.Position).normalized * (connSpace / 4));
                    scale = scale + (connSpace / 2);
                }

                // Place object and set scale
                GameObject cyl = Object.Instantiate(prefabCyl, position, rot, mazeObjects.transform);
                cyl.transform.localScale = new Vector3(cyl.transform.localScale.x, cyl.transform.localScale.y, cyl.transform.localScale.z * scale);
                cyl.name = "base-" + node.Identifier + "-" + neighbor.Identifier;
                cyl.AddComponent<MeshCollider>();
                cyl.GetComponent<MeshCollider>().convex = true;

                // Add two nodes (start/end) to storage with naming scheme: base-<nodeid>-<neighid>
                // If on dead end ends/start/end --> nodes at the extremes
                // If not --> shift nodes from the extremes to the center (as the conn nodes start at the extremes as well
                Vector3 nodeGravPos;
                Vector3 neighbGravPos;
                if (node.ConnectedNeighbors.Count == 1 || node.Identifier == "start" || node.Identifier == "end")
                { nodeGravPos = position + (node.Position - neighbor.Position).normalized * (scale / 2); }
                else
                { nodeGravPos = position + (node.Position - neighbor.Position).normalized * (scale / 4); }
                if (neighbor.ConnectedNeighbors.Count == 1 || neighbor.Identifier == "start" || neighbor.Identifier == "end")
                { neighbGravPos = position + (neighbor.Position - node.Position).normalized * (scale / 2); }
                else
                { { neighbGravPos = position + (neighbor.Position - node.Position).normalized * (scale / 4); } }
                objCenterLineNodes.Add("base-" + node.Identifier + "-" + neighbor.Identifier, nodeGravPos);
                objCenterLineNodes.Add("base-" + neighbor.Identifier + "-" + node.Identifier, neighbGravPos);
            }

            // Set current node as visisted
            isVisited[node.Identifier] = true;

            // Increment global counter
            MazeObjectsPlaced++;
        }
        //Utilities.MergeChildrenOfParent(mazeObjects);
        return objCenterLineNodes;
    }


    private Dictionary<string, Vector3> PlaceShapeConnectionAtJunction(GameObject mazeObjects, MazeNode node, GameObject prefabCylConn, float connSpace, int nNodesPerConnectionObj)
    {
        // Set dict to gather future gravity nodes
        Dictionary<string, Vector3> objCenterLineNodes = new Dictionary<string, Vector3>();

        // Create parent object to hold the connections
        GameObject parentObj = new GameObject("conn-" + node.Identifier);
        parentObj.layer = 10;
        parentObj.AddComponent<MeshFilter>();
        parentObj.AddComponent<MeshRenderer>();
        parentObj.GetComponent<MeshRenderer>().material = prefabCylConn.GetComponent<MeshRenderer>().sharedMaterial;
        parentObj.transform.parent = mazeObjects.transform;
        parentObj.transform.position = node.Position;


        // Go sthrough each connection pair coming from this node to built its connection
        for (int ineighb1 = 0; ineighb1 < (node.ConnectedNeighbors.Count - 1); ineighb1++)
        {
            for (int ineighb2 = ineighb1 + 1; ineighb2 < node.ConnectedNeighbors.Count; ineighb2++)
            {
                // get angle between connections, using direction from current node
                Vector3 currDirA = node.ConnectedNeighbors[ineighb1].Position - node.Position;
                Vector3 currDirB = node.ConnectedNeighbors[ineighb2].Position - node.Position;

                // MazeCylinderConnection is always bended in Z in ZX plane to the left, around Y
                // Create a new axis system, where:
                // forward = the direction of the first neighbor pointing the center node
                // right = pointing in the opposite of the general direction of the second neighbor
                // up = the direction such that the above holds
                // The connection object is placed in this axis system centered on the center node, and 
                // then bended along in the forward/right plane.
                // The unsigned angle is never bigger than 180 degrees due to the 180 degree rotation of the
                // right/up plane along the forward axis when a connection is on the left vs the right

                // Get new axis system 
                Vector3 currForward = -currDirA.normalized;
                Vector3 currUp = Vector3.Cross(currForward, -currDirB.normalized); // the general direction of the object should be left for easy bending

                // Instantiate object and resize
                GameObject conn = Object.Instantiate(prefabCylConn, parentObj.transform) as GameObject;
                Utilities.MeshResize(conn, new Vector3(1, 1, connSpace));

                // If angle is appropriate, bend if (angle>0), and determine gravPos nodes
                float angle = Vector3.Angle(-currDirA, currDirB);
                Vector3[] curveSamples;
                if (!Mathf.Approximately(angle, 0))
                { curveSamples = Utilities.MeshBendCircle(conn, angle, 2, 1, nNodesPerConnectionObj); }
                else
                {
                    curveSamples = new Vector3[nNodesPerConnectionObj];
                    Vector3 startPos = node.Position + (node.ConnectedNeighbors[ineighb1].Position - node.Position).normalized * (connSpace / 2);
                    for (int i = 0; i < nNodesPerConnectionObj; i++)
                    {
                        float currFracPos = 0f + ((1f / nNodesPerConnectionObj) * (float)i);
                        curveSamples[i] = startPos + ((node.ConnectedNeighbors[ineighb2].Position - node.Position).normalized * (connSpace * currFracPos));
                    }
                }

                // Set new axis system and place object at position
                conn.transform.rotation = Quaternion.LookRotation(currForward, currUp);
                conn.transform.position = node.Position;

                // name it 
                conn.name = "conn-" + node.Identifier + "-" + node.ConnectedNeighbors[ineighb1].Identifier + "-" + node.ConnectedNeighbors[ineighb2].Identifier;

                // Add nodes to storage with naming scheme: conn-<center_nodeid>-<start_neighid>-<end_neighid>-<num>-<index==0>
                for (int i = 0; i < curveSamples.Length; i++)
                {
                    string name = "conn-" + node.Identifier + "-" + node.ConnectedNeighbors[ineighb1].Identifier + "-" + node.ConnectedNeighbors[ineighb2].Identifier + "-" + curveSamples.Length + "-" + i;
                    Vector3 currCurveSample = curveSamples[i];
                    if (!Mathf.Approximately(angle, 0)) { currCurveSample = conn.transform.TransformPoint(currCurveSample); }
                    objCenterLineNodes.Add(name, currCurveSample);
                }
            }
        }
        // Merge children into parent and add colider
        Utilities.MergeChildrenOfParent(parentObj);
        parentObj.AddComponent<MeshCollider>();
        return objCenterLineNodes;
    }





    //public static void PopulateWithBars(MazeFrame mazeFrame)
    //{
    //    // Load prefab and set parent for maze objects
    //    GameObject prefabBar = Resources.Load("Prefabs/" + "MazeBar") as GameObject;
    //    GameObject mazeObjects = new GameObject("mazeObjects");
    //    mazeObjects.transform.position = Vector3.zero;
    //    mazeObjects.transform.rotation = Quaternion.identity;
    //    mazeObjects.AddComponent<MeshFilter>();
    //    mazeObjects.AddComponent<MeshRenderer>();
    //    mazeObjects.GetComponent<MeshRenderer>().material = prefabBar.GetComponent<MeshRenderer>().sharedMaterial;

    //    // First, create a way to save which nodes have been visited
    //    Dictionary<string, bool> isVisited = new Dictionary<string, bool>(mazeFrame.Nodes.Count);
    //    foreach (MazeNode nd in mazeFrame.Nodes)
    //    { isVisited.Add(nd.Identifier, false); }

    //    // Go throuch each node's neighbors and build the connections
    //    foreach (MazeNode node in mazeFrame.Nodes)
    //    {
    //        foreach (MazeNode neighbor in node.ConnectedNeighbors)
    //        {
    //            // Check whether all connections involving this neighbor have been built already
    //            if (isVisited[neighbor.Identifier]) { continue; }

    //            // set object properties
    //            Vector3 position = (node.Position + neighbor.Position) / 2;
    //            float scale = Vector3.Distance(node.Position, neighbor.Position);
    //            scale = scale + prefabBar.transform.localScale.x; // This increases the object such that the nodes are equidistant from their neirby surfaces(x/y are assumed identical)
    //            Quaternion rot = prefabBar.transform.rotation * Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(neighbor.Position - node.Position));

    //            // Place object and set scale
    //            GameObject bar = UnityEngine.Object.Instantiate(prefabBar, position, rot, mazeObjects.transform);
    //            bar.transform.localScale = new Vector3(1, 1, bar.transform.localScale.z * scale);
    //            bar.name = "bar-" + node.Identifier + "-" + neighbor.Identifier;
    //            bar.AddComponent<BoxCollider>();

    //        }
    //        // Set current node as visisted
    //        isVisited[node.Identifier] = true;
    //    }
    //    //Utilities.MergeChildrenOfParent(mazeObjects);
    //    //mazeObjects.AddComponent<MeshCollider>();
    //}

    private Color GetColorBasedOnPathIndex(List<int> shortestPathInd, ref List<Color> colorList)
    {
        // If color list has not been created yet, create a new random one
        if (colorList.Count == 0)
        {
            List<Color> baseList = new List<Color>(shortestPathInd.Count) {
                Color.blue,
                Color.cyan,
                Color.green,
                Color.magenta,
                Color.red,
                Color.yellow};
            // Randomize order into colorList
            List<int> indices = new List<int>(baseList.Count);
            for (int i = 0; i < baseList.Count; i++) { indices.Insert(i, i); }
            for (int i = 0; i < baseList.Count; i++)
            {
                int randInd = indices[Random.Range(0, indices.Count)];
                indices.Remove(randInd);
                colorList.Add(baseList[randInd]);
            }
        }
        // Set current color
        Color colorOut = Color.clear;
        if (shortestPathInd.Count > 0)
        {
            colorOut = Color.clear;
            foreach (int ind in shortestPathInd)
            { colorOut = colorOut + colorList[ind + 1]; }
        }
        else
        { colorOut = colorOut + colorList[0]; }
        //colorOut = (colorOut + (Color.white * 3f)) / 3f;
        if (shortestPathInd.Count > 1)
        { colorOut = (colorOut + (Color.black * 3f)) / 3f; }
        return colorOut;
    }



    public void PlacePlayerAndCamera(ref GameObject player, ref GameObject cameraRig, Vector3 position)
    {
        // Load prefab and get parent for maze objects
        GameObject playerPrefab = Resources.Load("Prefabs/" + "PlayerISH") as GameObject; //xEthan
        GameObject cameraPrefab = Resources.Load("Prefabs/" + "CameraRig") as GameObject;

        // Set Start position
        Vector3 playerPos = position;
        playerPos.y = playerPos.y + .75f + (playerPrefab.GetComponent<MeshRenderer>().bounds.size.y / 2);// + (playerPrefab.transform.localScale.y / 2);
        //playerPos.y = playerPos.y + .75f;
        playerPos.z = playerPos.z + 1;
        Vector3 cameraPos = playerPos;
        //Vector3 cameraPos = playerPos - (playerPrefab.transform.position - cameraPrefab.transform.position);
        Quaternion playerRot = playerPrefab.transform.rotation;
        Quaternion cameraRot = cameraPrefab.transform.rotation;

        // Place player and cam
        if (player == null) { player = Object.Instantiate(playerPrefab); player.name = "Player"; }
        if (cameraRig == null) { cameraRig = Object.Instantiate(cameraPrefab); cameraRig.name = "CameraRig"; }
        player.transform.position = playerPos;
        player.transform.rotation = playerRot;
        cameraRig.transform.position = cameraPos;
        cameraRig.transform.rotation = cameraRot;
        player.SetActive(true);
        cameraRig.SetActive(true);
    }

    public void PlaceCubeOfDeath(ref GameObject cubeParent, Vector3 position, Vector3 scale, Vector3 mazeFrameScale, MazeFrameSplines mazeFrameSplines)
    {
        if (cubeParent != null)
        {
            Object.Destroy(cubeParent);
            cubeParent = null;
        }
        cubeParent = new GameObject("CubesOfDeath");
        GameObject cube = Object.Instantiate(Resources.Load("Prefabs/" + "CubeOfDeath"), cubeParent.transform) as GameObject;
        cube.transform.position = position;
        cube.transform.localScale = Vector3.zero;
        cube.name = "CubeOfDeath";
        CubeOfDeathController cubeOfDeathController = cube.GetComponent<CubeOfDeathController>();
        CubeOfDeathController.MazeScale = mazeFrameScale;
        CubeOfDeathController.MazeFrameSplines = mazeFrameSplines;
        CubeOfDeathController.cubeScale = scale;
        cubeOfDeathController.Activate();
        //cube.SetActive(true);
    }

    public void PlaceEndPortal(ref GameObject portal, Vector3 position)
    {
        if (portal == null)
        { portal = Object.Instantiate(Resources.Load("Prefabs/" + "EndPortal")) as GameObject; }
        portal.transform.position = position;
        portal.name = "EndPortal";
        portal.SetActive(true);
    }

    public void PlaceMindWarpTriggers(ref GameObject triggerParent, MazeFrame mazeFrame, float scale, float triggerProb, int intensity)
    {
        if (triggerParent != null)
        {
            Object.Destroy(triggerParent);
            triggerParent = null;
        }
        triggerParent = new GameObject("MindWarpTriggers");
        GameObject prefabInst = Object.Instantiate(Resources.Load("Prefabs/" + "MindWarpTrigger")) as GameObject;
        prefabInst.GetComponent<SphereCollider>().radius = scale / 2f;
        Transform parent = triggerParent.GetComponent<Transform>();
        foreach (MazeNode node in mazeFrame.Nodes)
        {
            if (node == mazeFrame.StartNode ||
                node == mazeFrame.EndNode ||
                node.Identifier.Contains("entry") ||
                node.Identifier.Contains("exit"))
            { continue; }

            // Place trigger 
            if (node.ConnectedNeighbors.Count >= 3)
            {
                GameObject trigger = Object.Instantiate(prefabInst, parent);
                trigger.transform.position = node.Position;
                trigger.name = "trigger-" + node.Identifier;
                Vector3[] neighborPosition = new Vector3[node.ConnectedNeighbors.Count];
                for (int i = 0; i < node.ConnectedNeighbors.Count; i++)
                { neighborPosition[i] = node.ConnectedNeighbors[i].Position; }
                trigger.GetComponent<MindWarpController>().NeighborPosition = neighborPosition;
            }
        }
        Object.Destroy(prefabInst);
        // Set statics
        MindWarpController.TriggerProb = triggerProb;
        MindWarpController.IntensityLevel = intensity;
    }

}
