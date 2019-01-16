using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class used for creating a out of mesh vertices/triangles.
/// </summary>
public abstract class MazeFrameCreatorMesh : MazeFrameCreator
{
    protected int nUniqueVertices;
    private MazeBaseBase mazeBaseBase;

    /// <summary>
    /// Gets or sets the name of the shape in the Resources/MazeShapes folder to base
    /// The maze on
    /// </summary>
    /// <value>Shape name.</value>
    public string ShapeName { get; protected set; }

    /// <summary>
    /// Creates a list of nodes, arranged according to mesh vert/tri, to be used for 
    /// building a maze using one of the implemented algorithms.
    /// </summary>
    /// <returns>List of nodes.</returns>
    protected List<MazeNode> GetMeshMazeBase()
    {
        // Loads vertices and triangles of specified mesh
        if (mazeBaseBase == null) mazeBaseBase = new MazeBaseBase(ShapeName);
        nUniqueVertices = mazeBaseBase.nNodes;

        // Set scale based on average vertex distance 
        Vector3 localScale = Scale / mazeBaseBase.avgVertexDistance;

        // Create maze base based on the above
        List<MazeNode> mazeBase = new List<MazeNode>(mazeBaseBase.nNodes);

        // Create nodes 
        for (int i = 0; i < mazeBaseBase.nNodes; i++)
        {
            // Set random jitter
            Vector3 randJitter = new Vector3(Random.Range(-(jitter * localScale.x), jitter * localScale.x), Random.Range(-(jitter * localScale.y), jitter * localScale.y), Random.Range(-(jitter * localScale.z), jitter * localScale.z));
            Vector3 currPosition = Vector3.Scale(mazeBaseBase.nodeBasePositions[i], localScale);
            mazeBase.Add(new MazeNode(currPosition + randJitter, mazeBaseBase.nodeIdentifiers[i]));
        }
        // Add connections
        for (int iNode = 0; iNode < mazeBaseBase.nNodes; iNode++)
        {
            for (int iNeigh = 0; iNeigh < mazeBaseBase.NeighborsIndices[iNode].Count; iNeigh++)
            {
                mazeBase[iNode].AddBaseConnectionByReference(mazeBase[mazeBaseBase.NeighborsIndices[iNode][iNeigh]]);
            }
        }

        // Sort for later easy reference
        mazeBase.Sort();

        return mazeBase;
    }


    /// <summary>
    /// Private class containing ingredients to create maze base.
    /// </summary>
    private class MazeBaseBase
    {
        public readonly List<string> nodeIdentifiers;
        public readonly List<Vector3> nodeBasePositions;
        public readonly List<List<int>> NeighborsIndices;
        public readonly int nNodes;
        public readonly float avgVertexDistance;
        public MazeBaseBase(string ShapeName)
        {
            // Load mesh
            Mesh mesh = Object.Instantiate(Resources.Load(ShapeName)) as Mesh;
            // Set storage
            nodeIdentifiers = new List<string>(mesh.vertexCount); // too much but better than nothing
            nodeBasePositions = new List<Vector3>(mesh.vertexCount); // too much but better than nothing
            NeighborsIndices = new List<List<int>>(mesh.vertexCount); // too much but better than nothing
            // First create node info
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                if (nodeIdentifiers.Contains(mesh.vertices[i].ToString())) { continue; }
                nodeIdentifiers.Add(mesh.vertices[i].ToString());
                nodeBasePositions.Add(mesh.vertices[i]);
            }
            nNodes = nodeIdentifiers.Count;
            // Set average position to 0
            Vector3 meanPos = Vector3.zero;
            foreach (Vector3 pos in nodeBasePositions) { meanPos += pos / nNodes; }
            for (int i = 0; i < nNodes; i++) { nodeBasePositions[i] = nodeBasePositions[i] - meanPos; }
            // Then, set neighbors based on triangles
            for (int i = 0; i < nNodes; i++)
            { NeighborsIndices.Add(new List<int>(6)); } // six is a typical number of maximum connections in a mesh of triangles
            for (int i = 0; i < mesh.triangles.Length; i = i + 3)
            {
                string node1ID = mesh.vertices[mesh.triangles[i + 0]].ToString();
                string node2ID = mesh.vertices[mesh.triangles[i + 1]].ToString();
                string node3ID = mesh.vertices[mesh.triangles[i + 2]].ToString();
                int node1Ind = nodeIdentifiers.FindIndex(x => x == node1ID);
                int node2Ind = nodeIdentifiers.FindIndex(x => x == node2ID);
                int node3Ind = nodeIdentifiers.FindIndex(x => x == node3ID);
                NeighborsIndices[node1Ind].AddIfNotPresent(node2Ind);
                NeighborsIndices[node1Ind].AddIfNotPresent(node3Ind);
                NeighborsIndices[node2Ind].AddIfNotPresent(node3Ind);
            }
            // Compute average distance between all connected vertices
            avgVertexDistance = 0;
            for (int iNode = 0; iNode < nNodes; iNode++)
            {
                float currDist = 0;
                for (int iNeigh = 0; iNeigh < NeighborsIndices[iNode].Count; iNeigh++)
                { currDist += Vector3.Distance(nodeBasePositions[iNode], nodeBasePositions[NeighborsIndices[iNode][iNeigh]]) / NeighborsIndices[iNode].Count; }
                avgVertexDistance += currDist / nNodes;
            }
        }
    }

}
