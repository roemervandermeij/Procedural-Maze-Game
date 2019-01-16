using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class provides the computation, storage, and interface for Rotation Minimizing Frames 
/// for a Bezier Curve or Spline. 
/// </summary>
public class RotationMinimizingFrames
{

    /// <summary>Fetches the Minimally Rotating Frames.</summary>
    /// <value>List of frames.</value>
    public List<Frame> Frames => frames;
    /// <summary>
    /// Fetches the amount of frames Rotation Minimizing Frames can be computed at.
    /// </summary>
    public int NFrames => nFrames;
    /// <summary>
    /// Gets or sets the approximate normal of the first frame.
    /// It will be made orthogonal to the local tangent.
    /// </summary>
    /// <value>The start normal.</value>
    public Vector3 StartNormal
    {
        get { return startNormal; }
        set { startNormal = value.normalized; }
    }
    /// <summary>
    /// Gets or sets the approximate normal of the last frame.
    /// It will be made orthogonal to the local tangent.
    /// </summary>
    /// <value>The end normal.</value>
    public Vector3 EndNormal
    {
        get { return endNormal; }
        set { endNormal = value.normalized; }
    }

    private Vector3 startNormal = Vector3.up;
    private Vector3 endNormal = Vector3.one.NaN();
    private List<Frame> frames;
    private readonly int nFrames;

    /// <summary>
    /// Method handle to obtain Vector3 position at point t in curve, with parameter t: 0-1.
    /// </summary>
    private readonly GetPointOnCurve getPointOnCurve;
    public delegate Vector3 GetPointOnCurve(float t);

    /// <summary>
    /// Method handle to obtain Vector3 tangent to curve at point t in curve, with parameter t: 0-1.
    /// </summary>
    private readonly GetTangentToPointOnCurve getTangentToPointOnCurve;
    public delegate Vector3 GetTangentToPointOnCurve(float t);

    /// <summary>
    /// Initializes a new instance of the <see cref="RotationMinimizingFrames"/> class.
    /// </summary>
    /// <param name="nFrames">Amount of frames Rotation Minimizing Frames can be computed at.</param>
    /// <param name="getPointOnCurve">Method handle to obtain Vector3 position at point t in curve, with parameter t: 0-1.</param>
    /// <param name="getTangentToPointOnCurve">Method handle to obtain Vector3 tangent to curve at point t in curve, with parameter t: 0-1.</param>
    public RotationMinimizingFrames(int nFrames, GetPointOnCurve getPointOnCurve, GetTangentToPointOnCurve getTangentToPointOnCurve)
    {
        this.nFrames = nFrames;
        this.getPointOnCurve = getPointOnCurve;
        this.getTangentToPointOnCurve = getTangentToPointOnCurve;
    }

    /// <summary>
    /// Compute the Rotation Minimizing Frames of Bezier Curve/Spline at steps of t in 1/nFrames-1 from 0-1.
    /// </summary>
    public void ComputeRotationMinimizingFrames()
    {
        // Implement algorithm for computing rotation minimizing frame 
        // (i.e., normals with minimal rotation from point to point)
        // This uses the double reflection method as presented in:
        // https://www.microsoft.com/en-us/research/wp-content/uploads/2016/12/Computation-of-rotation-minimizing-frames.pdf
        // DOI = 10.1145/1330511.1330513 http://doi.acm.org/10.1145/1330511.1330513
        // (following Table 1 pseudo code)

        // Initialize output and set initial frame 
        frames = new List<Frame>(nFrames);
        Vector3 currTvec = getTangentToPointOnCurve(0).normalized;
        Vector3 currRvec = LegaliseNormal(startNormal, currTvec);
        Vector3 currSvec = Vector3.Cross(currRvec, currTvec).normalized;
        frames.Add(new Frame(currRvec, currTvec, currSvec, 0));

        // start constructing RM frames
        Vector3 nextRvec;
        Vector3 nextTvec;
        Vector3 nextSvec;
        for (int i = 0; i < (nFrames - 1); i++)
        {
            float currT = 1f / (nFrames - 1) * i;
            float nextT = 1f / (nFrames - 1) * (i + 1);
            nextTvec = getTangentToPointOnCurve(nextT).normalized;

            // step 1
            Vector3 v1 = getPointOnCurve(nextT) - getPointOnCurve(currT);
            // step 2
            float c1 = Vector3.Dot(v1, v1);
            // step 3
            Vector3 riL = currRvec - (2f / c1) * Vector3.Dot(v1, currRvec) * v1;
            // step 4
            Vector3 tiL = currTvec - (2f / c1) * Vector3.Dot(v1, currTvec) * v1;
            // step 5
            Vector3 v2 = nextTvec - tiL;
            // step 6
            float c2 = Vector3.Dot(v2, v2);
            // step 7
            nextRvec = riL - (2f / c2) * Vector3.Dot(v2, riL) * v2;
            // step 8 
            nextSvec = Vector3.Cross(nextTvec, nextRvec);
            // step 9
            frames.Add(new Frame(nextRvec, nextTvec, nextSvec, nextT));

            // set initials for next i
            currRvec = nextRvec;
            currTvec = nextTvec;
            currSvec = nextSvec;

            //// DEBUG
            //Debug.DrawRay(GetPointOnCurve(nextT), nextRvec, Color.red);
            //Debug.DrawRay(GetPointOnCurve(nextT), nextTvec, Color.cyan);
            //Debug.DrawRay(GetPointOnCurve(nextT), nextSvec, Color.green);
            //// DEBUG
        }
        // Apply desired end normal
        CurveFrameNormalsTowardsEndNormal();
    }

    /// <summary>
    /// Adjust computed frames to curve towards target ending normal, where the first frame
    /// remains as is, and the last frame's normal becomes end normal
    /// </summary>
    private void CurveFrameNormalsTowardsEndNormal()
    {
        if (endNormal.IsNaN()) return;
        // First, determine whether endNormal or -endNormal results in less overall curving
        endNormal = LegaliseNormal(EndNormal, getTangentToPointOnCurve(1));
        float angle = Vector3.SignedAngle(frames[nFrames - 1].rvec, endNormal, frames[nFrames - 1].tvec);
        if (Mathf.Abs(Vector3.SignedAngle(frames[nFrames - 1].rvec, -endNormal, frames[nFrames - 1].tvec)) < Mathf.Abs(angle))
        {
            endNormal = -endNormal;
            angle = Vector3.SignedAngle(frames[nFrames - 1].rvec, endNormal, frames[nFrames - 1].tvec);
        }
        //float angle = Vector3.Angle(frames[nFrames - 1].rvec, endNormal);
        // Curve per frame
        for (int i = 0; i < nFrames; i++)
        {
            float curveAmount = i * (angle / (nFrames - 1));
            Quaternion rot = Quaternion.AngleAxis(curveAmount, frames[i].tvec);
            frames[i].rvec = rot * frames[i].rvec;
            frames[i].svec = Vector3.Cross(frames[i].rvec, frames[i].tvec).normalized;
        }
    }

    /// <summary>
    /// Legalise normal, given tangent.
    /// </summary>
    /// <param name="normal">Normal vector.</param>
    /// <param name="tangent">Tangent vector.</param>
    private Vector3 LegaliseNormal(Vector3 normal, Vector3 tangent)
    {
        // Check if normal is identical to tangent and change it slightly if so
        tangent = tangent.normalized;
        if (Mathf.Approximately(Vector3.Angle(normal, tangent), 0)) // normal and tangent are identical, revert to uppish if possible, otherwise add small element to normal
        {
            if (Mathf.Approximately(Vector3.Angle(tangent, Vector3.up), 0))
            { normal.z -= 0.1f; } // if tangent/normal are up, tilt normal slightly backwards
            else if (Mathf.Approximately(Vector3.Angle(tangent, Vector3.forward), 0))
            { normal.y -= 0.1f; } // if tangent/normal are forward, tilt normal slightly upwards
            else
            { normal -= Vector3.one * 0.1f; } // tilt normal in all three directions
        }
        return Vector3.Cross(tangent, Vector3.Cross(normal, tangent)).normalized;
    }

    /// <summary>
    /// Get approximate minimally rotating normal at point <paramref name="t"/>  on the curve.
    /// </summary>
    /// <param name="t">Point on curve specified between 0-1</param>
    /// <returns>Vector3 describing normal</returns>
    public Vector3 GetMinimallyRotatingNormalToPointOnCurve(float t)
    {
        // Find neighboring frames to t, and compute the average normal from those two

        // First, compute if not done so yet
        if (frames == null) { ComputeRotationMinimizingFrames(); }

        // Find indices
        List<int> index = new List<int>(2);
        index.Add(Mathf.FloorToInt(t * (nFrames - 1)));
        if (index[0] == nFrames - 1) { index.Add(nFrames - 2); index.Sort(); }
        else { index.Add(index[0] + 1); }
        // Get average normal, weighted by the distance to in t both normals
        float range = frames[index[1]].t - frames[index[0]].t;
        float weight1 = range - (t - frames[index[0]].t);
        float weight2 = range - (frames[index[1]].t - t);
        Vector3 normal1 = frames[index[0]].rvec;
        Vector3 normal2 = frames[index[1]].rvec;
        Vector3 normal = ((normal1 * weight1) + (normal2 * weight2)) / (weight1 + weight2);

        //Debug.Log(t + " - " + frames[index[0]].t + " - " + frames[index[1]].t);
        return normal;
    }

    /// <summary>
    /// Class descrbing a single Rotation Minimizing Frame.
    /// Each frame is described by:
    /// </summary>
    /// <list type="bullet">
    /// <item>
    /// <term>rvec</term>
    /// <description>Normal, orthogonal to svec and tvec</description>
    /// </item>
    /// <item>
    /// <term>tvec</term>
    /// <description>Tangent, orthogonal to rvec and svec</description>
    /// </item>
    /// <item>
    /// <term>svec</term>
    /// <description>Vector orthogonal to rvec and tvec</description>
    /// </item>
    /// <item>
    /// <term>t</term>
    /// <description>The point along the curve this frame was computed at, t ranging from 0-1.</description>
    /// </item>
    /// </list>
    public class Frame
    {
        public Vector3 rvec; // normal
        public Vector3 svec; // orthogonal to normal/tangent
        public Vector3 tvec; // tangent
        public float t; // bezier curve t 0<>1

        /// <summary>
        /// Initializes a new instance of the <see cref="Frame"/> class.
        /// </summary>
        /// <param name="rvec">Normal, orthogonal to svec and tvec</param>
        /// <param name="tvec">Tangent, orthogonal to rvec and svec</param>
        /// <param name="svec">Vector orthogonal to rvec and tvec</param>
        /// <param name="t">The point along the curve this frame was computed at, t ranging from 0-1. </param>
        public Frame(Vector3 rvec, Vector3 tvec, Vector3 svec, float t)
        {
            this.rvec = rvec;
            this.tvec = tvec;
            this.svec = svec;
            this.t = t;
        }
    }


}
