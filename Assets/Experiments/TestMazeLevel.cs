using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMazeLevel : MonoBehaviour
{

    public MazeFrame mazeFrame;
    MazeFrameSplines mazeFrameSplines;

    public GravityFrameController gravityFrameController;

    void Start()
    {
        // FIXME is new object the way to go?? Or scriptable object without new keyword?? Or static??
        MazeFrameCreator mazeFrameCreator = new MazeFrameCreatorMeshIcosahedron(2, 4)
        {
            Scale = new Vector3(10, 10, 10),
            Jitter = 0f
        };

        mazeFrame = mazeFrameCreator.GenerateEmptyMazeFrame();
        for (int i = 0; i <= 3; i++)
        {
            mazeFrame.SetActiveQuadrant(i);
            mazeFrameCreator.GenerateMaze(ref mazeFrame);
            mazeFrame.SetOnShortestPath(mazeFrame.Quadrants[i], i); // HACKY
        }
        mazeFrame.ActivateAllNodes();

        //mazeFrame.KeepOnlyShortestPathConnections();

        mazeFrame.AddOffsetStartAndEndNodes(Vector3.Scale(Vector3.forward, mazeFrameCreator.Scale) * 3, true);
        mazeFrame.AddPathSegments();

        //mazeFrameSplines = new MazeFrameSplines(mazeFrame);
        //GameObject mazeObjects = null;
        //MazePopulator.PopulateWithSplineFittedBars(mazeFrame, ref mazeFrameSplines, ref mazeObjects, new Vector3(2, .15f, 1));


        //GameObject player = null;
        //GameObject cameraRig = null;
        //MazePopulator.PlacePlayerAndCamera(ref player, ref cameraRig, mazeFrame.StartNode.Position + (Vector3.Scale(Vector3.forward, mazeFrameCreator.Scale) * 1.5f));

    }



    private void OnDrawGizmos()
    {
        if (mazeFrame != null)
        {

            //foreach (MazeFrameSplines.SplineSegment segment in mazeFrameSplines.SplineSegments)
            //{
            //    Gizmos.color = Color.white;
            //    Vector3 prevPos = segment.StartPoint;
            //    //for (int i = 1; i < segment.spline.NSamplesToUse; i++)
            //    //{
            //    //    float t = i * (1f / (segment.spline.NSamplesToUse - 1));
            //    //    Vector3 newPos = segment.spline.GetPointOnSpline(t);
            //    //    Gizmos.DrawLine(prevPos, newPos);
            //    //    prevPos = newPos;
            //    //}
            //    Gizmos.DrawLine(segment.StartPoint, segment.EndPoint);
            //    float scaleFac = 0.1f;
            //    Gizmos.color = Color.cyan;
            //    Vector3 shift = (segment.EndPoint - segment.StartPoint).normalized * scaleFac * mazeFrame.Scale.ComponentMin();
            //    for (int i = 0; i < segment.startNeighbors.Count; i++)
            //    { Gizmos.DrawSphere(segment.StartPoint + (shift * (i + 1)), scaleFac / 2f * mazeFrame.Scale.ComponentMin()); }
            //    shift = (segment.StartPoint - segment.EndPoint).normalized * scaleFac * mazeFrame.Scale.ComponentMin();
            //    for (int i = 0; i < segment.endNeighbors.Count; i++)
            //    { Gizmos.DrawSphere(segment.EndPoint + (shift * (i + 1)), scaleFac / 2f * mazeFrame.Scale.ComponentMin()); }
            //}


            //foreach (MazeNode node in mazeFrame.Nodes)
            //{

            //    Gizmos.color = Color.white;
            //    Gizmos.DrawSphere(node.Position, .05f * mazeFrame.Scale.ComponentMin());

            //    foreach (MazeNode neigh in node.AllNeighbors)
            //    {
            //        { Gizmos.color = Color.white; }
            //        Gizmos.DrawLine(node.Position, neigh.Position);
            //    }
            //}
            //Gizmos.color = Color.green;
            //Gizmos.DrawSphere(mazeFrame.StartNode.Position, .1f * mazeFrame.Scale.ComponentMin());
            //Gizmos.color = Color.red;
            //Gizmos.DrawSphere(mazeFrame.EndNode.Position, .1f * mazeFrame.Scale.ComponentMin());


            //foreach (MazeNode node in mazeFrame.Nodes)
            //{
            //    // Nodes
            //    if (node.shortestPathInd.Count > 0 && (node.ConnectedNeighbors.Count > 2))
            //    { Gizmos.color = Color.cyan; }
            //    else if (node.shortestPathInd.Count > 0)
            //    { Gizmos.color = Color.magenta; }
            //    else if ((bool)node.OnLoop)
            //    { Gizmos.color = Color.blue; }
            //    else if ((bool)node.OnDeadEnd)
            //    { Gizmos.color = Color.yellow; }
            //    else if ((bool)node.NotConnectedToPath)
            //    { Gizmos.color = Color.green; }
            //    else
            //    { Gizmos.color = Color.white; }
            //    Gizmos.DrawSphere(node.Position, .05f * mazeFrame.Scale.ComponentMin());

            //    foreach (MazeNode neigh in node.ConnectedNeighbors)
            //    {
            //        if (node.shortestPathInd.Count > 0 && neigh.shortestPathInd.Count > 0)
            //        {
            //            if ((node.shortestPathInd.Count == 1 || neigh.shortestPathInd.Count == 1) && node.shortestPathInd.Contains(0) && neigh.shortestPathInd.Contains(0))
            //            { Gizmos.color = Color.blue; }
            //            else if ((node.shortestPathInd.Count == 1 || neigh.shortestPathInd.Count == 1) && node.shortestPathInd.Contains(1) && neigh.shortestPathInd.Contains(1))
            //            { Gizmos.color = Color.red; }
            //            else if ((node.shortestPathInd.Count == 1 || neigh.shortestPathInd.Count == 1) && node.shortestPathInd.Contains(2) && neigh.shortestPathInd.Contains(2))
            //            { Gizmos.color = Color.yellow; }
            //            else if (node.shortestPathInd.Count > 1 && neigh.shortestPathInd.Count > 1)
            //            { Gizmos.color = Color.magenta; }
            //        }
            //        //else if ((bool)node.OnLoop && (bool)neigh.OnLoop)
            //        //{ Gizmos.color = Color.blue; }
            //        //else if ((bool)node.OnDeadEnd || (bool)neigh.OnDeadEnd)
            //        //{ Gizmos.color = Color.yellow; }
            //        //else if ((bool)node.NotConnectedToPath || (bool)neigh.NotConnectedToPath)
            //        //{ Gizmos.color = Color.green; }
            //        else
            //        { Gizmos.color = Color.white; }
            //        // Connections
            //        Gizmos.DrawLine(node.Position, neigh.Position);
            //    }
            //}
            //Gizmos.color = Color.green;
            //Gizmos.DrawSphere(mazeFrame.StartNode.Position, .1f * mazeFrame.Scale.ComponentMin());
            //Gizmos.color = Color.red;
            //Gizmos.DrawSphere(mazeFrame.EndNode.Position, .1f * mazeFrame.Scale.ComponentMin());


            //foreach (List<MazeNode> path in mazeFrame.PathSegments)
            //{
            //    for (int i = 0; i < path.Count - 1; i++)
            //    {
            //        if (path[i].ConnectedNeighbors.Contains(path[i + 1]))
            //        { Gizmos.color = Color.white; }
            //        else
            //        { Gizmos.color = Color.red; }
            //        Gizmos.DrawLine(path[i].Position, path[i + 1].Position);
            //    }
            //}


            //// Preview level
            //Vector3 offset = new Vector3(mazeFrame.Scale[0] / 20, mazeFrame.Scale[0] / 20, mazeFrame.Scale[0] / 20);
            //foreach (Spline spline in mazeFrameSpline)
            //{
            //    int nSample = spline.NControlPoints * 20;
            //    Vector3 pOut;
            //    Vector3 prevpOut = spline.startPoint;
            //    for (int i = 0; i < nSample; i++)
            //    {
            //        float t = (1f / nSample) + ((1f / nSample) * i);
            //        pOut = spline.GetPointOnSpline(t);
            //        //Debug.DrawLine(prevpOut + offset, pOut + offset, Color.yellow);
            //        prevpOut = pOut;

            //        Vector3 tOut = spline.GetMinimallyRotatingNormalToPointOnSpline(t).normalized;
            //        Debug.DrawRay(pOut, tOut, Color.red);
            //        //Vector3 nOut = spline.GetNormalToPointOnSpline(t);
            //        //Debug.DrawRay(pOut, nOut * 2, Color.red);
            //    }
            //}


            //// Preview level
            //foreach (MazeNode node in mazeFrame.Nodes)
            //{
            //    // Nodes
            //    if (node.shortestPathInd.Count > 0)
            //    {
            //        Gizmos.color = Color.magenta;
            //        Gizmos.DrawSphere(node.Position, .1f * mazeFrame.Scale.ComponentMin());
            //    }
            //    if ((bool)node.OnLoop)
            //    {
            //        Gizmos.color = Color.blue;
            //        Gizmos.DrawSphere(node.Position + new Vector3(0.1f, 0f, 0f), .1f * mazeFrame.Scale.ComponentMin());
            //    }
            //    if ((bool)node.OnDeadEnd)
            //    {
            //        Gizmos.color = Color.yellow;
            //        Gizmos.DrawSphere(node.Position + new Vector3(0.2f, 0f, 0f), .1f * mazeFrame.Scale.ComponentMin());
            //    }
            //    if ((bool)node.NotConnectedToPath)
            //    {
            //        Gizmos.color = Color.green;
            //        Gizmos.DrawSphere(node.Position + new Vector3(0.3f, 0f, 0f), .1f * mazeFrame.Scale.ComponentMin());
            //    }
            //    if (!(node.shortestPathInd.Count > 0) && !(bool)node.OnLoop && !(bool)node.OnDeadEnd && !(bool)node.NotConnectedToPath)
            //    {
            //        Gizmos.color = Color.white;
            //        Gizmos.DrawSphere(node.Position + new Vector3(0.4f, 0f, 0f), .1f * mazeFrame.Scale.ComponentMin());
            //    }


            //    foreach (MazeNode neigh in node.ConnectedNeighbors)
            //    {
            //        if (node.shortestPathInd.Count > 0 && neigh.shortestPathInd.Count > 0)
            //        { Gizmos.color = Color.magenta; }
            //        else if ((bool)node.OnLoop && (bool)neigh.OnLoop)
            //        { Gizmos.color = Color.blue; }
            //        else if ((bool)node.OnDeadEnd || (bool)neigh.OnDeadEnd)
            //        { Gizmos.color = Color.yellow; }
            //        else if ((bool)node.NotConnectedToPath || (bool)neigh.NotConnectedToPath)
            //        { Gizmos.color = Color.green; }
            //        else
            //        { Gizmos.color = Color.white; }
            //        // Connections
            //        Gizmos.DrawLine(node.Position, neigh.Position);
            //    }
            //}
            //Gizmos.color = Color.green;
            //Gizmos.DrawSphere(mazeFrame.StartNode.Position, .2f * mazeFrame.Scale.ComponentMin());
            //Gizmos.color = Color.red;
            //Gizmos.DrawSphere(mazeFrame.EndNode.Position, .2f * mazeFrame.Scale.ComponentMin());
        }
    }


}
