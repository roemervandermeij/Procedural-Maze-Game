using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class holding the gravity frame for a scene, representing an invisible frame 
/// inside all maze objects to pull the player towards.
/// </summary>
public class GravityFrameController
{
    /// <summary>
    /// Gets the closest point of player on the gravity frame.
    /// </summary>
    /// <value>Closest point of player on the gravity frame.</value>
    public Vector3 ClosestPointOnFrameLine => closestPointOnFrameLine;
    /// <summary>
    /// Gets the closest point of player on local plane of gravity frame.
    /// </summary>
    /// <value>Closest point of player on the gravity frame.</value>
    public Vector3 ClosestPointOnFramePlane => closestPointOnFramePlane;
    /// <summary>
    /// Gets a value indicating whether current closest frame position is near junction.
    /// </summary>
    /// <value><c>true</c> if near junction; otherwise, <c>false</c>.</value>
    public bool NearJunction => firstClosestNode.nearJunction;

    private Vector3 closestPointOnFrameLine;
    private Vector3 closestPointOnFramePlane;
    private GravityNode firstClosestNode;
    private GravityNode secondClosestNode;
    private List<GravityNode> gravityFrame;


    /// <summary>
    /// Initializes a new gravity frame from maze frame fitted splines.
    /// </summary>
    /// <param name="mazeFrame">Maze frame.</param>
    /// <param name="mazeFrameSplines">Maze frame fitted splines.</param>
    /// <param name="closebyDistFac"> Units of max(mazeFrame.Scale) at which nodes are considered close by.</param>
    /// <param name="junctionDistFac"> Units of max(mazeFrame.Scale) at which nodes are considered close to junction.</param>
    public GravityFrameController(MazeFrame mazeFrame, MazeFrameSplines mazeFrameSplines, float junctionDistFac, float planeSize)
    {
        //float currCloseByDist = mazeFrame.Scale.ComponentMax() * closebyDistFac;
        float currJunctionDist = mazeFrame.Scale.ComponentMax() * junctionDistFac;
        GenerateGravityFrameFromSplines(mazeFrame, mazeFrameSplines, currJunctionDist, planeSize);
    }


    /// <summary>
    /// Initialize base variables of gravity frame, to be used with player position 
    /// when player has entered the level.
    /// </summary>
    ///  <param name="playerPosition">Position of player.</param>
    public void Initialize(Vector3 playerPosition)
    {
        // initialize 
        firstClosestNode = FindFirstClosestNode(playerPosition);
        secondClosestNode = FindSecondClosestNode(playerPosition);
        closestPointOnFrameLine = FindClosestPointOnFrame(playerPosition);
        closestPointOnFramePlane = FindClosestPointOnFramePlane(playerPosition);
    }

    /// <summary>
    /// Updates the variables holding first/second closest nodes and 
    /// position on frame of the player.
    /// </summary>
    /// <param name="playerPosition">Position of player.</param>
    public void UpdatePlayerBasedElements(Vector3 playerPosition)
    {
        firstClosestNode = FindFirstClosestNode(playerPosition, firstClosestNode);
        secondClosestNode = FindSecondClosestNode(playerPosition);
        closestPointOnFrameLine = FindClosestPointOnFrame(playerPosition);
        closestPointOnFramePlane = FindClosestPointOnFramePlane(playerPosition);
        ////DEBUG
        //Debug.DrawLine(playerPosition, firstClosestNode.position, Color.gray);
        //Debug.DrawLine(playerPosition, secondClosestNode.position, Color.gray);
        //Debug.DrawLine(playerPosition, closestPointOnFrameLine, Color.yellow);
        //Debug.DrawLine(firstClosestNode.planePoints[0], secondClosestNode.planePoints[0], Color.magenta);
        //Debug.DrawLine(secondClosestNode.planePoints[0], secondClosestNode.planePoints[1], Color.magenta);
        //Debug.DrawLine(secondClosestNode.planePoints[1], firstClosestNode.planePoints[1], Color.magenta);
        //Debug.DrawLine(firstClosestNode.planePoints[1], firstClosestNode.planePoints[0], Color.magenta);
        //Debug.DrawLine(playerPosition, closestPointOnFramePlane, Color.green);
        ////DEBUG
    }

    /// <summary>
    /// Find the direction aligned with the frame that is close to <paramref name="direction"/>.
    /// Whether <paramref name="position"/> is near a dead end (ambigious direction) is controlled for. 
    /// </summary>
    /// <returns>Frame aligned direction vector.</returns>
    /// <param name="position">Position (on frame) at which to find frame aligned direction.</param>
    /// <param name="direction">Direction for which a close frame aligned direction should be found for.</param>
    public Vector3 FindFrameAlignedDirection(Vector3 position, Vector3 direction)
    {
        // Get directions from surrounding nodes
        Vector3 dir12 = (firstClosestNode.position - secondClosestNode.position).normalized;
        Vector3 dir21 = (secondClosestNode.position - firstClosestNode.position).normalized;
        // check whether position is not in between the two nodes (dead end), if so return same direction, if not, return new direction
        Vector3 dirOut;
        if (Vector3.Angle(position - firstClosestNode.position, position - secondClosestNode.position) <= 90) // really 180, but 90 to be safe
        { dirOut = direction; }
        else
        // Return the one with the smallest angle to direction
        { dirOut = Vector3.Angle(dir12, direction) < Vector3.Angle(dir21, direction) ? dir12 : dir21; }
        return dirOut;
    }

    /// <summary>
    /// Finds the closest point on frame of <paramref name="position"/>.
    /// </summary>
    /// <returns>Closest point on frame.</returns>
    /// <param name="position">Position to use for finding closest point.</param>
    private Vector3 FindClosestPointOnFrame(Vector3 position)
    {
        // Get closest point on grav line.
        Vector3 point1 = firstClosestNode.position;
        Vector3 point2 = secondClosestNode.position;
        return Utilities.FindClosestPointOnLineOfPositionWithClamping(point1, point2, position);
    }

    /// <summary>
    /// Finds the closest point on local plane of frame of <paramref name="position"/>.
    /// </summary>
    /// <returns>Closest point on frame plane.</returns>
    /// <param name="position">Position to use for finding closest point.</param>
    private Vector3 FindClosestPointOnFramePlane(Vector3 position)
    {
        // First, get current plane points as the average of those from the closest neighbors
        // transformed to directions.
        Vector3[] planePoints = new Vector3[2];
        for (int i = 0; i <= 1; i++)
        {
            planePoints[i] = ((firstClosestNode.planePoints[i] - firstClosestNode.position) + (secondClosestNode.planePoints[i] - secondClosestNode.position)) / 2;
            planePoints[i] += closestPointOnFrameLine;
        }
        // Then, find closest point on line defined by these two points
        return Utilities.FindClosestPointOnLineOfPositionWithClamping(planePoints[0], planePoints[1], position);
    }

    /// <summary>
    /// Finds node closest to <paramref name="position"/> to <paramref name="position"/> in full 
    /// frame, or among nodes in the closeby field of <paramref name="node"/>.
    /// </summary>
    /// <returns>Closest node on frame.</returns>
    /// <param name="position">Position for which to find the closest node.</param>
    /// <param name="node">Optional node with closeby field, use to reduce search space.</param>
    private GravityNode FindFirstClosestNode(Vector3 position, GravityNode node = null)
    {
        // Find closest gravNode to position, either in full frame or among node.closeby
        List<GravityNode> currFrame = node == null ? gravityFrame : node.closeby;
        List<float> dist = new List<float>(currFrame.Count);
        int count = 0;
        foreach (GravityNode gn in currFrame)
        {
            dist.Insert(count++, Vector3.Distance(position, gn.position));
        }
        int closestNodeInd = dist.IndexOf(Mathf.Min(dist.ToArray())); //
        return currFrame[closestNodeInd];
    }


    /// <summary>
    /// Finds gravity nodes surrounding <paramref name="position"/>.
    /// </summary>
    /// <returns>Surrounding nodes.</returns>
    /// <param name="position">Position for which surrounding nodes should be found.</param>
    private GravityNode FindSecondClosestNode(Vector3 position)
    {
        // Find second node by finding closest point (clamped) on line between closest node and its neighbors
        // The nodes whose line has the closest point to position, are the first and seconds closest nodes
        float[] distOfPointToPosition = new float[firstClosestNode.neighbors.Count];
        for (int i = 0; i < firstClosestNode.neighbors.Count; i++)
        {
            Vector3 point1 = firstClosestNode.position;
            Vector3 point2 = firstClosestNode.neighbors[i].position;
            Vector3 pointOnLine = Utilities.FindClosestPointOnLineOfPositionWithClamping(point1, point2, position);
            distOfPointToPosition[i] = Vector3.Distance(pointOnLine, position);
        }
        // Find neighor with which closest node has closest point on line to position
        return firstClosestNode.neighbors[System.Array.IndexOf(distOfPointToPosition, Mathf.Min(distOfPointToPosition))];
    }


    /// <summary>
    /// Class representing individual gravity frame node.
    /// </summary>
    private class GravityNode
    {
        public readonly Vector3 position;
        public readonly string identifier;
        public bool nearJunction;
        public List<GravityNode> neighbors;
        public List<GravityNode> closeby;
        public readonly Vector3[] planePoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="GravityFrameController.GravityNode"/> class.
        /// </summary>
        /// <param name="position">Position of gravity node.</param>
        /// <param name="identifier">Unique identifier for gravity node.</param>
        public GravityNode(Vector3 position, string identifier)
        {
            this.position = position;
            this.identifier = identifier;
            neighbors = new List<GravityNode>();
            closeby = new List<GravityNode>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GravityFrameController.GravityNode"/> class.
        /// </summary>
        /// <param name="position">Position of gravity node.</param>
        /// <param name="identifier">Unique identifier for gravity node.</param>
        /// <param name="planePoints">Points to be used with those of other node to define plane.</param>
        public GravityNode(Vector3 position, string identifier, Vector3[] planePoints)
        {
            this.position = position;
            this.identifier = identifier;
            neighbors = new List<GravityNode>();
            closeby = new List<GravityNode>();
            this.planePoints = planePoints;
        }
    }


    /// <summary>
    /// Draw gravity frame.
    /// </summary>
    public void DebugDrawGravityFrame()
    {
        for (int inode = 0; inode < gravityFrame.Count; inode++)
        {
            for (int ineigh = 0; ineigh < gravityFrame[inode].neighbors.Count; ineigh++)
            {
                Debug.DrawLine(gravityFrame[inode].position, gravityFrame[inode].neighbors[ineigh].position, Color.magenta);
            }

        }
        //Debug.Break();
    }


    /// <summary>
    /// Adds nodes that are close by (determined by <paramref name="maxDistance"/>) to allow efficient searching for closest node.
    /// </summary>
    /// <param name="maxDistance">Distance limit for when nodes should be considered closeby.</param>
    private void AddClosebyNodes(float maxDistance)
    {
        // Add nodes that are closeby based on distBound, and first add neighbors as well (and add self)
        foreach (GravityNode node in gravityFrame) { node.closeby.Add(node); foreach (GravityNode neighbor in node.neighbors) { node.closeby.Add(neighbor); } }
        for (int inode1 = 0; inode1 < gravityFrame.Count - 1; inode1++)
        {
            for (int inode2 = inode1 + 1; inode2 < gravityFrame.Count; inode2++)
            {
                if (Vector3.Distance(gravityFrame[inode1].position, gravityFrame[inode2].position) < maxDistance)
                {
                    if (!gravityFrame[inode1].neighbors.Contains(gravityFrame[inode2]))
                    {
                        gravityFrame[inode1].closeby.Add(gravityFrame[inode2]);
                        gravityFrame[inode2].closeby.Add(gravityFrame[inode1]);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Generate  gravity frame from splines fitted to maze frame.
    /// </summary>
    /// <param name="mazeFrameSplines">Splines fitted to maze frames.</param>
    ///// <param name="closebyDist"> Distance at which nodes are considered close by.</param>
    /// <param name="junctionDist"> Distance at which nodes are considered close to junction.</param>
    private void GenerateGravityFrameFromSplines(MazeFrame mazeFrame, MazeFrameSplines mazeFrameSplines, float junctionDist, float planeWidth)
    {
        // Generate gravity frame based on maze frame fitted splines
        int nodeCount = 0;
        foreach (MazeFrameSplines.SplineSegment splineSeg in mazeFrameSplines.SplineSegments)
        { nodeCount += splineSeg.spline.NSamplesToUse; }
        gravityFrame = new List<GravityNode>(nodeCount);

        // Generate sub frame to use later
        List<List<GravityNode>> segmentSubFrame = new List<List<GravityNode>>(mazeFrameSplines.SplineSegments.Count);

        // Create nodes for each splines
        for (int iSpline = 0; iSpline < mazeFrameSplines.SplineSegments.Count; iSpline++)
        {
            segmentSubFrame.Add(new List<GravityNode>(mazeFrameSplines.SplineSegments[iSpline].spline.NSamplesToUse));
            GravityNode prevNode = null;
            for (int i = 0; i < mazeFrameSplines.SplineSegments[iSpline].spline.NSamplesToUse; i++)
            {
                Vector3 currPos = mazeFrameSplines.SplineSegments[iSpline].spline.GetPointOnSpline(mazeFrameSplines.SplineSegments[iSpline].spline.SampleIndToT(i));
                Vector3 currTang = mazeFrameSplines.SplineSegments[iSpline].spline.GetTangentToPointOnSpline(mazeFrameSplines.SplineSegments[iSpline].spline.SampleIndToT(i)).normalized;
                Vector3 currNorm = mazeFrameSplines.SplineSegments[iSpline].spline.DefaultGetNormalAtT(mazeFrameSplines.SplineSegments[iSpline].spline.SampleIndToT(i)).normalized;
                Vector3 cross = Vector3.Cross(currTang, currNorm);
                Vector3[] planePoints = new Vector3[2] { currPos + (cross * (planeWidth / 2f)), currPos - (cross * (planeWidth / 2f)) };
                GravityNode node = new GravityNode(currPos, iSpline + "-" + currPos.ToString(), planePoints);
                if (prevNode != null)
                {
                    node.neighbors.Add(prevNode);
                    prevNode.neighbors.Add(node);
                }
                // Determine if near junction
                if (mazeFrameSplines.SplineSegments[iSpline].startNeighbors.Count > 0)
                {
                    if (Vector3.Distance(currPos, mazeFrameSplines.SplineSegments[iSpline].spline.GetPointOnSpline(0)) < junctionDist)
                    { node.nearJunction = true; }
                }
                if (mazeFrameSplines.SplineSegments[iSpline].endNeighbors.Count > 0)
                {
                    if (Vector3.Distance(currPos, mazeFrameSplines.SplineSegments[iSpline].spline.GetPointOnSpline(1)) < junctionDist)
                    { node.nearJunction = true; }
                }
                // Add to frame
                gravityFrame.Add(node);
                prevNode = node;
                // Add spline segment sub-frame as well
                segmentSubFrame[iSpline].Add(node);
            }
        }
        // Create closeby list from, and connect, neighboring splines
        for (int iSeg = 0; iSeg < mazeFrameSplines.SplineSegments.Count; iSeg++)
        {
            // Add frame to its own closeby
            List<GravityNode> currSubFrame = segmentSubFrame[iSeg];
            foreach (GravityNode node in currSubFrame)
            {
                node.closeby.AddRange(currSubFrame);
            }

            // Start/end of spline
            List<List<MazeFrameSplines.SplineSegment>> segList = new List<List<MazeFrameSplines.SplineSegment>>(2)
            { mazeFrameSplines.SplineSegments[iSeg].startNeighbors,mazeFrameSplines.SplineSegments[iSeg].endNeighbors };
            for (int iStartEnd = 0; iStartEnd <= 1; iStartEnd++)
            {
                foreach (MazeFrameSplines.SplineSegment neighSeg in segList[iStartEnd])
                {
                    // Get sub frame index
                    List<GravityNode> neighSegSF = segmentSubFrame[mazeFrameSplines.SplineSegments.IndexOf(neighSeg)];

                    // Add to closeby list
                    foreach (GravityNode node in currSubFrame)
                    { node.closeby.AddRange(neighSegSF); }

                    // Connect start/end
                    switch (iStartEnd)
                    {
                        case 0:
                            {
                                if (Vector3.Distance(currSubFrame[0].position, neighSegSF[0].position) <
                                    Vector3.Distance(currSubFrame[0].position, neighSegSF[neighSegSF.Count - 1].position))
                                { currSubFrame[0].neighbors.Add(neighSegSF[0]); }
                                else
                                { currSubFrame[0].neighbors.Add(neighSegSF[neighSegSF.Count - 1]); }
                                break;
                            }
                        case 1:
                            {
                                if (Vector3.Distance(currSubFrame[currSubFrame.Count - 1].position, neighSegSF[0].position) <
                                    Vector3.Distance(currSubFrame[currSubFrame.Count - 1].position, neighSegSF[neighSegSF.Count - 1].position))
                                { currSubFrame[currSubFrame.Count - 1].neighbors.Add(neighSegSF[0]); }
                                else
                                { currSubFrame[currSubFrame.Count - 1].neighbors.Add(neighSegSF[neighSegSF.Count - 1]); }
                                break;
                            }
                    }
                }
            }
        }
    }



    /// <summary>
    /// Generate gravity frame from maze frame object, should only be used when paths between nodes are perfectly straight.
    /// </summary>
    /// <param name="mazeFrame">MazeFrame object.</param>
    private void GenerateGravityFrameFromMazeObj(MazeFrame mazeFrame, float closebyDist)
    {
        // Generate gravity frame based on maze nodes
        gravityFrame = new List<GravityNode>(mazeFrame.Nodes.Count);

        // First create base list, then add neighbors
        foreach (MazeNode mazeNode in mazeFrame.Nodes)
        {
            // Create gravity node from maze node
            GravityNode gravNode = new GravityNode(mazeNode.Position, mazeNode.Identifier);
            gravityFrame.Add(gravNode);
        }
        for (int i = 0; i < gravityFrame.Count; i++)
        {
            foreach (MazeNode neighbor in mazeFrame.Nodes[i].ConnectedNeighbors)
            { gravityFrame[i].neighbors.Add(gravityFrame.Find(x => x.identifier == neighbor.Identifier)); }
        }

        // Add closeby list to each node
        AddClosebyNodes(closebyDist);
    }


    /// <summary>
    /// Generate gravity frame from object center line nodes (the output given by <see cref="MazePopulator.PopulateWithCylinders(MazeFrame, float, int)"/>.
    /// </summary>
    /// <param name="objCenterLineNodes">Object center line nodes, given as output from <see cref="MazePopulator.PopulateWithCylinders(MazeFrame, float, int)"/>.</param>
    private void GenerateGravityFrameFromObjCenterLineNodes(Dictionary<string, Vector3> objCenterLineNodes, float closebyDist) // FIXME name parsing below is too complicated, should be easier way
    {
        // Generate gravity frame based on maze nodes
        gravityFrame = new List<GravityNode>(objCenterLineNodes.Count);

        // First create base list with shortened names and separate neighbor dictionary
        Dictionary<string, List<string>> neighbors = new Dictionary<string, List<string>>(objCenterLineNodes.Count);
        foreach (KeyValuePair<string, Vector3> node in objCenterLineNodes)
        {
            // Create gravity node 
            GravityNode gravNode = new GravityNode(node.Value, node.Key);
            gravityFrame.Add(gravNode);

            //Parse identifier
            string[] identifier = node.Key.Split(new char[] { '-' }, System.StringSplitOptions.RemoveEmptyEntries);
            if ((identifier[0][0] == 'c') && identifier.Length > 6) { throw new System.Exception("Node naming in PopulateMaze is incorrect."); }
            if ((identifier[0][0] == 'b') && identifier.Length > 3) { throw new System.Exception("Node naming in PopulateMaze is incorrect."); }

            // Generate neighbor identifiers and add to dictionary
            // The naming schemes exist, with specified neighbors:
            // conn-<center_nodeid>-<start_neighid>-<end_neighid>-<num>-<index==0>
            //            --> base-<center_nodeid>-<start_neighid>
            // conn-<center_nodeid>-<start_neighid>-<end_neighid>-<num>-<index==num>
            //            --> base-<center_nodeid>-<end_neighid>
            // conn-<center_nodeid>-<start_neighid>-<end_neighid>-<num>-<index!=0/num>
            //            --> conn-<center_nodeid>-<start_neighid>-<end_neighid>-<num>-<index-1>
            //            --> conn-<center_nodeid>-<start_neighid>-<end_neighid>-<num>-<index+1>
            // base-<nodeid>-<neighid>
            //            --> base-<neighid>-<nodeid>
            // (if exists)--> conn-<nodeid>-<neighid>-<ANY>
            //           
            List<string> currNeighbors = new List<string>();
            if (identifier[0][0] == 'b') // isBase
            {
                currNeighbors.Add("base" + "-" + identifier[2] + "-" + identifier[1]); // NEEDS TO ADDED INDIVIDUALLY
            }
            else if (identifier[0][0] == 'c') // isConn
            {
                int curveNum = System.Int32.Parse(identifier[4]);
                int curveInd = System.Int32.Parse(identifier[5]);
                if ((curveInd > (curveNum - 1)) || curveNum < 2) { throw new System.Exception("Node naming in PopulateMaze is incorrect."); }
                if (curveInd == 0)
                {
                    currNeighbors.Add("base" + "-" + identifier[1] + "-" + identifier[2]); // NEEDS TO ADDED RECIPROCALLY (as the base doesn't if it has a conn) // FIXME reciprocal, or..., using maze for neighbor def
                    currNeighbors.Add("conn" + "-" + identifier[1] + "-" + identifier[2] + "-" + identifier[3] + "-" + identifier[4] + "-" + (1));
                }
                else if (curveInd == (curveNum - 1))
                {
                    currNeighbors.Add("base" + "-" + identifier[1] + "-" + identifier[3]);  // NEEDS TO ADDED RECIPROCALLY (as the base doesn't if it has a conn)
                    currNeighbors.Add("conn" + "-" + identifier[1] + "-" + identifier[2] + "-" + identifier[3] + "-" + identifier[4] + "-" + (curveNum - 2));
                }
                else
                {
                    currNeighbors.Add("conn" + "-" + identifier[1] + "-" + identifier[2] + "-" + identifier[3] + "-" + identifier[4] + "-" + (curveInd - 1));
                    currNeighbors.Add("conn" + "-" + identifier[1] + "-" + identifier[2] + "-" + identifier[3] + "-" + identifier[4] + "-" + (curveInd + 1));
                }

            }
            else { throw new System.Exception("Node naming in PopulateMaze is incorrect."); }
            neighbors.Add(node.Key, currNeighbors);
        }
        // Add neighbors to gravity frame nodes
        foreach (GravityNode node in gravityFrame)
        {
            if (node.identifier[0] == 'b') // isBase
            {
                foreach (string currneighbor in neighbors[node.identifier])
                { node.neighbors.Add(gravityFrame.Find(x => x.identifier == currneighbor)); }
            }
            if (node.identifier[0] == 'c') // isConn
            {
                foreach (string currneighbor in neighbors[node.identifier])
                {
                    node.neighbors.Add(gravityFrame.Find(x => x.identifier == currneighbor));
                }
                foreach (GravityNode nb in node.neighbors)
                {
                    if (nb.identifier[0] == 'b') { nb.neighbors.Add(node); } // RECIPROCAL ADDING FOR BASE TO CONN // FIXME could also do with an 'if exists' type of check
                }
            }
        }
        // FIXME not all close nodes are merged
        //// Combine nodes at (nearly) identical positions
        //Stack<GravityNode> toRemove = new Stack<GravityNode>((int)Mathf.Round(gravityFrame.Count / 10f)); // guess...
        //for (int inode1 = 0; inode1 < gravityFrame.Count - 1; inode1++)
        //{
        //    for (int inode2 = inode1 + 1; inode2 < gravityFrame.Count; inode2++)
        //    {
        //        if ((Vector3.Distance(gravityFrame[inode1].position, gravityFrame[inode2].position) < 0.1f) && !toRemove.Contains(gravityFrame[inode2]))
        //        {
        //            // For each neighbor of node2:
        //            // - remove node2 from neighbor.neighbors
        //            // - add neighbor to node1.neighbors (if not already neighbors)
        //            // - add node1 to neigbor.neighbors (if not already neighbors)
        //            foreach (GravityNode neighborlvl2 in gravityFrame[inode2].neighbors)
        //            {
        //                neighborlvl2.neighbors.Remove(gravityFrame[inode2]);
        //                if (!gravityFrame[inode1].neighbors.Contains(neighborlvl2))
        //                {
        //                    gravityFrame[inode1].neighbors.Add(neighborlvl2);
        //                    neighborlvl2.neighbors.Add(gravityFrame[inode1]);
        //                }
        //            }
        //            // Then, remove node2 from node1 and from frame
        //            toRemove.Push(gravityFrame[inode2]);
        //            if (gravityFrame[inode1].neighbors.Contains(gravityFrame[inode2])) { gravityFrame[inode1].neighbors.Remove(gravityFrame[inode2]); }
        //        }
        //    }
        //}
        //while (toRemove.Count > 0)
        //{
        //    GravityNode currNode = toRemove.Pop();
        //    gravityFrame.Remove(currNode);
        //}

        // Add closeby list to each node
        AddClosebyNodes(closebyDist);
    }

}
