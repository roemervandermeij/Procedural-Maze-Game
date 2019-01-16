using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parent class containing methods used in maze frame creation. 
/// </summary>
public abstract class MazeFrameCreator
{

    protected float jitter = 0f;
    /// <summary>
    /// Gets or sets the random jitter applied to maze frame nodes, clamped between 0-0.5.
    /// </summary>
    /// <value>The jitter.</value>
    public float Jitter
    {
        get { return jitter; }
        set { jitter = Mathf.Clamp(value, 0f, 0.5f); }
    }

    /// <summary>
    /// Gets or sets the scale of the maze frame, with which each node's position will be multiplied.
    /// </summary>
    /// <value>The scale.</value>
    public Vector3 Scale { get; set; } = new Vector3(1, 1, 1);

    protected int randomSeed;
    /// <summary>
    /// Random seed used during maze frame creation.
    /// </summary>
    /// <value>Random seed.</value>
    public int RandomSeed
    {
        get { return randomSeed; }
        set
        {
            randomSeed = value;
            mazeRandGen = new ConsistentRandom(randomSeed);
        }
    }

    protected ConsistentRandom mazeRandGen;
    protected ConsistentRandom randGen;


    /// <summary>
    /// Generate maze from maze frame.
    /// </summary>
    /// <returns>A maze object containing the maze.</returns>
    /// <param name="mazeFrame">Maze frame.</param>
    public void GenerateMaze(ref MazeFrame mazeFrame)
    {
        // Build maze 
        //BuildMazeFrameUsingRecursiveBacktracking(ref mazeFrame);
        BuildMazeFrameUsingHuntAndKill(ref mazeFrame, 50, 0.50f);

        // Label all nodes 
        LabelNodesWRTPath(ref mazeFrame);
    }

    /// <summary>
    /// Generate maze from maze frame, inside quadrant specified by <paramref name="quadrantInd"/>.
    /// </summary>
    /// <returns>A maze object containing the maze.</returns>
    /// <param name="mazeFrame">Maze frame.</param>
    /// <param name="quadrantInd">Index of quadrant in which to build maze.</param>
    public void GenerateMaze(ref MazeFrame mazeFrame, int quadrantInd)
    {
        // Active quadrant
        mazeFrame.SetActiveQuadrant(quadrantInd);

        // Build maze and label
        GenerateMaze(ref mazeFrame);

        // Reactivate all nodes
        mazeFrame.ActivateAllNodes();
    }

    /// <summary>
    /// Method to generate maze.
    /// </summary>
    /// <returns>The maze.</returns>
    public MazeFrame GenerateMaze()
    {
        // Initialize mazeFrame 
        MazeFrame mazeFrame = GenerateEmptyMazeFrame();

        // Build maze and label
        GenerateMaze(ref mazeFrame);

        return mazeFrame;
    }

    /// <summary>
    /// Method to generate empty maze frame
    /// </summary>
    /// <returns>The empty maze frame.</returns>
    public abstract MazeFrame GenerateEmptyMazeFrame();

    /// <summary>
    /// Gets the minimum path length based on size.
    /// </summary>
    /// <returns>The minimum path length.</returns>
    protected abstract int GetMinPathLength();

    /// <summary>
    /// Gets the maximum path length based on size.
    /// </summary>
    /// <returns>The max path length.</returns>
    protected abstract int GetMaxPathLength();

    /// <summary>
    /// Sets the start and end nodes.
    /// </summary>
    /// <param name="mazeBase">Maze base.</param>
    protected abstract void SetStartAndEndNodes(ref List<MazeNode> mazeBase);

    /// <summary>
    /// Get the quadrant specification
    /// </summary>
    /// <returns>The quadrants.</returns>
    protected abstract List<List<MazeNode>> GetQuadrants(List<MazeNode> mazeBase);

    /// <summary>
    /// /// Build connections inside maze frame using recursive backtracking algorithm.
    /// </summary>
    /// <param name="mazeFrame">Maze frame object.</param>
    protected void BuildMazeFrameUsingRecursiveBacktracking(ref MazeFrame mazeFrame)
    {
        if (mazeRandGen == null) { mazeRandGen = new ConsistentRandom(); }
        if (randGen == null) { randGen = new ConsistentRandom(); }

        // Build maze using recursive backtracking
        // From wikipedia:
        /* 1) Make the initial node the current node and mark it as visited
         * 2) While there are unvisited nodes
                1) If the current node has any neighbours which have not been visited
                    1) Choose randomly one of the unvisited neighbours
                    2) Push the current node to the stack
                    3) Remove the wall between the current node and the chosen node
                    4) Make the chosen node the current node and mark it as visited
                2) Else if stack is not empty
                    1) Pop a node from the stack
                    2) Make it the current node
        */

        // Check start/end node active status
        if (!mazeFrame.StartNode.IsActive || !mazeFrame.EndNode.IsActive)
        { throw new System.Exception("Start/end nodes are required to be active."); }

        // Set storage of visit flags and create stack for visisted nodes
        int nNodesVisited = 0;
        Dictionary<string, bool> isVisited = new Dictionary<string, bool>(mazeFrame.Nodes.Count);
        foreach (MazeNode nd in mazeFrame.Nodes)
        {
            if (nd.IsActive) { isVisited.Add(nd.Identifier, false); }
            else { isVisited.Add(nd.Identifier, true); nNodesVisited++; }
        }
        Stack<MazeNode> nodeTrack = new Stack<MazeNode>(mazeFrame.Nodes.Count); // FIXME does preallocating help performance here?

        // Ignore nodes without neighbors
        foreach (MazeNode nd in mazeFrame.Nodes)
        { if (nd.AllNeighbors.Count == 0) { isVisited[nd.Identifier] = true; nNodesVisited++; } }

        //Initialize near startNode
        MazeNode currentNode = mazeFrame.StartNode;
        isVisited[currentNode.Identifier] = true;
        nNodesVisited++;
        while (nNodesVisited < mazeFrame.Nodes.Count)
        {
            // See if there are unvisited neighbors
            List<MazeNode> unvisitedNeighbors = new List<MazeNode>();
            foreach (MazeNode nb in currentNode.AllNeighbors)
            { if (!isVisited[nb.Identifier]) { unvisitedNeighbors.Add(nb); } }

            if (unvisitedNeighbors.Count != 0)
            {
                // Add current node to stack (1)
                nodeTrack.Push(currentNode);
                // Select random neighbor (2)
                //MazeNode selNode = RandomlySelectNodeWithBiasTowardsForwards(currentNode, unvisitedNeighbors);
                //Node selNode = RandomlySelectNodeWithBiasTowardsNode(currentNode, unvisitedNeighbors, endNode.AllNeighbors[0], 2,mazeRandGen);
                MazeNode selNode = unvisitedNeighbors[mazeRandGen.Next(unvisitedNeighbors.Count)];
                // Add as connected neighbor to current node object(3) and vice versa
                currentNode.AddConnectionByReference(selNode);
                // Switch to new node(4) and record visit
                currentNode = selNode;
                isVisited[currentNode.Identifier] = true;
                nNodesVisited++;
            }
            else if ((unvisitedNeighbors.Count == 0) && (nodeTrack.Count != 0))
            {
                currentNode = nodeTrack.Pop();
            }

            // Safety check
            if ((nodeTrack.Count == 0) && (nNodesVisited > mazeFrame.Nodes.Count))
            { throw new System.Exception("Recursive backtracking loop did not terminate correctly."); }
        }

        // Sanity check
        foreach (MazeNode nb in mazeFrame.Nodes)
        { if (isVisited[nb.Identifier] == false) { throw new System.Exception("Recursive backtracking loop did not terminate correctly."); } }
    }

    /// <summary>
    /// Build connections inside maze frame using hunt and kill algorithm.
    /// </summary>
    /// <param name="mazeFrame">Maze frame object.</param>
    /// <param name="huntAfterSegmentLength">Enter hunting mode after segment has grown to this length in world units, 
    /// defaults to unlimited (uses smalles Scale value.</param>
    /// <param name="huntProbability">Probability of starting hunt when it arrives, defaults to 0.50.</param>
    protected void BuildMazeFrameUsingHuntAndKill(ref MazeFrame mazeFrame, float huntAfterSegmentLength = float.PositiveInfinity, float huntProbability = 0.50f)
    {
        if (mazeRandGen == null) { mazeRandGen = new ConsistentRandom(); }
        if (randGen == null) { randGen = new ConsistentRandom(); }

        // Build maze using hunt and kill
        /* 1) Make the initial node the current node and mark it as visited
         * 2) While there are unvisited nodes
                1) If the current node has any neighbours which have not been visited
                    1) Choose randomly one of the unvisited neighbours
                    2) Remove the wall between the current node and the chosen node
                    3) Make the chosen node the current node and mark it as visited and add to targets
                2) Find random unvisited node next to visited node
                    1) Make it the current node and mark it as visited and add to targets
        */

        // Check start/end node active status
        if (!mazeFrame.StartNode.IsActive || !mazeFrame.EndNode.IsActive)
        { throw new System.Exception("Start/end nodes are required to be active."); }

        // Set storage of visit flags and create list for hunt to target
        int nNodesVisited = 0;
        Dictionary<string, bool> isVisited = new Dictionary<string, bool>(mazeFrame.Nodes.Count);
        foreach (MazeNode nd in mazeFrame.Nodes)
        {
            if (nd.IsActive) { isVisited.Add(nd.Identifier, false); }
            else { isVisited.Add(nd.Identifier, true); nNodesVisited++; }
        }
        List<MazeNode> targets = new List<MazeNode>(Mathf.RoundToInt(mazeFrame.Nodes.Count / 2f)); // half of full maze should be more than enough

        // Ignore nodes without neighbors
        foreach (MazeNode nd in mazeFrame.Nodes)
        { if (nd.AllNeighbors.Count == 0) { isVisited[nd.Identifier] = true; nNodesVisited++; } }

        // Determine huntAfterNConnections
        int huntAfterNConnections = Mathf.RoundToInt(huntAfterSegmentLength / Scale.ComponentMin());

        //Initialize near startNode
        MazeNode currentNode = mazeFrame.StartNode;
        isVisited[currentNode.Identifier] = true;
        nNodesVisited++;
        targets.Add(currentNode);
        int count = 0;
        int nConnectionsMade = 0;
        while (nNodesVisited < mazeFrame.Nodes.Count)
        {
            // See if there are unvisited neighbors
            List<MazeNode> unvisitedNeighbors = new List<MazeNode>();
            foreach (MazeNode nb in currentNode.AllNeighbors)
            { if (!isVisited[nb.Identifier]) { unvisitedNeighbors.Add(nb); } }

            if (nConnectionsMade >= huntAfterNConnections)
            { if (mazeRandGen.NextDouble() < huntProbability) { nConnectionsMade = 0; } }
            if (unvisitedNeighbors.Count > 0 && nConnectionsMade < huntAfterNConnections)
            {
                // Select random neighbor (2)
                MazeNode selNode = unvisitedNeighbors[mazeRandGen.Next(unvisitedNeighbors.Count)];
                // Add as connected neighbor to current node object(3) and vice versa
                currentNode.AddConnectionByReference(selNode);
                // Switch to new node(4) and record visit
                currentNode = selNode;
                isVisited[currentNode.Identifier] = true;
                nNodesVisited++;
                targets.Add(currentNode);
                nConnectionsMade++;
            }
            else
            {
                // Find random unvisited node neighboring a visited node
                List<int> targetInd = new List<int>(targets.Count);
                for (int i = 0; i < targets.Count; i++) { targetInd.Insert(i, i); }
                bool found = false;
                Stack<MazeNode> targetsToRemove = new Stack<MazeNode>();
                int huntCount = 0;
                while (!found)
                {
                    // sample a random target node
                    int randInd = targetInd[mazeRandGen.Next(targetInd.Count)];
                    MazeNode currTarget = targets[randInd];
                    // See if target has unvisited neighbors
                    unvisitedNeighbors = new List<MazeNode>();
                    foreach (MazeNode nb in currTarget.AllNeighbors)
                    { if (!isVisited[nb.Identifier]) { unvisitedNeighbors.Add(nb); } }
                    // if it has no unvisited neighors, remove target from list and go again, otherwise, pick random unvisited neighbor
                    if (unvisitedNeighbors.Count == 0)
                    {
                        targetInd.Remove(randInd);
                        targetsToRemove.Push(currTarget);
                    }
                    else
                    {
                        // Select random neighbor and set as currentNode
                        currentNode = currTarget;
                        found = true;
                    }
                    // Reset connections counter
                    nConnectionsMade = 0;

                    // Safety check
                    huntCount++;
                    if (huntCount > targets.Count) { throw new System.Exception("Hunt did not terminate correctly."); }
                }
                // Remove nodes from targets that had no unvisisted neigbors
                while (targetsToRemove.Count > 0) { targets.Remove(targetsToRemove.Pop()); }
            }

            // Safety check
            count++;
            if (targets.Count == 0 || count > Mathf.Pow(mazeFrame.Nodes.Count, 2)) { throw new System.Exception("Hunt and kill loop did not terminate correctly."); }
        }

        // Sanity check
        foreach (MazeNode nb in mazeFrame.Nodes)
        { if (!isVisited[nb.Identifier]) { throw new System.Exception("Hunt and kill loop did not terminate correctly."); } }

    }

    /// <summary>
    /// Label all nodes according to whether they are (1) on the shortest path, (2) are on a loop to the shortest path,
    /// (3) are on a path resulting in a dead end, and (4) are not connected to the maze. 
    /// All labels are inclusive, i.e. the starting point of a dead end is a node labeled as on the shorted path.
    /// The shortest path is found using Dijkstra's algorithm.
    /// </summary>
    /// <returns>List of nodes with path labels.</returns>
    /// <param name="mazeFrame">List of nodes.</param>
    protected void LabelNodesWRTPath(ref MazeFrame mazeFrame)
    {
        //
        // This function labels each node according to the following booleans:
        //
        // onShortestPath - whether node is on the path found using psuedo Dijkstra's method
        // onLoop - whether node is on path that diverges from the shortest path, but returns to it eventually
        // onDeadEnd - whether the node is on a dead end, defined as a path that (1) diverges from the shortest path and doesn't return
        // notConnectedToPath - whether node has a connection on path or is superfluous visual filler
        // (junction nodes have more than one label)

        // Find shortest path from beginning to end using Dijkstra's algorithm
        // From wikipedia/other:
        /* 
         * 1) Mark all nodes unvisited, mark selected initial node with a current distance of 0 and the rest with infinity. 
         * 2) Set the non-visited node with the smallest current distance as the current node C.
         * 3) For each neighbour N of current node C: add the current distance of C with the weight of the edge 
         *    connecting C-N. If it's smaller than the current distance of N, set it as the new current distance of N.
         * 4) Mark the current node C as visited.
         * 5) If end not touched yet, and if there are non-visited nodes, go to step 2.
        */

        // Check start/end node active status
        if (!mazeFrame.StartNode.IsActive || !mazeFrame.EndNode.IsActive)
        { throw new System.Exception("Start/end nodes are required to be active."); }

        // Prep storage of labels and initilaze as false
        int activeNodeCount = 0;
        foreach (MazeNode nd in mazeFrame.Nodes) { if (nd.IsActive) { activeNodeCount++; } }
        Dictionary<string, bool> onShortestPath = new Dictionary<string, bool>(activeNodeCount);
        Dictionary<string, bool> onDeadEnd = new Dictionary<string, bool>(activeNodeCount);
        Dictionary<string, bool> onLoop = new Dictionary<string, bool>(activeNodeCount);
        Dictionary<string, bool> notConnectedToPath = new Dictionary<string, bool>(activeNodeCount);
        foreach (MazeNode nd in mazeFrame.Nodes)
        {
            if (nd.IsActive)
            {
                onShortestPath.Add(nd.Identifier, false);
                onDeadEnd.Add(nd.Identifier, false);
                onLoop.Add(nd.Identifier, false);
                notConnectedToPath.Add(nd.Identifier, false);
            }
        }

        // Find start and end node
        MazeNode startNode = mazeFrame.StartNode;
        MazeNode endNode = mazeFrame.EndNode;

        // Create bookkeeping for visits and set all nodes to unvisitied 
        Dictionary<string, bool> isVisited = new Dictionary<string, bool>(mazeFrame.Nodes.Count);
        foreach (MazeNode nd in mazeFrame.Nodes)
        {
            if (nd.IsActive) { isVisited.Add(nd.Identifier, false); }
            else { isVisited.Add(nd.Identifier, true); }
        }

        // Ignore nodes without neighbors
        foreach (MazeNode nd in mazeFrame.Nodes)
        { if (nd.ConnectedNeighbors.Count == 0) { isVisited[nd.Identifier] = true; } }

        // Set distance to start, and initialize at infinity
        Dictionary<string, float> distToStart = new Dictionary<string, float>(activeNodeCount);
        foreach (MazeNode nd in mazeFrame.Nodes)
        { if (nd.IsActive) { distToStart.Add(nd.Identifier, float.PositiveInfinity); } }

        // Start search
        MazeNode currentNode;
        distToStart[startNode.Identifier] = 0;
        int count = 0;
        bool endFound = false;
        while (!endFound)
        {
            // Set current unvisited node based on distance to start
            int currentNodeInd = -1;
            float lastDist = float.PositiveInfinity;
            for (int i = 0; i < mazeFrame.Nodes.Count; i++)
            {
                if (!isVisited[mazeFrame.Nodes[i].Identifier])
                {
                    float currDist = distToStart[mazeFrame.Nodes[i].Identifier];
                    if (currDist <= lastDist) { currentNodeInd = i; lastDist = currDist; }
                }
            }
            currentNode = mazeFrame.Nodes[currentNodeInd];

            // Find unvisited neighbors of current node
            List<MazeNode> unvisitedNeighbors = new List<MazeNode>();
            foreach (MazeNode nb in currentNode.ConnectedNeighbors)
            { if (isVisited[nb.Identifier] == false) { unvisitedNeighbors.Add(nb); } }

            // For each neighbor, set distToStart as min of (currentNode disttostart + dist to current) and neighbor disttostart
            foreach (MazeNode nb in unvisitedNeighbors)
            {
                // Distance in case of non-equidistant nodes
                //float distToCurrentNode = Vector3.Distance(currentNode.position, nb.position);
                // Distance in case of equidistant nodes
                float distToCurrentNode = 1;
                // Update neighbor
                distToStart[nb.Identifier] = Mathf.Min((distToStart[currentNode.Identifier] + distToCurrentNode), distToStart[nb.Identifier]);
            }

            // Set current node to visisted
            isVisited[currentNode.Identifier] = true;

            // end reached?
            endFound |= currentNode == endNode;

            // Safety check
            count++;
            if (count > mazeFrame.Nodes.Count) { throw new System.Exception("Finding shortest path loop did not terminate correctly."); }
        }

        // Store shortest path length in maze frame now that we know it
        int shortestPathLength = (int)distToStart[endNode.Identifier];

        // Backtrack along distToStart and label each node as being on the shortest path
        currentNode = endNode;
        onShortestPath[currentNode.Identifier] = true;
        count = 0;
        while (currentNode != startNode)
        {
            // Find neighbor with shortest distance to start
            MazeNode nextNodeOnPath = currentNode;
            foreach (MazeNode nb in currentNode.ConnectedNeighbors)
            {
                if (nb.IsActive && distToStart[nb.Identifier] < distToStart[nextNodeOnPath.Identifier]) // There is ALWAYS a node closer than the current one
                { nextNodeOnPath = nb; }
            }

            // Update and continue
            currentNode = nextNodeOnPath;
            onShortestPath[currentNode.Identifier] = true;

            // Safety check
            count++;
            if (count > mazeFrame.Nodes.Count) { throw new System.Exception("Finding shortest path loop did not terminate correctly."); }
        }



        /* Label each node as onLoop/onDeadEnd/notConnectedToPath as follows:
         * 
         * 1) Find all nodes that are adjacent to the path
         * 2) For each of these:
         *   3) Create stack to keep unlabeled nodes, and set allLabeled flag
         *   4) While !allLabeled
         *      5) Create list of unlabeled neighbors of current node that are not on the stack
         *      6) If current node has 1+ unlabeled neighbors --> explore
         *         7) Add current node to stack and set an unlabeled neighbor not on the stack as current node
         *      8) If current node has 0 unlabeled neighbors not on the stack --> label and backtrack
         *         9) If current node has only 1 connected neighbor --> onDeadEnd
         *        10) If more than one connected neighbors, and one was onLoop --> onLoop
         *        11) If more than one connected neighbors, and one of them was onShortestPath (or two if at seed node) --> onLoop (loop connection found!)
         *        12) If more than one connected neighbors, and no onLoop/onShortestPath --> onDeadEnd (backtracking from only dead ends)
         *     13) If stack is not empty, pop node and set as current node, go to 5)
         *     14) If stack is empty, all nodes are labeled, set allLabeled to true;
         *      
         * 15) Set all junctions by finding all nodes adjacent to onDeadEnd/onLoop, and set them likewise. 
         * 16) Set all unlabeled nodes to notConnectedToPath
         */

        // First, find unlabeled nodes that are connected to the path (1)
        List<MazeNode> unlabeledSeedNode = new List<MazeNode>();
        for (int i = 0; i < mazeFrame.Nodes.Count; i++)
        {
            if (mazeFrame.Nodes[i].IsActive && !onShortestPath[mazeFrame.Nodes[i].Identifier])
            {
                foreach (MazeNode nb in mazeFrame.Nodes[i].ConnectedNeighbors)
                { if (nb.IsActive && onShortestPath[nb.Identifier]) { unlabeledSeedNode.Add(mazeFrame.Nodes[i]); break; } }
            }
        }

        // Start the search (2)
        for (int inode = 0; inode < unlabeledSeedNode.Count; inode++)
        {
            // set seed node
            MazeNode seedNode = unlabeledSeedNode[inode];
            if (onDeadEnd[seedNode.Identifier] || onLoop[seedNode.Identifier]) // check whether node was touched from a previous cycle below
            { continue; }

            // Create stack for to be labeled nodes
            Stack<MazeNode> nodesToLabel = new Stack<MazeNode>(); // FIXME should I initialize with conservative estimate?

            // Search from the starting node until the stack is empty (all are labeled) (4)
            count = 0;
            currentNode = seedNode;
            bool allLabeled = false;
            while (!allLabeled)
            {

                // Parse current neighbors
                List<MazeNode> unlabeledNeighborsNotOnStack = new List<MazeNode>();
                bool hasOnShortestPath = false;
                bool hasOnLoop = false;
                int shortestPathCount = 0;
                foreach (MazeNode nb in currentNode.ConnectedNeighbors)
                {
                    if (nb.IsActive)
                    {
                        if (onShortestPath[nb.Identifier])
                        { shortestPathCount++; }
                        else if (onLoop[nb.Identifier]) { hasOnLoop = true; }
                        else if (onDeadEnd[nb.Identifier]) { }
                        else
                        { if (!nodesToLabel.Contains(nb)) { unlabeledNeighborsNotOnStack.Add(nb); } }
                    }
                }
                // If we're at the seed node, the first onShortestPath node is ignored (it's the hook), otherwise any one is fine
                if (currentNode == seedNode)
                { hasOnShortestPath = shortestPathCount > 1; }
                else
                { hasOnShortestPath = shortestPathCount > 0; }

                // Move forward or label and backtrack
                // If current node has an unlabeled node not on the stack --> explore (6)
                if (unlabeledNeighborsNotOnStack.Count > 0)
                {
                    // Add current node to stack and set first neighbor not on stack (order doesn't matter) as current node
                    foreach (MazeNode nb in unlabeledNeighborsNotOnStack)
                    {
                        nodesToLabel.Push(currentNode);
                        currentNode = nb;
                        break;
                    }
                }
                else
                // If there are no more unlabeled nodes that are not on the stack, we can label and backtrack
                {
                    int activeConnectedNeighborsCount = 0;
                    foreach (MazeNode nb in currentNode.ConnectedNeighbors) { if (nb.IsActive) { activeConnectedNeighborsCount++; } }
                    if (activeConnectedNeighborsCount <= 1) // easiest case
                    { onDeadEnd[currentNode.Identifier] = true; }
                    else if (hasOnLoop || (hasOnShortestPath && currentNode != seedNode))
                    { onLoop[currentNode.Identifier] = true; }
                    else
                    { onDeadEnd[currentNode.Identifier] = true; }

                    // Backtrack if there are still unlabeled, otherwise end
                    if (nodesToLabel.Count != 0)
                    { currentNode = nodesToLabel.Pop(); }
                    else
                    { allLabeled = true; }
                }

                // Safety check
                count++;
                if (count > mazeFrame.Nodes.Count * 2) { throw new System.Exception("Labeling nodes loop did not terminate correctly."); }

            }
        }

        // Set junctions
        // onDeadEnd junctions
        List<MazeNode> nodesNeighboringOnDeadEnd = new List<MazeNode>();
        List<MazeNode> nodesNeighboringOnLoop = new List<MazeNode>();
        for (int i = 0; i < mazeFrame.Nodes.Count; i++)
        {
            if (!mazeFrame.Nodes[i].IsActive) continue;
            // Every node neighboring an onDeadEnd is a junction for a dead ending path
            foreach (MazeNode nb in mazeFrame.Nodes[i].ConnectedNeighbors)
            { if (nb.IsActive && onDeadEnd[nb.Identifier]) { nodesNeighboringOnDeadEnd.Add(mazeFrame.Nodes[i]); break; } }

            // Only nodes that are onShortestPath neighboring an onLoop can be an onLoop junction
            if (onShortestPath[mazeFrame.Nodes[i].Identifier])
                foreach (MazeNode nb in mazeFrame.Nodes[i].ConnectedNeighbors)
                { if (nb.IsActive && onLoop[nb.Identifier]) { nodesNeighboringOnLoop.Add(mazeFrame.Nodes[i]); break; } }
        }
        foreach (MazeNode nd in nodesNeighboringOnDeadEnd)
        { onDeadEnd[nd.Identifier] = true; }
        foreach (MazeNode nd in nodesNeighboringOnLoop)
        { onLoop[nd.Identifier] = true; }

        // Find remaining nodes, and label them as notConnectedToPath
        foreach (MazeNode nd in mazeFrame.Nodes)
        { if (nd.IsActive && !onShortestPath[nd.Identifier] && !onLoop[nd.Identifier] && !onDeadEnd[nd.Identifier]) { notConnectedToPath[nd.Identifier] = true; } }

        // Assign elements to maze frame
        int shortestPathInd = 0;
        bool indNotFound = true;
        while (indNotFound)
        {
            if (mazeFrame.ShortestPathInd.Contains(shortestPathInd)) { shortestPathInd++; }
            else { indNotFound = false; }
        }
        mazeFrame.SetShortestPathLength(shortestPathLength, shortestPathInd);
        mazeFrame.SetOnShortestPath(onShortestPath, shortestPathInd);
        mazeFrame.SetOnDeadEnd(onDeadEnd);
        mazeFrame.SetOnLoop(onLoop);
        mazeFrame.SetNotConnectedToPath(notConnectedToPath);
    }

    /// <summary>
    /// Randomly select a node from a list, with a binary bias towards forward direction
    /// If nodes go against forward direction, they are not selected, unless no forward nodes
    /// exist.
    /// </summary>
    /// <returns>Selected node.</returns>
    /// <param name="originNode">Origin node.</param>
    /// <param name="nodes">Nodes from which one will be selected.</param>
    private MazeNode RandomlySelectNodeWithBiasTowardsForwards(MazeNode originNode, List<MazeNode> nodes)
    {
        // Select nodes that are not -forward
        List<MazeNode> forwardNodes = new List<MazeNode>();
        foreach (MazeNode node in nodes)
        {
            int sign = System.Math.Sign((Vector3.Scale(node.Position - originNode.Position, Vector3.forward).ComponentSum()));
            if (sign != -1) { forwardNodes.Add(node); }
        }
        if (forwardNodes.Count > 0)
        { return forwardNodes[mazeRandGen.Next(forwardNodes.Count)]; }
        else
        { return nodes[mazeRandGen.Next(nodes.Count)]; }
    }

    /// <summary>
    /// Randomly select a node from a list, with a bias towards the direction of a target node from an origin node.
    /// This is achieved by computing the difference between the angle between the origin node and the target
    /// node, and the angle between the nodes from the selection list. The smaller the difference, the higher
    /// the probability of selecting that node from the list.
    /// </summary>
    /// <returns>Selected node.</returns>
    /// <param name="originNode">Origin node.</param>
    /// <param name="nodes">Nodes from which one will be selected.</param>
    /// <param name="targetNode">Target node.</param>
    /// <param name="probExp">Exponent of the weighting coefficient to increase/decrease the bias towards the target node.</param>
    private MazeNode RandomlySelectNodeWithBiasTowardsNode(MazeNode originNode, List<MazeNode> nodes, MazeNode targetNode, float probExp)
    {
        // Bias random node selection using angle between direction from originNode towards targetNode 
        // Get angles as weights
        float[] weightsArray = new float[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            weightsArray[i] = Mathf.Pow(180f - Vector3.Angle(nodes[i].Position - originNode.Position, targetNode.Position - originNode.Position), probExp);
        }
        for (int i = 0; i < nodes.Count; i++)
        { if (Mathf.Approximately(weightsArray[i], 0)) { weightsArray[i] += 0.01f; } } // add a small bit incase of rare angle==0, weights shouldn't be 0
                                                                                       // set array of indices
        int[] intArray = new int[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        { intArray[i] = i; }
        // get index with weighted probability
        int ind = Utilities.GetRandomIntWeightedProbability(intArray, weightsArray, mazeRandGen);
        return nodes[ind];
    }


}

