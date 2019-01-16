using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class describing maze frame fitted splines.
/// </summary>
public class MazeFrameSplines
{
    public List<int> ShortestPathInd { get; private set; }
    public List<SplineSegment> SplineSegments { get; private set; }
    public readonly Vector3 StartPoint;
    public readonly Vector3 EndPoint;

    /// <summary>
    /// Fit splines to maze frame as a new instance of <see cref="MazeFrameSplines"/>.
    /// </summary>
    /// <param name="mazeFrame">Maze frame.</param>
    public MazeFrameSplines(MazeFrame mazeFrame)
    {
        if (mazeFrame.PathSegments == null) { throw new System.ArgumentException("Maze frame needs to have path segments in order to fit splines to it."); }
        SplineSegments = new List<SplineSegment>(mazeFrame.Nodes.Count); // decent estimate
        ShortestPathInd = mazeFrame.ShortestPathInd;
        StartPoint = mazeFrame.StartNode.Position;
        EndPoint = mazeFrame.EndNode.Position;

        // First, create all junction splines
        float junctionSpace = .5f;
        List<MazeNode> junctionList = new List<MazeNode>(mazeFrame.Nodes.Count / 2);
        foreach (MazeNode node in mazeFrame.Nodes)
        { if (node.ConnectedNeighbors.Count >= 3) junctionList.Add(node); }
        foreach (MazeNode junction in junctionList)
        {
            for (int ineighb1 = 0; ineighb1 < (junction.ConnectedNeighbors.Count - 1); ineighb1++)
            {
                for (int ineighb2 = ineighb1 + 1; ineighb2 < junction.ConnectedNeighbors.Count; ineighb2++)
                {
                    // Create spline
                    MazeNode neighb1 = junction.ConnectedNeighbors[ineighb1];
                    MazeNode neighb2 = junction.ConnectedNeighbors[ineighb2];
                    if (IsNodeConnectedToOrientedUp(junction))
                    { if (!IsNodePositionOrientedUp(neighb1) && !IsNodePositionOrientedUp(neighb2)) { continue; } }
                    Vector3[] pArray = {
                        neighb1.Position + (junction.Position - neighb1.Position) * (1-junctionSpace),
                        junction.Position,
                        junction.Position,
                        neighb2.Position + (junction.Position - neighb2.Position) * (1-junctionSpace)};
                    Spline spline = new Spline(pArray);
                    // Set start/end normals based on node identity
                    if (IsNodePositionOrientedUp(neighb1)) { spline.SetRMFStartNormal(Vector3.up); }
                    else { spline.SetRMFStartNormal(mazeFrame.Center - pArray[0]); }
                    if (IsNodePositionOrientedUp(neighb2)) { spline.SetRMFEndNormal(Vector3.up); }
                    else { spline.SetRMFEndNormal(mazeFrame.Center - pArray[3]); }
                    // Create new spline segment
                    //List<int> newShortestPathInd = new List<int>(neighb1.shortestPathInd);
                    //foreach (int ind in neighb2.shortestPathInd) { newShortestPathInd.AddIfNotPresent(ind); }
                    //List<int> newShortestPathInd = new List<int>();
                    //foreach (int ind in neighb1.shortestPathInd)
                    //{ if (neighb2.shortestPathInd.Contains(ind)) { newShortestPathInd.AddIfNotPresent(ind); } }
                    //foreach (int ind in neighb2.shortestPathInd)
                    //{ if (neighb1.shortestPathInd.Contains(ind)) { newShortestPathInd.AddIfNotPresent(ind); } }
                    List<string> identifiers = new List<string>(3) { junction.Identifier, neighb1.Identifier, neighb2.Identifier };
                    SplineSegments.Add(new SplineSegment(spline, identifiers, junction.shortestPathInd, neighb1.shortestPathInd, neighb2.shortestPathInd, true));

                }
            }
        }
        // Then, add all regular segments
        foreach (List<MazeNode> path in mazeFrame.PathSegments)
        {
            // Skip path if it only consists of two junctions
            if (path.Count == 2 && path[0].ConnectedNeighbors.Count >= 3 && path[1].ConnectedNeighbors.Count >= 3) { continue; }
            // Create spline
            Vector3[] pArray = new Vector3[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                pArray[i] = path[i].Position;
            }
            if (path[0].ConnectedNeighbors.Count >= 3)
            { pArray[0] = pArray[0] + (pArray[1] - pArray[0]) * junctionSpace; }
            if (path[path.Count - 1].ConnectedNeighbors.Count >= 3)
            { pArray[path.Count - 1] = pArray[path.Count - 1] + (pArray[path.Count - 2] - pArray[path.Count - 1]) * junctionSpace; }
            Spline spline = new Spline(pArray);
            // Set start/end normals based on node identity
            if (IsNodePositionOrientedUp(path[0])) { spline.SetRMFStartNormal(Vector3.up); }
            else { spline.SetRMFStartNormal(mazeFrame.Center - pArray[0]); }
            if (IsNodePositionOrientedUp(path[path.Count - 1])) { spline.SetRMFEndNormal(Vector3.up); }
            else { spline.SetRMFEndNormal(mazeFrame.Center - pArray[path.Count - 1]); }
            // Get shortestPathInd, from an in-between node in path if possible
            List<int> newShortestPathInd = new List<int>();
            if (path.Count >= 3) // use first non-junction node
            { newShortestPathInd = new List<int>(path[1].shortestPathInd); }
            else if (path.Count == 2) // use union of only two nodes
            {
                foreach (int ind in path[0].shortestPathInd)
                { if (path[1].shortestPathInd.Contains(ind)) { newShortestPathInd.AddIfNotPresent(ind); } }
                foreach (int ind in path[1].shortestPathInd)
                { if (path[0].shortestPathInd.Contains(ind)) { newShortestPathInd.AddIfNotPresent(ind); } }
            }
            // Create new spline segment
            List<string> identifiers = new List<string>(path.Count);
            foreach (MazeNode node in path) { identifiers.Add(node.Identifier); }
            SplineSegments.Add(new SplineSegment(spline, identifiers, newShortestPathInd, path[0].shortestPathInd, path[path.Count - 1].shortestPathInd, false));
        }

        // Finally, define start/end neighbors
        foreach (SplineSegment baseSeg in SplineSegments)
        {
            // For start
            Vector3 currStart = baseSeg.StartPoint;
            foreach (SplineSegment otherSeg in SplineSegments)
            {
                if (baseSeg == otherSeg) { continue; }
                if (otherSeg.StartPoint.ComponentsAreApproxEqualTo(currStart))
                {
                    baseSeg.startNeighbors.AddIfNotPresent(otherSeg);
                    otherSeg.startNeighbors.AddIfNotPresent(baseSeg);
                }
                if (otherSeg.EndPoint.ComponentsAreApproxEqualTo(currStart))
                {
                    baseSeg.startNeighbors.AddIfNotPresent(otherSeg);
                    otherSeg.endNeighbors.AddIfNotPresent(baseSeg);
                }
            }
            // For end
            Vector3 currEnd = baseSeg.EndPoint;
            foreach (SplineSegment otherSeg in SplineSegments)
            {
                if (baseSeg == otherSeg) { continue; }
                if (otherSeg.StartPoint.ComponentsAreApproxEqualTo(currEnd))
                {
                    baseSeg.endNeighbors.AddIfNotPresent(otherSeg);
                    otherSeg.startNeighbors.AddIfNotPresent(baseSeg);
                }
                if (otherSeg.EndPoint.ComponentsAreApproxEqualTo(currEnd))
                {
                    baseSeg.endNeighbors.AddIfNotPresent(otherSeg);
                    otherSeg.endNeighbors.AddIfNotPresent(baseSeg);
                }
            }
        }
    }


    /// <summary>
    /// Class containing spline and its neighbors.
    /// </summary>
    public class SplineSegment
    {
        public readonly Spline spline;
        public readonly List<string> NodeIdentifiers;
        public Vector3 StartPoint => spline.startPoint;
        public Vector3 EndPoint => spline.endPoint;
        public List<SplineSegment> startNeighbors;
        public List<SplineSegment> endNeighbors;
        public readonly List<int> shortestPathInd;
        public readonly List<int> shortestPathIndAtStart;
        public readonly List<int> shortestPathIndAtEnd;
        public readonly string JunctionID;
        public readonly bool isJunction;

        /// <summary>
        /// Initializes a new instance of  <see cref="MazeFrameSplines.SplineSegment"/> class.
        /// </summary>
        /// <param name="spline">Spline.</param>
        /// <param name="isJunction">Whether spline is at junction.</param>
        public SplineSegment(Spline spline, List<string> nodeIdentifiers, List<int> shortestPathInd, List<int> shortestPathIndAtStart, List<int> shortestPathIndAtEnd, bool isJunction)
        {
            this.spline = spline;
            this.NodeIdentifiers = nodeIdentifiers;
            this.isJunction = isJunction;
            this.shortestPathInd = shortestPathInd;
            this.shortestPathIndAtStart = shortestPathIndAtStart;
            this.shortestPathIndAtEnd = shortestPathIndAtEnd;
            if (isJunction)
            { this.JunctionID = spline.CurveSegments[0].controlPoints[2].ToString(); }
            this.startNeighbors = new List<SplineSegment>();
            this.endNeighbors = new List<SplineSegment>();
        }
    }


    /// <summary>
    /// Returns wether node's normal should be oriented up.
    /// </summary>
    /// <returns><c>true</c>, if node's normal should be up, <c>false</c> otherwise.</returns>
    /// <param name="node">Node.</param>
    private bool IsNodePositionOrientedUp(MazeNode node)
    {
        List<string> list = new List<string> { "start", "end", "interim-start", "interim-end" };
        //List<string> list = new List<string> { "start", "end", "entry", "exit", "interim-start", "interim-end" };
        return node.DoesIdentifierContainString(list);
    }

    /// <summary>
    /// Returns wether node is connected ot node whose normal should be oriented up.
    /// </summary>
    /// <returns><c>true</c>, if node is connect to node whose normal should be up, <c>false</c> otherwise.</returns>
    /// <param name="node">Node.</param>
    private bool IsNodeConnectedToOrientedUp(MazeNode node)
    {
        bool answer = false;
        foreach (MazeNode neigh in node.ConnectedNeighbors)
        { answer = IsNodePositionOrientedUp(neigh); if (answer) { break; } }
        return answer;
    }
}
