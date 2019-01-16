using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class representing a built maze frame, containing a list of nodes, paths, and others.
/// </summary>
public class MazeFrame
{
    public List<MazeNode> Nodes { get; }
    //public Vector3Int Size { get; }
    public Vector3 Scale { get; }
    public Vector3 Center { get; private set; }
    public MazeNode StartNode { get; private set; }
    public MazeNode EndNode { get; private set; }
    public List<int> ShortestPathInd { get; private set; }  // FIXME this now also sort of functions as quadrant/segment color
    public Dictionary<int, int> ShortestPathLength { get; private set; } // FIXME ugh, there has to be a nicer way 
    public List<List<MazeNode>> Quadrants { get; }
    public int MinPathLength { get; }
    public int MaxPathLength { get; }
    public List<List<MazeNode>> PathSegments { get; private set; }
    public readonly System.Type MazeFrameGenerator;

    /// <summary>
    /// Initializes a new instance of <see cref="MazeFrame"/> class.
    /// </summary>
    /// <param name="nodes">Nodes of the maze frame.</param>
    // /// <param name="Size">Size of the maze, used in creation. To get actual bounds of maze, multiply with <paramref name="Scale"/>.</param>
    /// <param name="Scale">Scale of the maze, used in creation.</param>
    public MazeFrame(List<MazeNode> nodes, Vector3 Scale, int minPathLength, int maxPathLength, List<List<MazeNode>> Quadrants = null)
    {
        this.Nodes = nodes;
        //this.Size = Size;
        this.Scale = Scale;
        this.MinPathLength = minPathLength;
        this.MaxPathLength = maxPathLength;
        this.Quadrants = Quadrants;
        this.ShortestPathLength = new Dictionary<int, int>();
        // Find start/end nodes by label, and randomly otherwise
        this.StartNode = Nodes.Find(x => x.Identifier == ("start"));
        this.EndNode = Nodes.Find(x => x.Identifier == ("end"));
        if (this.StartNode == null)
        {
            List<MazeNode> searchList = new List<MazeNode>(this.Nodes);
            searchList.Remove(this.EndNode);
            this.StartNode = searchList[Random.Range(0, searchList.Count)];
        }
        if (this.EndNode == null)
        {
            List<MazeNode> searchList = new List<MazeNode>(this.Nodes);
            searchList.Remove(this.StartNode);
            this.EndNode = searchList[Random.Range(0, searchList.Count)];
        }
        // Set center of mazeframe
        this.Center = Vector3.zero;
        foreach (MazeNode node in Nodes) { this.Center += node.Position / Nodes.Count; }
        // Add shortest path indices
        this.ShortestPathInd = new List<int>();
        foreach (MazeNode node in Nodes)
        { foreach (int ind in node.shortestPathInd) { ShortestPathInd.AddIfNotPresent(ind); } }
    }

    /// <summary>
    /// Numbers the number of connections between nodes in the maze.
    /// </summary>
    /// <returns>Number of connections.</returns>
    public int NumberOfConnections()
    {
        int nConn = 0;
        foreach (MazeNode nd in Nodes)
        {
            nConn += nd.ConnectedNeighbors.Count;
        }
        return (nConn / 2); // nConn is guaranteed to be even (int==good) as every connection is counted twice
    }

    /// <summary>
    /// Returns the number of unique pairwise connections in the maze.
    /// </summary>
    /// <returns>Number of unique pairwise connections.</returns>
    public int NumberOfUniquePairwiseConnections()
    {
        int nConn = 0;
        foreach (MazeNode nd in Nodes)
        {
            int nUniqueConn = Utilities.BinomialCoefficient(nd.ConnectedNeighbors.Count, 2);
            //  add to total
            nConn += nUniqueConn;
        }
        return nConn;
    }

    /// <summary>
    /// Removes all connections if node is not on shortest path
    /// </summary>
    public void KeepOnlyShortestPathConnections()
    {
        foreach (MazeNode node in Nodes)
        {
            if (node.shortestPathInd.Count == 0)
            {
                node.RemoveConnectionByReference(new List<MazeNode>(node.ConnectedNeighbors));
                node.OnDeadEnd = false;
                node.OnLoop = false;
                node.NotConnectedToPath = true;
            }
        }
    }

    /// <summary>
    /// Concatenates maze frame objects along <paramref name="concatenationAxis"/>.
    /// </summary>
    /// <returns>Concatenated maze frame.</returns>
    /// <param name="mazeFrames">List of maze frames to concatenate.</param>
    /// <param name="offset">Offset between each end node of start node respectively, of to-be-concatenated frames.</param>
    /// /// <param name="concatenationAxis">Axis along which maze frames will be concatenated.</param>
    public static MazeFrame Concatenate(List<MazeFrame> mazeFrames, Vector3 offset, Vector3 concatenationAxis)
    {
        MazeFrame concatenate = mazeFrames[0];
        for (int i = 1; i < mazeFrames.Count; i++)
        { concatenate = Concatenate(concatenate, mazeFrames[i], offset, concatenationAxis); }
        return concatenate;
    }

    /// <summary>
    /// Concatenates two maze frame objects at the end node of the first,
    /// and the start node of the second, along <paramref name="concatenationAxis"/>.
    /// </summary>
    /// <returns>Concatenated maze frame.</returns>
    /// <param name="first">First maze frame.</param>
    /// <param name="second">Second maze frame.</param>
    /// <param name="offset">Offset between end node of first and start node of second.</param>
    /// <param name="concatenationAxis">Axis along which maze frames will be concatenated.</param>
    public static MazeFrame Concatenate(MazeFrame first, MazeFrame second, Vector3 offset, Vector3 concatenationAxis)
    {
        // Check whether they have the same shortest path indices
        foreach (int key in first.ShortestPathLength.Keys) { if (!second.ShortestPathLength.ContainsKey(key)) { throw new System.ArgumentException("Maze frames must have the same shortest path indices."); } }
        // Get offset
        offset = Vector3.Scale(offset + first.EndNode.Position + second.StartNode.Position.ComponentAbs(), concatenationAxis);
        // Translate second maze and rename all nodes based on offset
        second.Translate(offset);
        string namePrefix = Mathf.RoundToInt(offset.ComponentSum()) + "-";
        second.RenameAllNodesWithPreFix(namePrefix);
        // Connect first and second maze, by creating an intermediate connection node
        second.RenameNode(second.EndNode, "end");
        first.RenameNode(first.EndNode, first.EndNode.Position.ToString());
        MazeNode connNode = CreateConnectingNode(first.EndNode, second.StartNode);
        // Create ingredients for new maze frame creation
        // Set new nodes
        List<MazeNode> newNodes = new List<MazeNode>(first.Nodes);
        newNodes.Add(connNode);
        newNodes.AddRange(second.Nodes);
        // Set new Size and "Scale"
        //Vector3Int newSize = first.Size + second.Size;
        Vector3 newScale = (first.Scale + second.Scale) / 2;
        // Set new path lengths
        Dictionary<int, int> newShortestPathLength = new Dictionary<int, int>();
        foreach (int key in first.ShortestPathLength.Keys)
        { newShortestPathLength.Add(key, first.ShortestPathLength[key] + second.ShortestPathLength[key]); }
        int newMinPathLength = first.MinPathLength + second.MinPathLength;
        int newMaxPathLength = first.MaxPathLength + second.MaxPathLength;
        // Set new path segments
        List<List<MazeNode>> newPathSegments = ConcatenatePathSegments(first, second, connNode);
        // Get new maze frame
        MazeFrame concatenate = new MazeFrame(newNodes, newScale, newMinPathLength, newMaxPathLength);
        // Set new shortest paths and dictionaries
        foreach (int key in newShortestPathLength.Keys)
        { concatenate.SetShortestPathLength(newShortestPathLength[key], key); }
        // Add path segments
        concatenate.PathSegments = newPathSegments;
        return concatenate;
    }

    /// <summary>
    /// Pairwise combines maze frame objects by shrinking each second, aligning it's center to 
    /// the each first, and connecting the end node of each first to the end/start node of each second,
    /// whichever is closest. The start/end node of each second will be the new end node, which ever is furthest from
    /// the connection point.
    /// </summary>
    /// <returns>Combined maze frame.</returns>
    /// <param name="mazeFrames">List of maze frames to be combined.</param>
    /// <param name="shrinkFactor">Factor with which to shrink second frame, between 0-1.</param>
    public static MazeFrame CombineShrink(List<MazeFrame> mazeFrames, float shrinkFactor)
    {
        MazeFrame combination = mazeFrames[0];
        for (int i = 1; i < mazeFrames.Count; i++)
        { combination = CombineShrink(combination, mazeFrames[i], Mathf.Pow(shrinkFactor, i)); }
        return combination;
    }

    /// <summary>
    /// Combines two maze frame objects by shrinking the second, aligning it's center to 
    /// the first, and connecting the end node of the first to the end/start node the second,
    /// whichever is closest. The start/end node of the second will be the new end node, which ever is furthest from
    /// the connection point.
    /// </summary>
    /// <returns>Combined maze frame.</returns>
    /// <param name="first">First maze frame.</param>
    /// <param name="second">Second maze frame.</param>
    /// <param name="shrinkFactor">Factor with which to shrink second frame, between 0-1.</param>
    public static MazeFrame CombineShrink(MazeFrame first, MazeFrame second, float shrinkFactor)
    {
        if (shrinkFactor < 0 || shrinkFactor > 1) { throw new System.ArgumentException("Shrinkfactor should be between 0 and 1."); }
        // Check whether they have the same shortest path indices
        foreach (int key in first.ShortestPathLength.Keys) { if (!second.ShortestPathLength.ContainsKey(key)) { throw new System.ArgumentException("Maze frames must have the same shortest path indices."); } }
        // Shrink second maze and rename all nodes based on factor, and align node positions to center of first
        second.ApplyScale(shrinkFactor);
        string namePrefix = shrinkFactor + "-";
        second.RenameAllNodesWithPreFix(namePrefix);
        Vector3 offset = Vector3.zero;
        foreach (MazeNode node in second.Nodes)
        { offset += node.Position / second.Nodes.Count; }
        second.Translate(-(offset + first.Center));
        // Connect first and second maze, by creating an intermediate connection node
        MazeNode connNode;
        if (Vector3.Distance(first.EndNode.Position, second.EndNode.Position) < Vector3.Distance(first.EndNode.Position, second.StartNode.Position))
        {
            connNode = CreateConnectingNode(first.EndNode, second.EndNode);
            second.RenameNode(second.EndNode, second.EndNode.Position.ToString());
            second.RenameNode(second.StartNode, "end");
        }
        else
        {
            connNode = CreateConnectingNode(first.EndNode, second.StartNode);
            second.RenameNode(second.StartNode, second.StartNode.Position.ToString());
            second.RenameNode(second.EndNode, "end");
        }
        first.RenameNode(first.EndNode, first.EndNode.Position.ToString());
        // Create ingredients for new maze frame creation
        // Set new nodes
        List<MazeNode> newNodes = new List<MazeNode>(first.Nodes);
        newNodes.Add(connNode);
        newNodes.AddRange(second.Nodes);
        // Set new Size and "Scale"
        Vector3 newScale = (first.Scale + second.Scale * shrinkFactor) / 2;
        // Set new path lengths
        Dictionary<int, int> newShortestPathLength = new Dictionary<int, int>();
        foreach (int key in first.ShortestPathLength.Keys)
        { newShortestPathLength.Add(key, first.ShortestPathLength[key] + second.ShortestPathLength[key]); }
        int newMinPathLength = first.MinPathLength + second.MinPathLength;
        int newMaxPathLength = first.MaxPathLength + second.MaxPathLength;
        // Set new path segments
        List<List<MazeNode>> newPathSegments = ConcatenatePathSegments(first, second, connNode);
        // Get new maze frame
        MazeFrame combination = new MazeFrame(newNodes, newScale, newMinPathLength, newMaxPathLength);
        // Set new shortest paths and dictionaries
        foreach (int key in newShortestPathLength.Keys)
        { combination.SetShortestPathLength(newShortestPathLength[key], key); }
        // Add path segments
        combination.PathSegments = newPathSegments;
        return combination;
    }


    /// <summary>
    /// Merge MazeFrames in list, adding connections and paths.
    /// This is only possible if they have identical Node identifiers, size, scale.
    /// Only shortest paths are kept, and path segments are removed.
    /// </summary>
    /// <param name="mazeFrames">Maze frames to combine.</param>
    public static MazeFrame Merge(List<MazeFrame> mazeFrames)
    {
        MazeFrame combination = mazeFrames[0];
        for (int i = 1; i < mazeFrames.Count; i++)
        { combination = Merge(combination, mazeFrames[i]); }
        return combination;
    }

    /// <summary>
    /// Merge MazeFrames, adding connections and paths.
    /// This is only possible if they have identical Node identifiers, size, scale.
    /// All nodes kept originate from the first frame.
    /// </summary>
    /// <param name="first">First maze frame to combine.</param>
    ///  <param name="second">Second maze frame to combine.</param>
    public static MazeFrame Merge(MazeFrame first, MazeFrame second)
    {
        // First check whether the maze frames can be added based on size/scale,  whether they contain each others nodes is checked automatically later
        if (!first.Scale.ComponentsAreApproxEqualTo(second.Scale)) // || !first.Size.ComponentsAreEqualTo(second.Size)
        { throw new System.ArgumentException("Maze frames cannot be added."); }
        //if (first.Quadrants != null && second.Quadrants != null && first.Quadrants.Count != second.Quadrants.Count)
        //{ throw new System.ArgumentException("Maze frames cannot be added."); }
        foreach (int key in first.ShortestPathLength.Keys)
        { if (second.ShortestPathLength.ContainsKey(key)) { throw new System.ArgumentException("Maze frames cannot be combined."); } }
        // Create ingredients for new maze frame creation
        // Set new nodes
        //first.KeepOnlyShortestPathConnections();
        //second.KeepOnlyShortestPathConnections();
        first.Nodes.Sort();
        second.Nodes.Sort();
        List<MazeNode> newNodes = new List<MazeNode>(first.Nodes);
        for (int i = 0; i < newNodes.Count; i++)
        { CombineSecondNodeWithFirstNode(newNodes[i], second.Nodes[i]); }
        // Set new Size and Scale
        //Vector3Int newSize = first.Size;
        Vector3 newScale = first.Scale;
        // Set new path lengths
        Dictionary<int, int> newShortestPathLength = new Dictionary<int, int>();
        foreach (int key in first.ShortestPathLength.Keys) { newShortestPathLength.Add(key, first.ShortestPathLength[key]); }
        foreach (int key in second.ShortestPathLength.Keys) { newShortestPathLength.Add(key, second.ShortestPathLength[key]); }
        int newMinPathLength = first.MinPathLength;
        int newMaxPathLength = first.MaxPathLength;
        // Set new path segments
        List<List<MazeNode>> newPathSegments = null;
        if (first.PathSegments != null && second.PathSegments != null)
        { newPathSegments = AddPathSegmentsOfSecondToThoseOfFirst(first, second); }
        // Get new maze frame
        MazeFrame combination = new MazeFrame(newNodes, newScale, newMinPathLength, newMaxPathLength);
        // Set new shortest paths and dictionaries
        foreach (int key in newShortestPathLength.Keys)
        { combination.SetShortestPathLength(newShortestPathLength[key], key); }
        // Add path segments
        combination.PathSegments = newPathSegments;
        return combination;
    }

    /// <summary>
    /// Create a new node connecting <paramref name="first"/> and <paramref name="second"/>.
    /// </summary>
    /// <returns>The connecting node.</returns>
    /// <param name="first">First node.</param>
    /// <param name="second">Second node.</param>
    private static MazeNode CreateConnectingNode(MazeNode first, MazeNode second)
    {
        if (first.Identifier == second.Identifier) { throw new System.ArgumentException("Nodes cannot be connected."); }
        MazeNode connNode = new MazeNode(first.Position + (second.Position - first.Position) / 2, "conn" + "-" + first.Identifier + "-" + second.Identifier);
        connNode.AddBaseConnectionByReference(first);
        connNode.AddConnectionByReference(first);
        connNode.AddBaseConnectionByReference(second);
        connNode.AddConnectionByReference(second);
        // Add flags 
        if (first.OnDeadEnd || second.OnDeadEnd) { connNode.OnDeadEnd = true; } else { connNode.OnDeadEnd = false; }
        if (first.OnLoop || second.OnLoop) { connNode.OnLoop = true; } else { connNode.OnLoop = false; }
        if (first.NotConnectedToPath && second.NotConnectedToPath) { connNode.NotConnectedToPath = true; } else { connNode.NotConnectedToPath = false; }
        // Add shortest path inds
        connNode.shortestPathInd = new List<int>(first.shortestPathInd);
        foreach (int ind in second.shortestPathInd)
        { connNode.shortestPathInd.AddIfNotPresent(ind); }
        return connNode;
    }

    /// <summary>
    /// Combines the second node with first node. 
    /// </summary>
    /// <param name="first">First node.</param>
    /// <param name="second">Second node.</param>
    private static void CombineSecondNodeWithFirstNode(MazeNode first, MazeNode second)
    {
        if (first.Identifier != second.Identifier) { throw new System.ArgumentException("Nodes cannot be combined."); }
        foreach (MazeNode neighbor in second.AllNeighbors) { first.CheckIfPartOfAllNeighborsByIdentifier(neighbor.Identifier); } // if this goes through we're good.
        // Add flags to first
        if (first.OnDeadEnd || second.OnDeadEnd) { first.OnDeadEnd = true; } else { first.OnDeadEnd = false; }
        if (first.OnLoop || second.OnLoop) { first.OnLoop = true; } else { first.OnLoop = false; }
        if (first.NotConnectedToPath && second.NotConnectedToPath) { first.NotConnectedToPath = true; } else { first.NotConnectedToPath = false; }
        // Add second's shortest path inds to first's
        foreach (int ind in second.shortestPathInd)
        { first.shortestPathInd.AddIfNotPresent(ind); }
        // Add all neighbors connection by identifier
        foreach (MazeNode neighbor in second.ConnectedNeighbors)
        { first.AddConnectionByIdentifier(neighbor.Identifier); }
    }

    /// <summary>
    /// "Concatenates" path segments of two maze frames, by adding the connecting 
    /// node if the start node of the second or the end node of the first was present.
    /// </summary>
    /// <returns>Added path segments</returns>
    /// <param name="first">First maze frame.</param>
    /// <param name="second">Second maze frame.</param>
    private static List<List<MazeNode>> ConcatenatePathSegments(MazeFrame first, MazeFrame second, MazeNode connNode)
    {
        List<List<MazeNode>> newPathSegments = null;
        if (first.PathSegments != null && second.PathSegments != null)
        {
            newPathSegments = new List<List<MazeNode>>(first.PathSegments.Count + second.PathSegments.Count);
            newPathSegments.AddRange(first.PathSegments);
            newPathSegments.AddRange(second.PathSegments);
            foreach (List<MazeNode> segment in newPathSegments)
            {
                int endInd = segment.FindIndex(x => x == first.EndNode);
                if (endInd != -1) { segment.Insert(endInd + 1, connNode); }
                int startInd = segment.FindIndex(x => x == second.StartNode);
                if (startInd != -1) { segment.Insert(startInd, connNode); }
            }
        }
        return newPathSegments;
    }


    /// <summary>
    /// Adds the path segments of two maze frames, where all resulting nodes
    /// originate from the first.
    /// </summary>
    /// <returns>Added path segments</returns>
    /// <param name="first">First maze frame.</param>
    /// <param name="second">Second maze frame.</param>
    private static List<List<MazeNode>> AddPathSegmentsOfSecondToThoseOfFirst(MazeFrame first, MazeFrame second)
    {
        List<List<MazeNode>> newPathSegments = new List<List<MazeNode>>(first.PathSegments.Count + second.PathSegments.Count);
        newPathSegments.AddRange(first.PathSegments);
        foreach (List<MazeNode> segment in second.PathSegments)
        {
            first.CheckNodePresenceByIdentifier(segment); // if this goes through, all segment nodes exist in first
            List<MazeNode> newSegment = new List<MazeNode>(segment.Count);
            foreach (MazeNode node in segment)
            { newSegment.Add(first.Nodes.Find(x => x.Identifier == node.Identifier)); }
            newPathSegments.Add(newSegment);
        }
        return newPathSegments;
    }


    /// <summary>
    /// Renames a node.
    /// </summary>
    /// <param name="node">Node.</param>
    /// <param name="name">Name.</param>
    private void RenameNode(MazeNode node, string name)
    {
        string oldName = node.Identifier;
        node.RenameNode(name);
    }

    /// <summary>
    /// Rename all Nodes by adding prefix to identifier.
    /// </summary>
    /// <param name="prefix">Prefix.</param>
    private void RenameAllNodesWithPreFix(string prefix)
    {
        foreach (MazeNode node in Nodes)
        { RenameNode(node, prefix + node.Identifier); }
    }

    /// <summary>
    /// Increments the shortest path indices by one.
    /// </summary>
    public void IncrementShortestPathIndices()
    {
        Dictionary<int, int> newShortestPathLength = new Dictionary<int, int>();
        foreach (int key in this.ShortestPathLength.Keys) { newShortestPathLength.Add(key + 1, this.ShortestPathLength[key]); }
        ShortestPathLength = newShortestPathLength;
        List<int> newShortestPathInd = new List<int>(ShortestPathInd.Count);
        foreach (int ind in ShortestPathInd) { newShortestPathInd.Add(ind + 1); }
        ShortestPathInd = newShortestPathInd;
        foreach (MazeNode node in Nodes)
        { node.IncrementShortestPathIndices(); }
    }

    /// <summary>
    /// Adds new start/end node with offset from maze (previous start/end will be 
    /// renamed entry/exit). If PathSegments is present, new nodes will be added to them.
    /// If <paramref name="addIntermediate"/> is true, two additional nodes will be added between
    /// old and new nodes. The intermediate nodes will be added to current path segments.
    /// </summary>
    /// <param name="offset">Offset.</param>
    /// <param name="addIntermediate">Whether to add intermediate nodes.</param>
    public void AddOffsetStartAndEndNodes(Vector3 offset, bool addIntermediate = false)
    {
        AddOffsetStartNode(offset, addIntermediate);
        AddOffsetEndNode(offset, addIntermediate);
    }

    /// <summary>
    /// Adds new start node with offset from maze (previous start will be 
    /// renamed entry). If PathSegments is present, new nodes will be added to them.
    /// If <paramref name="addIntermediate"/> is true, an additional node will be added between
    /// old and new nodes. The intermediate nodes will be added to current path segments.
    /// </summary>
    /// <param name="offset">Offset.</param>
    /// <param name="addIntermediate">Whether to add intermediate nodes.</param>
    public void AddOffsetStartNode(Vector3 offset, bool addIntermediate = false)
    {
        MazeNode newStart = new MazeNode(this.StartNode.Position - offset, "start");
        Nodes.Add(newStart);
        this.RenameNode(this.StartNode, "entry");
        if (!addIntermediate)
        {
            newStart.AddBaseConnectionByReference(this.StartNode);
            newStart.AddConnectionByReference(this.StartNode);
            newStart.shortestPathInd = new List<int>(this.StartNode.shortestPathInd);
        }
        else
        {
            MazeNode interimStart = new MazeNode(this.StartNode.Position - (offset / 2), "interim-start");
            Nodes.Add(interimStart);
            interimStart.AddBaseConnectionByReference(this.StartNode);
            interimStart.AddConnectionByReference(this.StartNode);
            interimStart.AddBaseConnectionByReference(newStart);
            interimStart.AddConnectionByReference(newStart);
            interimStart.shortestPathInd = new List<int>(this.StartNode.shortestPathInd);
            newStart.AddBaseConnectionByReference(interimStart);
            newStart.AddConnectionByReference(interimStart);
            newStart.shortestPathInd = new List<int>(interimStart.shortestPathInd);
        }
        this.StartNode = newStart;
        //
        if (PathSegments != null)
        {
            PathSegments.Add(new List<MazeNode>(2) { newStart, newStart.ConnectedNeighbors[0] });
            if (addIntermediate)
            {
                foreach (List<MazeNode> segment in PathSegments)
                {
                    int entryInd = segment.FindIndex(x => x.Identifier == ("entry"));
                    if (entryInd != -1) { segment.Insert(entryInd, this.StartNode.ConnectedNeighbors[0]); }
                }
            }
        }
    }

    /// <summary>
    /// Adds new end node with offset from maze (previous end will be 
    /// renamed exit). If PathSegments is present, new nodes will be added to them.
    /// If <paramref name="addIntermediate"/> is true, an additional node will be added between
    /// old and new nodes. The intermediate node will be added to current path segments.
    /// </summary>
    /// <param name="offset">Offset.</param>
    /// <param name="addIntermediate">Whether to add intermediate nodes.</param>
    public void AddOffsetEndNode(Vector3 offset, bool addIntermediate = false)
    {
        MazeNode newEnd = new MazeNode(this.EndNode.Position + offset, "end");
        Nodes.Add(newEnd);
        this.RenameNode(this.EndNode, "exit");
        if (!addIntermediate)
        {
            newEnd.AddBaseConnectionByReference(this.EndNode);
            newEnd.AddConnectionByReference(this.EndNode);
            newEnd.shortestPathInd = new List<int>(this.EndNode.shortestPathInd);
        }
        else
        {
            MazeNode interimEnd = new MazeNode(this.EndNode.Position + (offset / 2), "interim-end");
            Nodes.Add(interimEnd);
            interimEnd.AddBaseConnectionByReference(this.EndNode);
            interimEnd.AddConnectionByReference(this.EndNode);
            interimEnd.AddBaseConnectionByReference(newEnd);
            interimEnd.AddConnectionByReference(newEnd);
            interimEnd.shortestPathInd = new List<int>(this.EndNode.shortestPathInd);
            newEnd.AddBaseConnectionByReference(interimEnd);
            newEnd.AddConnectionByReference(interimEnd);
            newEnd.shortestPathInd = new List<int>(interimEnd.shortestPathInd);
        }
        this.EndNode = newEnd;
        if (PathSegments != null)
        {
            PathSegments.Add(new List<MazeNode>(2) { newEnd, newEnd.ConnectedNeighbors[0] });
            if (addIntermediate)
            {
                foreach (List<MazeNode> segment in PathSegments)
                {
                    int exitInd = segment.FindIndex(x => x.Identifier == ("exit"));
                    if (exitInd != -1) { segment.Insert(exitInd + 1, this.EndNode.ConnectedNeighbors[0]); }
                }
            }
        }
    }


    /// <summary>
    /// Translate the Maze Frame with <paramref name="translation"/>.
    /// </summary>
    /// <param name="translation">Translation.</param>
    public void Translate(Vector3 translation)
    {
        foreach (MazeNode node in Nodes) { node.Translate(translation); }
        this.Center = this.Center + translation;
    }

    /// <summary>
    /// Rotate the Maze Frame with <paramref name="rotation"/>, from the
    /// perspective of 0,0,0.
    /// </summary>
    /// <param name="rotation">Rotation.</param>
    public void Rotate(Quaternion rotation)
    {
        foreach (MazeNode node in Nodes) { node.Rotate(rotation); }
    }

    /// <summary>
    /// Scale the Maze Frame with <paramref name="scale"/>, by
    /// multiplying node positions with it.
    /// </summary>
    /// <param name="scale">Scale.</param>
    public void ApplyScale(float scale)
    {
        foreach (MazeNode node in Nodes) { node.Scale(scale); }
    }

    /// <summary>
    /// Get maze difficulty based on path length of each shortest path.
    /// </summary>
    public Dictionary<int, float> GetDifficulty(int quadrantInd = -1)
    {
        int currMaxPathLength;
        if (quadrantInd != -1) { currMaxPathLength = this.Quadrants[quadrantInd].Count; }
        else { currMaxPathLength = this.Nodes.Count; }
        Dictionary<int, float> difficulty = new Dictionary<int, float>(ShortestPathLength.Count);
        foreach (int key in ShortestPathLength.Keys)
        { difficulty.Add(key, ((float)ShortestPathLength[key] - (float)MinPathLength) / (float)currMaxPathLength); }
        return difficulty;
    }

    /// <summary>
    /// Get the number of intersections on each shortest path.
    /// </summary>
    public Dictionary<int, int> GetNIntersectionsOnPath()
    {
        Dictionary<int, int> nIntersectionsOnPath = new Dictionary<int, int>(ShortestPathLength.Count);
        foreach (int key in ShortestPathLength.Keys)
        { nIntersectionsOnPath.Add(key, 0); }
        foreach (MazeNode node in this.Nodes)
        {
            foreach (int key in ShortestPathLength.Keys)
            { if (node.shortestPathInd.Contains(key) && node.ConnectedNeighbors.Count >= 3) nIntersectionsOnPath[key]++; }
        }
        return nIntersectionsOnPath;
    }


    /// <summary>
    /// Sets the length of the shortest path.
    /// </summary>
    /// <param name="shortestPathLength">Shortest path length.</param>
    /// <param name="shortestPathInd">Index of shortest path.</param>
    public void SetShortestPathLength(int shortestPathLength, int shortestPathInd)
    {
        this.ShortestPathLength.Add(shortestPathInd, shortestPathLength);
        this.ShortestPathInd.AddIfNotPresent(shortestPathInd);
    }

    /// <summary>
    /// Sets shortestPathInd for each node.
    /// </summary>
    /// <param name="onShortestPath">Dictionary containing node identifiers and booleans.</param>
    /// <param name="shortestPathInd">Index of shortest path.</param>
    public void SetOnShortestPath(Dictionary<string, bool> onShortestPath, int shortestPathInd)
    {
        CheckNodePresenceByIdentifier(onShortestPath); // If this succeeds, all nodes are good
        foreach (MazeNode node in Nodes)
        {
            if (onShortestPath.ContainsKey(node.Identifier) && onShortestPath[node.Identifier])
            { node.shortestPathInd.AddIfNotPresent(shortestPathInd); }
        }
    }

    /// <summary>
    /// Sets shortestPathInd for each node.
    /// </summary>
    /// <param name="nodes">List of nodes to be placed on shortest path.</param>
    /// <param name="shortestPathInd">Index of shortest path.</param>
    public void SetOnShortestPath(List<MazeNode> nodes, int shortestPathInd)
    {
        foreach (MazeNode node in nodes)
        {
            CheckNodePresenceByReference(node); // If this succeeds, all nodes are good
            node.shortestPathInd.AddIfNotPresent(shortestPathInd);
        }
        ShortestPathInd.AddIfNotPresent(shortestPathInd);
    }

    /// <summary>
    /// Sets the OnDeadEnd flags of nodes.
    /// </summary>
    /// <param name="onDeadEnd">Dictionary containing node identifiers and booleans.</param>
    public void SetOnDeadEnd(Dictionary<string, bool> onDeadEnd)
    {
        CheckNodePresenceByIdentifier(onDeadEnd); // If this succeeds, all nodes are good
        foreach (MazeNode node in Nodes)
        { if (onDeadEnd.ContainsKey(node.Identifier)) { node.OnDeadEnd = onDeadEnd[node.Identifier]; } }
    }
    /// <summary>
    /// Sets the OnLoop flags of nodes.
    /// </summary>
    /// <param name="onLoop">Dictionary containing node identifiers and booleans.</param>
    public void SetOnLoop(Dictionary<string, bool> onLoop)
    {
        CheckNodePresenceByIdentifier(onLoop); // If this succeeds, all nodes are good
        foreach (MazeNode node in Nodes)
        { if (onLoop.ContainsKey(node.Identifier)) { node.OnLoop = onLoop[node.Identifier]; } }
    }
    /// <summary>
    /// Sets the NotConnectToPath flags of nodes.
    /// </summary>
    /// <param name="notConnectedToPath">Dictionary containing node identifiers and booleans.</param>
    public void SetNotConnectedToPath(Dictionary<string, bool> notConnectedToPath)
    {
        CheckNodePresenceByIdentifier(notConnectedToPath); // If this succeeds, all nodes are good
        foreach (MazeNode node in Nodes)
        { if (notConnectedToPath.ContainsKey(node.Identifier)) { node.NotConnectedToPath = notConnectedToPath[node.Identifier]; } }
    }

    /// <summary>
    /// Sets all nodes belonging to quadrant to active, all else to inactive.
    /// </summary>
    /// <param name="quadrantInd">Quadrant index.</param>
    public void SetActiveQuadrant(int quadrantInd)
    {
        foreach (MazeNode node in this.Nodes)
        {
            node.IsActive = Quadrants[quadrantInd].Contains(node) ? true : false;
        }
    }

    /// <summary>
    /// Activates all nodes.
    /// </summary>
    public void ActivateAllNodes()
    {
        foreach (MazeNode node in this.Nodes) { node.IsActive = true; }
    }

    ///// <summary>
    ///// Sets the path segments.
    ///// </summary>
    ///// <param name="pathSegments">Path segments.</param>
    //public void SetPathSegments(List<List<MazeNode>> pathSegments)
    //{
    //    // Check wether all nodes exist in Nodes
    //    foreach (List<MazeNode> segment in pathSegments)
    //    { CheckIfNodesArePresent(segment); } // If this succeeds, all nodes are good
    //    PathSegments = pathSegments;
    //}



    /// <summary>
    /// Checks if nodes in list are present, by reference.
    /// </summary>
    /// <param name="nodes">Nodes.</param>
    private void CheckNodePresenceByReference(List<MazeNode> nodes)
    {
        foreach (MazeNode node in nodes) { CheckNodePresenceByReference(node); }
    }
    /// <summary>
    /// Checks if node is present, by reference.
    /// </summary>
    /// <param name="node">Node.</param>
    private void CheckNodePresenceByReference(MazeNode node)
    {
        if (!Nodes.Contains(node)) { throw new System.ArgumentException("Node not part of maze frame."); }
    }

    /// <summary>
    /// Checks if nodes described in dictionary identifiers are present.
    /// </summary>
    /// <param name="dict">Dict.</param>
    private void CheckNodePresenceByIdentifier(Dictionary<string, bool> dict)
    {
        foreach (string identifier in new List<string>(dict.Keys)) { CheckNodePresenceByIdentifier(identifier); }
    }

    /// <summary>
    /// Checks if nodes in list are present.
    /// </summary>
    /// <param name="nodes">Nodes.</param>
    private void CheckNodePresenceByIdentifier(List<MazeNode> nodes)
    {
        foreach (MazeNode node in nodes) { CheckNodePresenceByIdentifier(node.Identifier); }
    }

    /// <summary>
    /// Checks if node is present.
    /// </summary>
    /// <param name="identifier">Node identifier.</param>
    private void CheckNodePresenceByIdentifier(string identifier)
    {
        if (Nodes.Find(x => x.Identifier == identifier) == null) { throw new System.ArgumentException("Node not part of maze frame."); }
    }

    /// <summary>
    /// Connect unconnected nodes by generating minimazes using recursive backtracking
    /// </summary>
    public void ConnectUnconnectedNodes()
    {
        // Find unconnected nodes and fill em in
        List<MazeNode> unConnectedNodes = new List<MazeNode>(Nodes.Count);
        foreach (MazeNode node in Nodes) { if (node.IsActive && node.ConnectedNeighbors.Count == 0) { unConnectedNodes.Add(node); } }
        int count = 0;
        while (unConnectedNodes.Count > 0)
        {
            // Find island among unconnected nodes
            List<MazeNode> island = new List<MazeNode>(Mathf.RoundToInt(Mathf.Pow(Nodes.Count, .5f)));
            island.Add(unConnectedNodes[0]);
            unConnectedNodes.RemoveAt(0);
            Stack<MazeNode> searchStack = new Stack<MazeNode>();
            searchStack.Push(island[0]);
            int islandCount = 0;
            do
            {
                MazeNode currNode = searchStack.Pop();
                currNode.NotConnectedToPath = false;
                foreach (MazeNode neigh in currNode.AllNeighbors)
                {
                    if (island.Contains(neigh)) { continue; }
                    if (unConnectedNodes.Contains(neigh))
                    {
                        island.Add(neigh);
                        unConnectedNodes.Remove(neigh);
                        searchStack.Push(neigh);
                    }
                }
                // Safety check
                islandCount++; if (islandCount > this.Nodes.Count) { throw new System.Exception("Connecting unconnected nodes went wrong."); }
            } while (searchStack.Count > 0);

            // If island contains only one node, connect it randomly, otherwise create mini maze
            if (island.Count == 1)
            { island[0].AddConnectionByReference(island[0].AllNeighbors[Random.Range(0, island[0].AllNeighbors.Count)]); }
            else
            {
                //// Get mini maze
                //// Deactive nodes not part of the minimaze
                //List<MazeNode> deactivatedNodes = new List<MazeNode>(this.Nodes.Count);
                //foreach (MazeNode node in this.Nodes)
                //{
                //    if (node.IsActive && !island.Contains(node))
                //    { node.IsActive = false; deactivatedNodes.Add(node); }
                //}
                //// Generate mini maze
                //MazeFrameCreatorUnspecified mazeFrameCreator = new MazeFrameCreatorUnspecified(island);
                //MazeFrame tmpFrame = mazeFrameCreator.GenerateMaze();
                //// Remove shortest path indices 
                //foreach (MazeNode node in tmpFrame.Nodes)
                //{ node.shortestPathInd = new List<int>(); }
                //// Reactive deactivated nodes
                //foreach (MazeNode node in deactivatedNodes) { node.IsActive = true; }
                //// Randomly connect mini maze to main frame and carry over labels
                //bool done = false;
                //List<int> indices = new List<int>(tmpFrame.Nodes.Count);
                //for (int i = 0; i < tmpFrame.Nodes.Count; i++) { indices.Insert(i, i); }
                //for (int i = 0; i < tmpFrame.Nodes.Count; i++)
                //{
                //    int randInd = indices[Random.Range(0, indices.Count)];
                //    indices.Remove(randInd);
                //    MazeNode currNode = tmpFrame.Nodes[randInd];
                //    foreach (MazeNode neigh in currNode.AllNeighbors)
                //    {
                //        if (neigh.shortestPathInd.Count != 0)
                //        {
                //            currNode.AddConnectionByReference(neigh);
                //            if (currNode.OnDeadEnd || neigh.OnDeadEnd) { neigh.OnDeadEnd = true; } else { neigh.OnDeadEnd = false; }
                //            if (currNode.OnLoop || neigh.OnLoop) { neigh.OnLoop = true; } else { neigh.OnLoop = false; }
                //            done = true;
                //            break;
                //        }
                //    }
                //    if (done) { break; }
                //}
                //Debug.Assert(done);

                // Get mini maze
                // First sever island nodes connections to main frame, restore them later
                Dictionary<string, List<MazeNode>> removedConnections = new Dictionary<string, List<MazeNode>>();
                foreach (MazeNode node in island)
                {
                    List<MazeNode> toBeRemoved = new List<MazeNode>(node.AllNeighbors.Count);
                    foreach (MazeNode neigh in node.AllNeighbors)
                    { if (neigh.ConnectedNeighbors.Count > 0) { toBeRemoved.Add(neigh); } }
                    node.RemoveBaseConnectionByReference(toBeRemoved);
                    removedConnections.Add(node.Identifier, toBeRemoved);
                }
                // Generate mini maze
                MazeFrameCreatorUnspecified mazeFrameCreator = new MazeFrameCreatorUnspecified(island);
                MazeFrame tmpFrame = mazeFrameCreator.GenerateMaze();
                // Remove shortest path indices 
                foreach (MazeNode node in tmpFrame.Nodes)
                { node.shortestPathInd = new List<int>(); }
                // Restore removed connections
                foreach (MazeNode node in island)
                { if (removedConnections.ContainsKey(node.Identifier)) { node.AddBaseConnectionByReference(removedConnections[node.Identifier]); } }
                // Randomly connect mini maze to main frame and carry over labels
                bool done = false;
                List<int> indices = new List<int>(tmpFrame.Nodes.Count);
                for (int i = 0; i < tmpFrame.Nodes.Count; i++) { indices.Insert(i, i); }
                for (int i = 0; i < tmpFrame.Nodes.Count; i++)
                {
                    int randInd = indices[Random.Range(0, indices.Count)];
                    indices.Remove(randInd);
                    MazeNode currNode = tmpFrame.Nodes[randInd];
                    foreach (MazeNode neigh in currNode.AllNeighbors)
                    {
                        if (neigh.shortestPathInd.Count != 0)
                        {
                            currNode.AddConnectionByReference(neigh);
                            if (currNode.OnDeadEnd || neigh.OnDeadEnd) { neigh.OnDeadEnd = true; } else { neigh.OnDeadEnd = false; }
                            if (currNode.OnLoop || neigh.OnLoop) { neigh.OnLoop = true; } else { neigh.OnLoop = false; }
                            done = true;
                            break;
                        }
                    }
                    if (done) { break; }
                }
                Debug.Assert(done);
            }
            // Safety check
            count++; if (count > this.Nodes.Count) { throw new System.Exception("Connecting unconnected nodes went wrong."); }
        }
    }



    /// <summary>
    /// Get a list of lists of nodes that form a path segment. A segment in this case in this case 
    /// is a set of nodes from a junction to a dead end, or another junction.
    /// </summary>
    public void AddPathSegments()
    {
        // This function adds to the maze frame a list of a list of Nodes that are connected // FIXME this is kind of hacky, think of a better algorithm
        // up to (and including) the junction points.
        if (PathSegments != null) { throw new System.Exception("Path segments are already present, cannot add new ones."); }

        // Prep storage output
        List<List<MazeNode>> pathSegments = new List<List<MazeNode>>();

        // Set storage of completed flags 
        Dictionary<string, bool> isCompleted = new Dictionary<string, bool>(this.Nodes.Count);
        foreach (MazeNode nd in this.Nodes)
        {
            if (nd.IsActive && nd.ConnectedNeighbors.Count > 0)
            { isCompleted.Add(nd.Identifier, false); }
            else
            { isCompleted.Add(nd.Identifier, true); }
        }

        // Set separate list for selecting not yet completed nodes
        List<MazeNode> notCompleted = new List<MazeNode>(isCompleted.Count);
        foreach (MazeNode nd in this.Nodes)
        { if (nd.IsActive && nd.ConnectedNeighbors.Count > 0) notCompleted.Add(nd); }

        // Setup path queue
        List<MazeNode> queueToPath = new List<MazeNode>();

        // Setup searching list
        List<MazeNode> searching = new List<MazeNode>();

        // Start at start node
        MazeNode currentNode = this.Nodes.Find(x => x.Identifier == ("start"));
        int count = 0;
        while (notCompleted.Count > 0)
        {
            //-If currentNode has 2 neighbors:
            //   -if only one is in queue:
            //      -set currentNode as completed
            //      -add currentNode to queue
            //      -set un-queued neighbor to currentNode
            //   -if two are in queue 
            //      -if one is the first in the queue (returned back to starting point --> on a loop)
            //         -add first node to end of queue again
            //         -add path to list using queued nodes
            //         -clear queue
            //         -find new node
            //      -else --> error
            //   -else (--> need to find junction)
            //      -add currentNode to Searching list
            //      -set neighbor to currentNode not on Searching list
            List<MazeNode> currentNodeActiveConnectedNeighbors = new List<MazeNode>();
            foreach (MazeNode nb in currentNode.ConnectedNeighbors) { if (nb.IsActive) { currentNodeActiveConnectedNeighbors.Add(nb); } }
            if (currentNodeActiveConnectedNeighbors.Count == 2)
            {
                if (queueToPath.Count == 0)
                {
                    searching.Add(currentNode);
                    int newNodeInd = 0;
                    bool inSearching0 = searching.Contains(currentNodeActiveConnectedNeighbors[0]);
                    bool inSearching1 = searching.Contains(currentNodeActiveConnectedNeighbors[1]);
                    if (inSearching0 && inSearching1) { throw new System.Exception("Impossible condition in searching queue."); }
                    if (inSearching0) { newNodeInd = 1; }
                    currentNode = currentNodeActiveConnectedNeighbors[newNodeInd];
                }
                else
                {
                    queueToPath.Add(currentNode);
                    isCompleted[currentNode.Identifier] = true; notCompleted.Remove(currentNode);
                    int newNodeInd = 0;
                    bool inQueue0 = queueToPath.Contains(currentNodeActiveConnectedNeighbors[0]);
                    bool inQueue1 = queueToPath.Contains(currentNodeActiveConnectedNeighbors[1]);
                    if (inQueue0 && inQueue1)
                    {
                        if (currentNodeActiveConnectedNeighbors[0] == queueToPath[0] || currentNodeActiveConnectedNeighbors[1] == queueToPath[0])
                        {
                            queueToPath.Add(queueToPath[0]);
                            pathSegments.Add(new List<MazeNode>(queueToPath));
                            queueToPath.Clear();
                            if (notCompleted.Count > 0) { currentNode = notCompleted[0]; }
                        }
                        else { throw new System.Exception("Impossible condition in path queue."); }
                    }
                    if (inQueue0) { newNodeInd = 1; }
                    currentNode = currentNodeActiveConnectedNeighbors[newNodeInd];
                }
            }
            //-ElseIf currentNode has 3+ neighbors:
            //   -if queue is empty, start new queue 
            //      -clear searching list
            //      -if there are uncompleted neigbors (came here from junction search)
            //         -if there are non-junction neighbors
            //            -add currentNode to queue
            //            -set an uncompleted non-junction neighbor as currentNode
            //         -else (only other neighbors are junctions
            //            -set currentNode as completed
            //            -for each junction neighbor (connections >2)
            //               -add currentNode to queue
            //               -add junction neighbor to queue
            //               -add path to list using queued nodes
            //               -clear queue
            //            -find new node
            //      -else (came here randomly)
            //         -set currentNode as completed
            //         -find new node
            //   -else (queue not empty --> arrived from other junction)
            //      -add currentNode to queue
            //      -add path to list using queued nodes
            //      -clear queue
            //      -find new node
            else if (currentNodeActiveConnectedNeighbors.Count > 2)
            {
                if (queueToPath.Count == 0)
                {
                    searching.Clear();
                    List<MazeNode> unCompletedNeighbors = new List<MazeNode>();
                    foreach (MazeNode nb in currentNodeActiveConnectedNeighbors) { if (!isCompleted[nb.Identifier]) { unCompletedNeighbors.Add(nb); } }
                    if (unCompletedNeighbors.Count > 0)
                    {
                        bool onlyJunctionNeighbors = true;
                        foreach (MazeNode nb in unCompletedNeighbors)
                        {
                            List<MazeNode> activeConnectedNeighbors = new List<MazeNode>();
                            foreach (MazeNode neigh in nb.ConnectedNeighbors) { if (neigh.IsActive) { activeConnectedNeighbors.Add(neigh); } }
                            if (activeConnectedNeighbors.Count < 3)
                            {
                                queueToPath.Add(currentNode);
                                currentNode = nb;
                                onlyJunctionNeighbors = false;
                                break;
                            }
                            if (!onlyJunctionNeighbors) break;
                        }
                        if (onlyJunctionNeighbors)
                        {
                            isCompleted[currentNode.Identifier] = true; notCompleted.Remove(currentNode);
                            foreach (MazeNode nb in unCompletedNeighbors)
                            {
                                queueToPath.Add(currentNode);
                                queueToPath.Add(nb);
                                pathSegments.Add(new List<MazeNode>(queueToPath));
                                queueToPath.Clear();
                            }
                            if (notCompleted.Count > 0) { currentNode = notCompleted[0]; }
                        }
                    }
                    else
                    {
                        isCompleted[currentNode.Identifier] = true; notCompleted.Remove(currentNode);
                        if (notCompleted.Count > 0) { currentNode = notCompleted[0]; }
                        if (queueToPath.Count > 0) { throw new System.Exception("Impossible condition in path queue."); }
                    }
                }
                else
                {
                    queueToPath.Add(currentNode);
                    pathSegments.Add(new List<MazeNode>(queueToPath));
                    queueToPath.Clear();
                    if (notCompleted.Count > 0) { currentNode = notCompleted[0]; }
                }
            }
            //-Else (currentNode has 1 neighbor):
            //   -set currentNode as completed
            //   -if queue is not empty
            //      -add currentNode to queue
            //      -add path to list using queued nodes
            //      -clear queue
            //      -find new node
            //   -else (queue empty)
            //      -clear searching list
            //      -add currentNode to queue
            //      -set neighbor to currentNode
            else
            {
                isCompleted[currentNode.Identifier] = true; notCompleted.Remove(currentNode);
                if (queueToPath.Count > 0)
                {
                    queueToPath.Add(currentNode);
                    pathSegments.Add(new List<MazeNode>(queueToPath));
                    queueToPath.Clear();
                    if (notCompleted.Count > 0) { currentNode = notCompleted[0]; }
                }
                else
                {
                    searching.Clear();
                    queueToPath.Add(currentNode);
                    currentNode = currentNodeActiveConnectedNeighbors[0];
                }
            }

            // Safety check
            count++;
            if (queueToPath.Count > this.Nodes.Count || count > Mathf.Pow(this.Nodes.Count, 2)) { throw new System.Exception("Impossible condition occured in finding path segmets"); }
        }

        // Assign output
        this.PathSegments = pathSegments;
    }
}