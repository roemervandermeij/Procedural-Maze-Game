using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class used for creating a out of mesh vertices/triangles.
/// </summary>
public class MazeFrameCreatorMeshIcosahedron : MazeFrameCreatorMesh
{

    private readonly int nDivisions;
    private readonly int nQuadrants;

    /// <summary>
    /// Initializes a new instance of the <see cref="MazeFrameCreatorMeshIcosahedron"/> class 
    /// and set the shape to create the maze with, where <paramref name="nDivisions"/> refers
    /// to the number of divisions of the icosahedron when built with ProBuillder.
    /// </summary>
    /// <param name="nDivisions">Number of divisions parameter in ProBuilder.</param>
    public MazeFrameCreatorMeshIcosahedron(int nDivisions, int numberOfQuadrants = 0)
    {
        this.ShapeName = "Prefabs/MazeShapes/IcosphereDiv" + nDivisions + "Mesh";
        this.nDivisions = nDivisions;
        if (Resources.Load(this.ShapeName) == null) { throw new System.ArgumentException("Icosahedron asset with specified divisions not found."); }
        this.nQuadrants = numberOfQuadrants;
        if (nQuadrants != 0)
        {
            if (this.nQuadrants != 2 && this.nQuadrants != 4)
            { throw new System.ArgumentException("Icosahedron can only be subdivided into 2 or 4 quadrants."); }
        }
    }


    /// <summary>
    /// Generate empty maze frame.
    /// </summary>
    /// <returns>The empty maze frame.</returns>
    public override MazeFrame GenerateEmptyMazeFrame()
    {
        List<MazeNode> mazeBase = GetMeshMazeBase();
        SetStartAndEndNodes(ref mazeBase);
        return new MazeFrame(mazeBase, Scale, GetMinPathLength(), GetMaxPathLength(), GetQuadrants(mazeBase));
    }

    /// <summary>
    /// Gets the minimum path length based on size.
    /// </summary>
    /// <returns>The minimum path length.</returns>
    protected override int GetMinPathLength()
    {
        return (nDivisions * 5) + 1;
    }

    /// <summary>
    /// Gets the maximum path length based on size.
    /// </summary>
    /// <returns>The max path length.</returns>
    protected override int GetMaxPathLength()
    {
        return nUniqueVertices;
    }

    /// <summary>
    /// Sets the start and end nodes.
    /// </summary>
    /// <param name="mazeBase">Maze base.</param>
    protected override void SetStartAndEndNodes(ref List<MazeNode> mazeBase)
    {
        // Set start and end nodes, as the nodes with the most extreme z-value
        // start
        MazeNode startNode = mazeBase[0];
        for (int i = 0; i < mazeBase.Count; i++)
        {
            if (mazeBase[i].Position.z < startNode.Position.z)
            { startNode = mazeBase[i]; }
        }
        startNode.RenameNode("start");
        // end
        MazeNode endNode = mazeBase[0];
        for (int i = 0; i < mazeBase.Count; i++)
        {
            if (mazeBase[i].Position.z > endNode.Position.z)
            { endNode = mazeBase[i]; }
        }
        endNode.RenameNode("end");

        // Sort for later easy reference
        mazeBase.Sort();
    }

    /// <summary>
    /// Get the quadrant specification
    /// </summary>
    /// <returns>The quadrants.</returns>
    protected override List<List<MazeNode>> GetQuadrants(List<MazeNode> mazeBase)
    {
        // Get start/end
        MazeNode start = mazeBase.Find(x => x.Identifier == "start");
        MazeNode end = mazeBase.Find(x => x.Identifier == "end");

        // Get the 6 extremes
        MazeNode front = mazeBase[0];
        MazeNode back = mazeBase[0];
        MazeNode up = mazeBase[0];
        MazeNode down = mazeBase[0];
        MazeNode left = mazeBase[0];
        MazeNode right = mazeBase[0];
        for (int i = 0; i < mazeBase.Count; i++)
        {
            if (mazeBase[i].Position.z > front.Position.z) { front = mazeBase[i]; }
            if (mazeBase[i].Position.z < back.Position.z) { back = mazeBase[i]; }
            if (mazeBase[i].Position.y > up.Position.y) { up = mazeBase[i]; }
            if (mazeBase[i].Position.y < down.Position.y) { down = mazeBase[i]; }
            if (mazeBase[i].Position.x < left.Position.x) { left = mazeBase[i]; }
            if (mazeBase[i].Position.x > right.Position.x) { right = mazeBase[i]; }
        }
        // Set tolerance
        float tol = Scale.ComponentMin() * .05f;
        List<List<MazeNode>> quadrants = null;

        // First, remove some base connections around start/end to make the entry/exit symmetric
        foreach (MazeNode baseNode in new List<MazeNode>(2) { start, end })
        {
            // front/back and up/down
            List<MazeNode> keepList = new List<MazeNode>(2);
            for (int iUpDown = 0; iUpDown <= 1; iUpDown++)
            {
                MazeNode targetNode = baseNode;
                foreach (MazeNode neigh in baseNode.AllNeighbors)
                {
                    switch (iUpDown)
                    {
                        case 0: { if (neigh.Position.y > targetNode.Position.y) targetNode = neigh; break; }
                        case 1: { if (neigh.Position.y < targetNode.Position.y) targetNode = neigh; break; }
                    }
                }
                if (nQuadrants == 4)
                { baseNode.RemoveBaseConnectionByReference(targetNode); }
                else if (nQuadrants == 2)
                { keepList.Add(targetNode); }
            }
            if (nQuadrants == 2)
            {
                List<MazeNode> removeList = new List<MazeNode>(baseNode.AllNeighbors);
                removeList.Remove(keepList[0]);
                removeList.Remove(keepList[1]);
                baseNode.RemoveBaseConnectionByReference(removeList);
            }
        }

        // Replace all nodes on quadrant boundaries with a boundary specific node that is closeby, moved in the direction of the quadrant
        List<MazeNode> newNodes = new List<MazeNode>();
        List<MazeNode> oldNodes = new List<MazeNode>();
        int nBoundaries = -1;
        if (nQuadrants == 2) nBoundaries = 0;
        else if (nQuadrants == 4) nBoundaries = 1;
        for (int iBoundary = 0; iBoundary <= nBoundaries; iBoundary++) // 0 = up/down boundary, 1 = left/right boundary
        {
            foreach (MazeNode node in mazeBase)
            {
                if (node == start || node == end) continue;
                bool comparison = false;
                switch (iBoundary)
                {
                    case 0: comparison = node.Position.y > back.Position.y - tol && node.Position.y < back.Position.y + tol; break;
                    case 1: comparison = node.Position.x > back.Position.x - tol && node.Position.x < back.Position.x + tol; break;
                }
                if (comparison)
                {
                    // Create new node nodes
                    for (int iNewNode = 0; iNewNode <= 1; iNewNode++) // iBoundary = 0: 0/1 = up/down, iBoundary = 1: 0/1 = left/right 
                    {
                        List<MazeNode> newNodeNeighbors = new List<MazeNode>();
                        MazeNode targetNeigh = node;
                        foreach (MazeNode neigh in node.AllNeighbors)
                        {
                            bool neighComparison = false;
                            switch (iBoundary)
                            {
                                case 0:
                                    switch (iNewNode)
                                    {
                                        case 0: neighComparison = (neigh.Position.y > targetNeigh.Position.y - tol); break; // Up
                                        case 1: neighComparison = (neigh.Position.y < targetNeigh.Position.y + tol); break; // Down
                                    }
                                    break;
                                case 1:
                                    switch (iNewNode)
                                    {
                                        case 0: neighComparison = (neigh.Position.x < targetNeigh.Position.x + tol); break; // Left
                                        case 1: neighComparison = (neigh.Position.x > targetNeigh.Position.x - tol); break; // Right
                                    }
                                    break;
                            }
                            if (neighComparison)
                            {
                                targetNeigh = neigh;
                                newNodeNeighbors.Add(neigh);
                            }
                        }
                        Vector3 newNodeDirection = (targetNeigh.Position - node.Position).normalized;
                        switch (iBoundary)
                        {
                            case 0: newNodeDirection.x = 0; break;
                            case 1: newNodeDirection.y = 0; break;
                        }
                        Vector3 currPosition = node.Position + newNodeDirection * tol * 2;
                        Vector3 randJitter = new Vector3(Random.Range(-(jitter * Scale.x), jitter * Scale.x), Random.Range(-(jitter * Scale.y), jitter * Scale.y), Random.Range(-(jitter * Scale.z), jitter * Scale.z));
                        MazeNode newNode = new MazeNode(currPosition + randJitter, node.Identifier + (iNewNode + 1));
                        newNode.AddBaseConnectionByReference(newNodeNeighbors);
                        newNodes.Add(newNode);
                    }
                    // Store old node to remove later
                    oldNodes.Add(node);
                }
            }
        }
        // Add new nodes
        foreach (MazeNode node in newNodes) { mazeBase.Add(node); }
        // Remove old node from mazeBase and sever connections
        foreach (MazeNode node in oldNodes)
        {
            mazeBase.Remove(node);
            node.RemoveBaseConnectionByReference(node.AllNeighbors);
        }

        // Get 2 quadrants 
        if (nQuadrants == 2)
        {
            quadrants = new List<List<MazeNode>>(2);
            quadrants.Add(new List<MazeNode>(Mathf.RoundToInt(mazeBase.Count)));
            quadrants.Add(new List<MazeNode>(Mathf.RoundToInt(mazeBase.Count)));

            // Get the quadrants
            foreach (MazeNode node in mazeBase)
            {
                // quadrant 1
                if (node.Position.y > back.Position.y - tol)
                { quadrants[0].Add(node); }

                // quadrant 2
                if (node.Position.y < back.Position.y + tol)
                { quadrants[1].Add(node); }
            }

            foreach (List<MazeNode> quadrant in quadrants) { quadrant.TrimExcess(); }
        }

        // Get 4 quadrants 
        else if (nQuadrants == 4)
        {
            // Get the quadrants
            quadrants = new List<List<MazeNode>>(4);
            quadrants.Add(new List<MazeNode>(Mathf.RoundToInt(mazeBase.Count / 2)));
            quadrants.Add(new List<MazeNode>(Mathf.RoundToInt(mazeBase.Count / 2)));
            quadrants.Add(new List<MazeNode>(Mathf.RoundToInt(mazeBase.Count / 2)));
            quadrants.Add(new List<MazeNode>(Mathf.RoundToInt(mazeBase.Count / 2)));

            // get the quadrants
            foreach (MazeNode node in mazeBase)
            {
                // quadrant 1
                if (node.Position.x > back.Position.x - tol &&
                    node.Position.y > back.Position.y - tol)
                { quadrants[0].Add(node); }

                // quadrant 2
                if (node.Position.x > back.Position.x - tol &&
                    node.Position.y < back.Position.y + tol)
                { quadrants[1].Add(node); }

                // quadrant 3
                if (node.Position.x < back.Position.x + tol &&
                    node.Position.y < back.Position.y + tol)
                { quadrants[2].Add(node); }

                // quadrant 4
                if (node.Position.x < back.Position.x + tol &&
                    node.Position.y > back.Position.y - tol)
                { quadrants[3].Add(node); }
            }
            foreach (List<MazeNode> quadrant in quadrants) { quadrant.TrimExcess(); }
        }

        //// Remove certain base connections that fall on the border between quadrants 
        //// These are 4 sections:
        //// - the N connections upwards/downwards of the most front and the most back node (for nQuadrants = 2 and 4)
        //// - the N connections forwards/backwords of the most left and the most right node (for nQuadrants = 4)
        //// The N, per section, excluding the center node of each section, is 2^nDivisions
        //int halfNToRemove = Mathf.RoundToInt(Mathf.Pow(2, nDivisions) / 2);
        //MazeNode currentNode;
        //MazeNode targetNode;

        //// front/back sections
        //foreach (MazeNode baseNode in new List<MazeNode>(2) { front, back })
        //{
        //    // front/back and up/down
        //    for (int iUpDown = 0; iUpDown <= 1; iUpDown++)
        //    {
        //        currentNode = baseNode;
        //        targetNode = baseNode;
        //        for (int iN = 0; iN < halfNToRemove; iN++)
        //        {
        //            foreach (MazeNode neigh in currentNode.AllNeighbors)
        //            {
        //                switch (iUpDown)
        //                {
        //                    case 0: { if (neigh.Position.y > targetNode.Position.y) targetNode = neigh; break; }
        //                    case 1: { if (neigh.Position.y < targetNode.Position.y) targetNode = neigh; break; }
        //                }
        //            }
        //            currentNode.RemoveBaseConnectionByReference(targetNode);
        //            currentNode = targetNode;
        //        }
        //    }
        //}
        //// left/right sections
        //if (nQuadrants == 4)
        //{
        //    foreach (MazeNode baseNode in new List<MazeNode>(2) { left, right })
        //    {
        //        // front/back and up/down
        //        for (int iForwBackw = 0; iForwBackw <= 1; iForwBackw++)
        //        {
        //            currentNode = baseNode;
        //            targetNode = baseNode;
        //            for (int iN = 0; iN < halfNToRemove; iN++)
        //            {
        //                foreach (MazeNode neigh in currentNode.AllNeighbors)
        //                {
        //                    switch (iForwBackw)
        //                    {
        //                        case 0: { if (neigh.Position.z > targetNode.Position.z) targetNode = neigh; break; }
        //                        case 1: { if (neigh.Position.z < targetNode.Position.z) targetNode = neigh; break; }
        //                    }
        //                }
        //                currentNode.RemoveBaseConnectionByReference(targetNode);
        //                currentNode = targetNode;
        //            }
        //        }
        //    }
        //}

        return quadrants;
    }

}
