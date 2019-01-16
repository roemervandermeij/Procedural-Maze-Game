using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class representing spline created from 1 or more N-Degree Bezier curve segments (<see cref="NDegreeBezierCurve"/>).
/// </summary>
public class Spline
{
    /// <summary>
    /// Current starting point of spline.
    /// </summary>
    public Vector3 startPoint;
    /// <summary>
    /// Current end point of spline.
    /// </summary>
    public Vector3 endPoint;
    /// <summary>
    /// Gets the current number of curve segments.
    /// </summary>
    /// <value>The current number of curve segments.</value>
    public int NCurveSegments => nCurveSegments;
    /// <summary>
    /// Gets the total number of Bezier control points in all curve segments.
    /// </summary>
    /// <value>The total number of Bezier control points in all curve segments.</value>
    public int NControlPoints => nControlPoints;
    /// <summary>
    /// Gets the number of unique Bezier control points in all curve segments.
    /// </summary>
    /// <value>The number of unique control points.</value>
    public int NUniqueControlPoints => (nControlPoints - (nCurveSegments - 1));
    /// <summary>
    /// The number of samples to use whenever sampling the curve is required.
    /// </summary>
    /// <value>Number of samples to use.</value>
    public int NSamplesToUse => NUniqueControlPoints * NSamplesPerControlPoint;
    /// <summary>
    /// Returns list of current Bezier curve segments.
    /// </summary>
    /// <value>List of (<see cref="BezierCurveSegment"/>).</value>
    public List<BezierCurveSegment> CurveSegments => curveSegments;
    /// <summary>
    /// Default function to obtain normal at point t.
    /// </summary>
    /// <value>The get normal at t.</value>
    public NormalFunction DefaultGetNormalAtT;
    public delegate Vector3 NormalFunction(float t);

    public float SplineTotalDistance => GetSplineTotalDistance();
    private float[] distanceTable = new float[0];
    private Vector3[] positionTable = new Vector3[0];

    private List<BezierCurveSegment> curveSegments;
    private int nControlPoints;
    private int nCurveSegments;
    private readonly int NSamplesPerControlPoint;
    private readonly int maximumDegree;
    private readonly int minimumDegree;
    private RotationMinimizingFrames rotationMinimizingFrames;
    private Vector3 NormalReferencePosition = Vector3.zero.NaN();

    /// <summary>
    /// Initializes a new instance of <see cref="Spline"/> class.
    /// </summary>
    /// <param name="controlPoints">Optional Bezier control points for first curve segment.</param>
    /// <param name="maximumDegree">Maximum degree of all Bezier curve segments for this spline.
    /// If new curve segments' degree exceeds this maximum, they will be split up and form separate curve segments.</param>
    /// <param name="minimumDegree">Minimum degree of all Bezier curve segments for this spline. 
    /// If new curve segments' degree fall below this minimum, additional control points will be added between last two points.</param>
    public Spline(Vector3[] controlPoints = null, int maximumDegree = 4, int minimumDegree = 4, int NSamplesPerControlPoint = 20)
    {
        nControlPoints = 0;
        nCurveSegments = 0;
        curveSegments = new List<BezierCurveSegment>();
        this.maximumDegree = maximumDegree;
        this.minimumDegree = minimumDegree;
        this.NSamplesPerControlPoint = NSamplesPerControlPoint;
        if (controlPoints != null)
        {
            this.AddSegment(controlPoints);
            this.rotationMinimizingFrames = new RotationMinimizingFrames(NSamplesToUse, this.GetPointOnSpline, this.GetTangentToPointOnSpline);
        }
        DefaultGetNormalAtT = this.GetMinimallyRotatingNormalToPointOnSpline;
    }

    /// <summary>
    /// Adds a new Bezier curve segment to spline. The new segment and the previous last segment will have a 
    /// control point added between the last two and the first two control points respectively, to ensure spline 
    /// continuity between segments. 
    /// </summary>
    /// <param name="controlPoints">Control points of new Bezier curve. If this exceeds the maximum specified 
    /// upon spline instantiation, they will be split up accordingly.</param>
    public void AddSegment(Vector3[] controlPoints)
    {
        // Cut up controlpoints to targetN degree bezier if necessary by calling AddSegment recursively
        if (controlPoints.Length >= (maximumDegree + minimumDegree - 1))
        {
            int currN;
            if (controlPoints.Length >= maximumDegree)
            { currN = maximumDegree; }
            else
            { currN = controlPoints.Length; }

            // create two selections of control points 6: 1 2 3 4    4 5 6 
            Vector3[] sel1 = new Vector3[currN];
            Vector3[] sel2 = new Vector3[controlPoints.Length - (currN - 1)];
            for (int i = 0; i < currN; i++)
            { sel1[i] = controlPoints[i]; }
            for (int i = 0; i < sel2.Length; i++)
            { sel2[i] = controlPoints[i + currN - 1]; }
            this.AddSegment(sel1);
            this.AddSegment(sel2);
        }
        else
        {
            // Add segment
            if (nCurveSegments == 0)
            {
                curveSegments.Add(new BezierCurveSegment(controlPoints, minimumDegree));
                startPoint = curveSegments[0].startPoint;
            }
            else
            {
                // Ensure continuity between new and old segment
                List<BezierCurveSegment> segments = EnsureContinuityBetweenTwoSegmentsByMoving(curveSegments[nCurveSegments - 1], new BezierCurveSegment(controlPoints, minimumDegree));
                // Replace pre-segment 
                curveSegments[nCurveSegments - 1] = segments[0];
                //nControlPoints++;
                // Add post as new segment
                curveSegments.Add(segments[1]);
            }
            // Increment counters and recreate frame
            nCurveSegments++;
            nControlPoints += curveSegments[curveSegments.Count - 1].nControlPoints;
            endPoint = curveSegments[nCurveSegments - 1].endPoint;
            this.rotationMinimizingFrames = new RotationMinimizingFrames(NSamplesToUse, this.GetPointOnSpline, this.GetTangentToPointOnSpline);
        }
    }

    /// <summary>
    /// Get the nearest distance d on spline at a certain position, clamped to 
    /// fall on spline.
    /// </summary>
    /// <returns>The distance on spline from point.</returns>
    /// <param name="position">Point.</param>
    public float GetDistanceOnSplineFromPosition(Vector3 position, bool reverseD = false)
    {
        ComputeDistanceAndPositionTablesIfNecessary();
        // Find the two closest positions
        List<int> indices = new List<int>(2);
        indices.Add(0);
        for (int i = 0; i < NSamplesToUse; i++)
        {
            if (Vector3.Distance(position, positionTable[i]) < Vector3.Distance(position, positionTable[indices[0]]))
            { indices[0] = i; }
        }
        if (indices[0] == 0) { indices.Add(1); }
        else if (indices[0] == NSamplesToUse - 1) { indices.Add(NSamplesToUse - 2); }
        else
        {
            if (Vector3.Distance(position, positionTable[indices[0] - 1]) < Vector3.Distance(position, positionTable[indices[0] + 1]))
            { indices.Add(indices[0] - 1); }
            else
            { indices.Add(indices[0] + 1); }
        }
        indices.Sort();
        // Find closest point on line between the two points on spline
        position = Utilities.FindClosestPointOnLineOfPositionWithClamping(positionTable[indices[0]], positionTable[indices[1]], position);
        // Return interpolated distance value based on distance from the two points on spline
        float perc = Vector3.Distance(positionTable[indices[0]], position) / Vector3.Distance(positionTable[indices[0]], positionTable[indices[1]]);
        float interpDist = Mathf.Lerp(distanceTable[indices[0]], distanceTable[indices[1]], perc);
        if (reverseD) { interpDist = SplineTotalDistance - interpDist; }
        return interpDist;
    }

    ///// <summary>
    ///// Get a point on the spline, at distance from spline start d.
    ///// Do this by creating/reading a table of positions along the 
    ///// spline and interpolating.
    ///// </summary>
    ///// <returns>Point on spline.</returns>
    ///// <param name="d">Distance value from start of spline.</param>
    //public Vector3 GetPointOnSplineAtDistance(float d)
    //{
    //    ComputeDistanceAndPositionTablesIfNecessary();
    //    List<int> indices = GetDistanceTableIndicesSurroundingDistance(d);
    //    // Get positions at surrounding indices
    //    Vector3 pos1 = positionTable[indices[0]];
    //    Vector3 pos2 = positionTable[indices[1]];
    //    // Interpolate position given distance of value d from both distances
    //    float perc = (d - distanceTable[indices[0]]) / (distanceTable[indices[1]] - distanceTable[indices[0]]);
    //    return Vector3.Lerp(pos1, pos2, perc);
    //}

    /// <summary>
    /// Convert distance value to parameter t.
    /// </summary>
    /// <param name="d">Distance value.</param>
    public float GetTAtDistance(float d, bool reverseD = false)
    {
        ComputeDistanceAndPositionTablesIfNecessary();
        if (reverseD) { d = SplineTotalDistance - d; }
        List<int> indices = GetDistanceTableIndicesSurroundingDistance(d);
        float t1 = indices[0] * (1f / (NSamplesToUse - 1));
        float t2 = indices[1] * (1f / (NSamplesToUse - 1));
        // Interpolate t given distance of value d from both distances
        float perc = (d - distanceTable[indices[0]]) / (distanceTable[indices[1]] - distanceTable[indices[0]]);
        return Mathf.Lerp(t1, t2, perc);
    }

    /// <summary>
    /// Get indices into distance table surrounding distance value.
    /// </summary>
    /// <returns>The distance table indices surrounding distance.</returns>
    /// <param name="d">D.</param>
    private List<int> GetDistanceTableIndicesSurroundingDistance(float d)
    {
        ComputeDistanceAndPositionTablesIfNecessary();
        if (d > SplineTotalDistance || d < 0) { throw new System.ArgumentException("Distance exceeds 0<->spline length bounds."); }
        // Find indices surrounding value d
        List<int> indices = new List<int>(2);
        indices.Add(0);
        for (int i = 0; i < NSamplesToUse; i++)
        {
            if (distanceTable[i] < d) { indices[0] = i; }
            else { break; }
        }
        if (indices[0] == NSamplesToUse - 1) { indices.Add(indices[0] - 1); indices.Sort(); }
        else { indices.Add(indices[0] + 1); }
        //Debug.Log("Dist along spline: d = " + d + " | " + distanceTable[indices[0]] + " - " + distanceTable[indices[1]]);
        return indices;
    }

    /// <summary>
    /// Get a point on the spline, with <paramref name="t"/> (0-1) reflecting equally sampled 
    /// distance along the spline, irrespective of how many control points each individual segment has. 
    /// </summary>
    /// <returns>Point on spline.</returns>
    /// <param name="t">T.</param>
    public Vector3 GetPointOnSpline(float t)
    {
        t = Mathf.Clamp01(t);
        int sectionInd;
        GetSectionIndAndCorrectedT(ref t, out sectionInd);
        return curveSegments[sectionInd].curve.GetPointOnCurve(t);
    }

    /// <summary>
    /// Get tangent to point on spline, with <paramref name="t"/> (0-1) reflecting parameter t 
    /// along the spline, irrespective of how many control points each individual segment has. 
    /// </summary>
    /// <returns>Tangent to point on spline.</returns>
    /// <param name="t">T.</param>
    public Vector3 GetTangentToPointOnSpline(float t)
    {
        t = Mathf.Clamp01(t);
        int sectionInd;
        GetSectionIndAndCorrectedT(ref t, out sectionInd);
        return curveSegments[sectionInd].curve.GetTangentToPointOnCurve(t);
    }

    /// <summary>
    /// Get normal of point on spline, with <paramref name="t"/> (0-1) reflecting parameter t 
    /// along the spline, irrespective of how many control points each individual segment has. 
    /// </summary>
    /// <returns>Normal of point on spline.</returns>
    /// <param name="t">T.</param>
    public Vector3 GetNormalToPointOnSpline(float t)
    {
        t = Mathf.Clamp01(t);
        int sectionInd;
        GetSectionIndAndCorrectedT(ref t, out sectionInd);
        return curveSegments[sectionInd].curve.GetNormalToPointOnCurve(t);
    }

    /// <summary>
    /// Gets the normal to point on spline based on a reference position.
    /// </summary>
    /// <returns>Normal to point on spline.</returns>
    /// <param name="t">T.</param>
    public Vector3 GetNormalToPointOnSplineBasedOnReferencePosition(float t)
    {
        if (NormalReferencePosition.IsNaN()) { throw new System.Exception("Need to set NormalReferencePosition first."); }
        Vector3 pointOnSpline = GetPointOnSpline(t);
        return (NormalReferencePosition - pointOnSpline).normalized;
    }

    /// <summary>
    /// Get minimally rotating normal to point on spline, with <paramref name="t"/> (0-1) reflecting parameter t
    /// along the spline, irrespective of how many control points each individual segment has. 
    /// </summary>
    /// <returns>Minimally rotating normal to point on spline.</returns>
    /// <param name="t">T.</param>
    public Vector3 GetMinimallyRotatingNormalToPointOnSpline(float t)
    {
        t = Mathf.Clamp01(t);
        // First, compute frames if not yet done so
        if (rotationMinimizingFrames.Frames == null) { ComputeRotationMinimizingFrames(); }
        // Then, get normal
        return rotationMinimizingFrames.GetMinimallyRotatingNormalToPointOnCurve(t);
    }

    /// <summary>
    /// Sets the normal with which the first RMF should start.
    /// </summary>
    /// <param name="startNormal">Normal vector.</param>
    public void SetRMFStartNormal(Vector3 startNormal)
    {
        rotationMinimizingFrames.StartNormal = startNormal.normalized;
        ComputeRotationMinimizingFrames();
    }
    /// <summary>
    /// Sets the normal with which the last RMF should end.
    /// </summary>
    /// <param name="endNormal">Normal vector.</param>
    public void SetRMFEndNormal(Vector3 endNormal)
    {
        rotationMinimizingFrames.EndNormal = endNormal.normalized;
        ComputeRotationMinimizingFrames();
    }

    /// <summary>
    /// Computes the Rotation Minimizing Frames based on current curve segments.
    /// </summary>
    public void ComputeRotationMinimizingFrames()
    {
        rotationMinimizingFrames.ComputeRotationMinimizingFrames();
    }

    /// <summary>
    /// Set the reference position with which to compute the normal towards, based on
    /// point on the spline (t).
    /// </summary>
    public void SetNormalReferencePosition(Vector3 normalReferencePosition)
    {
        this.NormalReferencePosition = normalReferencePosition;
    }

    /// <summary>
    /// Conveniency method to get t from sample index.
    /// </summary>
    /// <returns>t.</returns>
    /// <param name="sampleInd">Sample index, from 0 to <see cref="NSamplesToUse"/></param>
    public float SampleIndToT(int sampleInd)
    {
        return 0 + (1f / (NSamplesToUse - 1)) * sampleInd;
    }

    /// <summary>
    /// Convert spline t to curve segment t and section index, with <paramref name="t"/> (0-1) reflecting parameter t 
    /// along the spline, irrespective of how many control points each individual segment has. 
    /// </summary>
    /// <param name="t">T.</param>
    /// <param name="sectionInd">Section ind.</param>
    public void GetSectionIndAndCorrectedT(ref float t, out int sectionInd)
    {
        // Interpret t as requesting controlPoint-to-controlPoint sampled points
        // find left side control point t is supposed to index
        int globalCPInd; // index to 0-(NUniqueControlPoints-1)
        int localCPInd; // index to 0-(Sections[ind].nControlPoints-1)
        float nIntervals = NUniqueControlPoints - 1; // number CP to CP intervals
        float intervalWidth = 1f / nIntervals; // width of interval, in global t, between two control points

        if (Mathf.Approximately(t, 0f))
        {
            globalCPInd = 0;
            sectionInd = 0;
            localCPInd = 0;
        }
        else if (Mathf.Approximately(t, 1f))
        {
            globalCPInd = NUniqueControlPoints - 1;
            sectionInd = nCurveSegments - 1;
            localCPInd = curveSegments[sectionInd].nControlPoints - 1;
        }
        else
        {
            // find section which has control point in it
            globalCPInd = Mathf.FloorToInt(t / intervalWidth);
            localCPInd = -1;
            sectionInd = 0;
            int[] sectionCPind = { 0, curveSegments[0].nControlPoints - 1 };
            foreach (BezierCurveSegment sec in curveSegments)
            {
                if (globalCPInd >= sectionCPind[0] && globalCPInd < sectionCPind[1])
                {
                    localCPInd = globalCPInd - sectionCPind[0];
                    break;
                }
                sectionInd++;
                sectionCPind[0] = sectionCPind[1];
                sectionCPind[1] += curveSegments[sectionInd].nControlPoints - 1;
                // Safety check
                if (sectionInd == nCurveSegments) { throw new System.Exception("Getting section index failed."); }
            }
        }
        // scale t to scale of single controlpoint
        t = t - (globalCPInd * intervalWidth); // t minus intervals to the left = t in first interval
        t = t * nIntervals; // t times number CP-CP intervals = t on single interval
        // scale t to scale of current section
        int nSectionIntervals = curveSegments[sectionInd].nControlPoints - 1;
        float sectionIntervalWidth = 1f / nSectionIntervals;
        t = (t * sectionIntervalWidth) + (localCPInd * sectionIntervalWidth);
        Debug.Assert(t <= (1 + Mathf.Epsilon), "t = " + t);
    }

    /// <summary>
    /// Convert curve segment t to spline t given section index, with <paramref name="t"/> (0-1) reflecting parameter t 
    /// along the curve of the segment. 
    /// </summary>
    /// <returns>Spline-wise T.</returns>
    /// <param name="t">T.</param>
    /// <param name="sectionInd">Section ind.</param>
    public float GetTFromSectionIndAndCorrectedT(float t, int sectionInd)
    {
        float nIntervals = NUniqueControlPoints - 1; // number CP to CP intervals
        float intervalWidth = 1f / nIntervals; // width of interval, in global t, between two control points
        int nSectionIntervals = curveSegments[sectionInd].nControlPoints - 1;
        float sectionIntervalWidth = 1f / nSectionIntervals;
        // First, scale t to scale of single controlpoint
        int localCPInd = Mathf.FloorToInt(t / sectionIntervalWidth);
        t = t - (localCPInd * sectionIntervalWidth);
        t = t * nSectionIntervals;
        // Then, scale to scale of global width
        t = t * intervalWidth;
        // Finally, if necesary, calculate cumsum of unique control points to add offset
        float offset = 0;
        if (sectionInd != 0)
        {
            int cumSumUniqueCP = 0;
            for (int i = 0; i < sectionInd; i++)
            { cumSumUniqueCP += curveSegments[i].nControlPoints - 1; }
            offset = (cumSumUniqueCP + localCPInd) * intervalWidth;
        }
        else { offset = localCPInd * intervalWidth; }
        t = t + offset;
        return t;
    }

    /// <summary>
    /// Returns the total spline distance.
    /// </summary>
    private float GetSplineTotalDistance()
    {
        ComputeDistanceAndPositionTablesIfNecessary();
        return distanceTable[distanceTable.Length - 1];
    }

    /// <summary>
    /// Computes the distance and position tables of samples along the spline.
    /// </summary>
    private void ComputeDistanceAndPositionTablesIfNecessary()
    {
        if (distanceTable.Length != NSamplesToUse || positionTable.Length != NSamplesToUse)
        {
            distanceTable = new float[NSamplesToUse];
            positionTable = new Vector3[NSamplesToUse];
            Vector3 prevPoint = GetPointOnSpline(0);
            distanceTable[0] = 0;
            positionTable[0] = prevPoint;
            for (int i = 1; i < NSamplesToUse; i++)
            {
                Vector3 currPoint = GetPointOnSpline(i * (1f / (NSamplesToUse - 1f)));
                distanceTable[i] = distanceTable[i - 1] + Vector3.Distance(currPoint, prevPoint);
                positionTable[i] = currPoint;
                prevPoint = currPoint;
            }
        }
    }

    /// <summary>
    /// Ensures the continuity between two Bezier curve segments by adding a control point between the last two of the
    /// first segment and the first two of the second segment. These control points are such that the end point of the first curve and
    /// the starting point of the second curve have identical tangents.
    /// </summary>
    /// <returns>The continuity between segments.</returns>
    /// <param name="preSeg">First Bezier curve segment.</param>
    /// <param name="postSeg">Second Bezier curve segment.</param>
    private List<BezierCurveSegment> EnsureContinuityBetweenTwoSegmentsByAdding(BezierCurveSegment preSeg, BezierCurveSegment postSeg)
    {
        float distFactor = 0.5f; // 1 results in roughly the length of the smallest of the two legs, 2 in twice that, 0.5 in half that
        // Duplicate N_end-1 and M1 to as a little cheat to ensure proper segment/tangent length
        Vector3[] preSegCP = preSeg.controlPoints;
        System.Array.Resize(ref preSegCP, preSegCP.Length + 1);
        preSegCP[preSegCP.Length - 1] = preSegCP[preSegCP.Length - 2];
        preSegCP[preSegCP.Length - 2] = preSegCP[preSegCP.Length - 3];
        Vector3[] postSegCP = postSeg.controlPoints;
        System.Array.Resize(ref postSegCP, postSeg.nControlPoints + 1);
        for (int i = postSegCP.Length - 1; i > 2; i--) { postSegCP[i] = postSegCP[i - 1]; }
        Vector3 lastTangentPre = preSeg.curve.GetTangentToPointOnCurve(1f);
        Vector3 firstTangentPost = postSeg.curve.GetTangentToPointOnCurve(0);
        Vector3[] positions = ComputeC1ContinuousControlPoints(preSegCP, lastTangentPre, postSegCP, firstTangentPost, distFactor);
        // Move control points and compute new segments
        preSegCP[preSegCP.Length - 2] = positions[0];
        postSegCP[1] = positions[1];
        return new List<BezierCurveSegment> { new BezierCurveSegment(preSegCP, minimumDegree), new BezierCurveSegment(postSegCP, minimumDegree) };
    }

    /// <summary>
    /// Ensures the continuity between two Bezier curve segments by moving the first to last control point of the
    /// first segment and the second control point of the second segment. Their positions are such that the end point of the first curve and
    /// the starting point of the second curve have identical tangents (C1 continuous).
    /// </summary>
    /// <returns>The continuity between segments.</returns>
    /// <param name="preSeg">First Bezier curve segment.</param>
    /// <param name="postSeg">Second Bezier curve segment.</param>
    private List<BezierCurveSegment> EnsureContinuityBetweenTwoSegmentsByMoving(BezierCurveSegment preSeg, BezierCurveSegment postSeg)
    {
        float distFactor = 0.7f; // 1 results in roughly the length of the smallest of the two legs, 2 in twice that, 0.5 in half that
        // Compute new point positions
        Vector3[] preSegCP = preSeg.controlPoints;
        Vector3[] postSegCP = postSeg.controlPoints;
        Vector3 lastTangentPre = preSeg.curve.GetTangentToPointOnCurve(1f);
        Vector3 firstTangentPost = postSeg.curve.GetTangentToPointOnCurve(0);
        Vector3[] positions = ComputeC1ContinuousControlPoints(preSegCP, lastTangentPre, postSegCP, firstTangentPost, distFactor);
        // Move control points and compute new segments
        preSegCP[preSegCP.Length - 2] = positions[0];
        postSegCP[1] = positions[1];
        return new List<BezierCurveSegment> { new BezierCurveSegment(preSegCP, minimumDegree), new BezierCurveSegment(postSegCP, minimumDegree) };
    }


    /// <summary>
    /// Computes C1 continuous control point positions, based on given control points,
    /// to make the given segments C1 continous. This is the case when preSeg t=1 tangent (tan1) and
    /// the postSeg t=0 tangent are identical. For this to be true, we need:
    /// (1) tan1 and tan2 to have the same direction, and
    /// (2) tan1 and tan2 to have the same length. This is true when N_end==M_0, N_end-1 and M_1 lie on the 
    /// same line, and when the length of N_end-N_end-1 is equal to the length of M_0-M_1.
    /// (N=preSeg/M=postSeg nControlPoints).
    /// </summary>
    /// <returns>Control points N_end-1 and M_1.</returns>
    /// <param name="preSegCP">Pre-join segment control points.</param>
    /// <param name="preSegLastTan">Pre-join segment tangent at t=1.</param>
    /// <param name="postSegCP">Post-join segment control points.</param>
    /// <param name="preSegLastTan">Post-join segment tangent at t=0.</param>
    /// <param name="distFactor">Factor influencing how far the new nodes are from N_end/M_0.</param>
    private Vector3[] ComputeC1ContinuousControlPoints(Vector3[] preSegCP, Vector3 preSegLastTan, Vector3[] postSegCP, Vector3 postSegFirstTan, float distFactor)
    {
        // distFactor: 1 results in roughly the length of the smallest of the two legs, 2 in twice that, 0.5 in half that
        // Compute new tangent
        Vector3 newTanDir = ((preSegLastTan.normalized + postSegFirstTan.normalized) / 2).normalized;
        // Compute distance for new control points from center 
        float preLegDist = Vector3.Distance(preSegCP[preSegCP.Length - 1], preSegCP[preSegCP.Length - 2]);
        float postLegDist = Vector3.Distance(postSegCP[0], postSegCP[1]);
        // Take minimum distance, with a rough weighting by mean(nN,nM)
        float distToPoint = Mathf.Min(preLegDist * preSegCP.Length, postLegDist * postSegCP.Length) / ((preSegCP.Length + postSegCP.Length) / 2);
        // Divide distance by mean(nN,nM), as the final positions will be multiplied with N/M again
        distToPoint = distToPoint / ((preSegCP.Length + postSegCP.Length) / 2);
        // Weigh by factor given during input 
        distToPoint = distToPoint * distFactor;
        // Compute new positions
        Vector3 preSegCPPos = preSegCP[preSegCP.Length - 2] = preSegCP[preSegCP.Length - 1] - (newTanDir * distToPoint * preSegCP.Length);
        Vector3 postSegCPPos = postSegCP[0] + (newTanDir * distToPoint * postSegCP.Length);
        return new Vector3[] { preSegCPPos, postSegCPPos };
    }


    /// <summary>
    /// Class representing individual Bezier curve segment
    /// </summary>
    public class BezierCurveSegment
    {
        public readonly Vector3 startPoint;
        public readonly Vector3 endPoint;
        public readonly Vector3[] controlPoints;
        public int nControlPoints;
        public NDegreeBezierCurve curve;

        /// <summary>
        /// Initializes a new instance of <see cref="Spline.BezierCurveSegment"/> class.
        /// </summary>
        /// <param name="controlPoints">Control points to be used for creating new instance of <see cref="NDegreeBezierCurve"/> class.</param>
        /// <param name="minimumDegree">Minimum number of control points. If the number of control points falls below this, 
        /// additional control points are added.</param>
        public BezierCurveSegment(Vector3[] controlPoints, int minimumDegree)
        {
            if (controlPoints.Length < minimumDegree)
            {
                if (controlPoints.Length == 1) { throw new System.Exception("NControlPoints should be >1"); }
                // Add additional points deterministically
                if (minimumDegree == 4 && (controlPoints.Length == 3))
                {
                    // Replace center point by intermediates
                    System.Array.Resize(ref controlPoints, minimumDegree);
                    Vector3 oldCenter = controlPoints[1];
                    controlPoints[3] = controlPoints[2];
                    controlPoints[2] = oldCenter + (controlPoints[3] - oldCenter) / 2;
                    controlPoints[1] = oldCenter - (oldCenter - controlPoints[0]) / 2;
                }
                else if (minimumDegree == 4 && (controlPoints.Length == 2))
                {
                    // Linearly space all 4 points
                    System.Array.Resize(ref controlPoints, minimumDegree);
                    controlPoints[3] = controlPoints[1];
                    Vector3 spacing = (controlPoints[3] - controlPoints[0]) / 3;
                    controlPoints[1] = controlPoints[0] + spacing;
                    controlPoints[2] = controlPoints[0] + 2 * spacing;
                }
                else
                { throw new System.ArgumentException("Expansion of control points to specified minimum degree not supported."); }
            }

            //// Add evenly spaced points between last two points based on on minimum degree
            //if (controlPoints.Length < minimumDegree)
            //{
            //    int diff = minimumDegree - controlPoints.Length;
            //    int orgN = controlPoints.Length;
            //    System.Array.Resize(ref controlPoints, minimumDegree);
            //    controlPoints[controlPoints.Length - 1] = controlPoints[orgN - 1]; // last point is previous last point
            //    Vector3 partialDistBetweenLastPoints = (controlPoints[orgN - 1] - controlPoints[orgN - 2]) / (1 + diff);
            //    for (int i = 0; i < diff; i++)
            //    { controlPoints[orgN - 1 + i] = controlPoints[orgN - 2] + (partialDistBetweenLastPoints * (i + 1)); }
            //}

            this.startPoint = controlPoints[0];
            this.endPoint = controlPoints[controlPoints.Length - 1];
            this.controlPoints = controlPoints;
            curve = new NDegreeBezierCurve(controlPoints);
            this.nControlPoints = controlPoints.Length;
        }
    }
}
