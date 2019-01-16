using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class representing N-Degree Bezier curve.
/// Contains methods to compute point, tangent, normal, and 2nd derivative at any point on the curve.
/// For curve specification, see https://en.wikipedia.org/wiki/B%C3%A9zier_curve.
/// </summary>
public class NDegreeBezierCurve
{

    private readonly Vector3[] p;
    private readonly int n;
    private readonly RotationMinimizingFrames rotationMinimizingFrames;

    /// <summary>
    /// Initializes a new instance of the <see cref="NDegreeBezierCurve"/> class.
    /// </summary>
    /// <param name="p">Control points of Bezier curve, of any length (typically 4). Note: very high degree can become unstable..</param>
    public NDegreeBezierCurve(Vector3[] p, int nSamplesPerControlPoint = 20)
    {
        this.p = p;
        this.n = p.Length - 1; // p0,p1,p2,pN
        this.rotationMinimizingFrames = new RotationMinimizingFrames(nSamplesPerControlPoint * n, this.GetPointOnCurve, this.GetTangentToPointOnCurve);
    }

    /// <summary>
    /// Get point on curve at <paramref name="t"/>.
    /// </summary>
    /// <returns>Point on curve.</returns>
    /// <param name="t">Relative point on curve ranging from 0-1, also known as 'distance'.</param>
    public Vector3 GetPointOnCurve(float t)
    {
        // Get point at t on N-degree Bezier curve
        t = Mathf.Clamp01(t);
        Vector3 pointOnCurve = Vector3.zero;
        for (int i = 0; i <= n; i++)
        {
            pointOnCurve += Utilities.BinomialCoefficient(n, i) * Mathf.Pow((1 - t), n - i) * Mathf.Pow(t, i) * p[i];
        }
        return pointOnCurve;
    }

    /// <summary>
    /// Get tangent to point on curve at <paramref name="t"/> (i.e. the 1st derivative).
    /// </summary>
    /// <returns>Tangent to point on curve.</returns>
    /// <param name="t">Relative point on curve ranging from 0-1, also known as 'distance'.</param>
    public Vector3 GetTangentToPointOnCurve(float t)
    {
        if (n > 2)
        {
            // Get tangent to point t on N-degree Bezier curve
            t = Mathf.Clamp01(t);
            Vector3 deriv1 = Vector3.zero;
            for (int i = 0; i <= (n - 1); i++)
            {
                deriv1 += Utilities.BinomialCoefficient(n - 1, i) * Mathf.Pow(1 - t, n - 1 - i) * Mathf.Pow(t, i) * n * (p[i + 1] - p[i]);
            }
            return deriv1;
        }
        else
        {
            // Get "tangent" as the difference between any two points on the curve (the curve is straight, so any two will do)
            Vector3 fakeTangent = (GetPointOnCurve(.2f) - GetPointOnCurve(.1f)).normalized;
            return fakeTangent;
        }
    }

    /// <summary>
    /// Get normal to curve at point <paramref name="t"/>, computed using offset method.
    /// </summary>
    /// <returns>Normal vector to point on curve.</returns>
    /// <param name="t">Relative point on curve ranging from 0-1, also known as 'distance'.</param>
    public Vector3 GetNormalToPointOnCurveUsingOffset(float t)
    {
        // Get (normalized) normal to point t on N-degree Bezier curve, by rotating tangent 90 in plane defined by tangent/next-tangent
        t = Mathf.Clamp01(t);

        // Get the normalized tanget at t
        Vector3 tangent = GetTangentToPointOnCurve(t).normalized;
        // Get second tangent at tiny offset of t
        float offset = 0.0001f;
        if ((t + offset) > 1 || Mathf.Approximately(t, 1)) { offset = -offset; } // it'll clamp to 1 with enough offset for the second tangent
        Vector3 tangent2 = GetTangentToPointOnCurve(t + offset);
        // Rotation axis is perpendicular to both tangents
        Vector3 rotAxis = (t + offset) > 1 || Mathf.Approximately(t, 1)
            ? Vector3.Cross(tangent, tangent2).normalized
            : Vector3.Cross(tangent2, tangent).normalized;
        // Normal is perpendicular to both tangent and rotation axis
        Vector3 normal = Vector3.Cross(rotAxis, tangent).normalized;
        return normal.normalized;
    }

    /// <summary>
    /// Get normal to curve at point <paramref name="t"/>, computed using second derivative.
    /// </summary>
    /// <returns>Normal vector to point on curve.</returns>
    /// <param name="t">Relative point on curve ranging from 0-1, also known as 'distance'.</param>
    public Vector3 GetNormalToPointOnCurve(float t)
    {
        // Get (normalized) normal to point t on N-degree Bezier curve, by rotating tangent 90 in plane defined by tangent/2nd-deriv
        t = Mathf.Clamp01(t);

        //// Get the normalized tanget at t
        Vector3 tangent = GetTangentToPointOnCurve(t).normalized;
        // Get the normalized second derivative at t, which forms a plane with the tangent
        Vector3 deriv2 = Get2ndDerivativeToPointOnCurve(t).normalized;
        // Get rotation axis perpendicular to both tangent and rotation axis
        Vector3 rotAxis = Vector3.Cross(deriv2, tangent).normalized;
        // Get the normal as perpendicular to rotation axis and tangent
        //Vector3 normal = Vector3.Cross(rotAxis, tangent).normalized;
        Vector3 normal = Quaternion.AngleAxis(90f, rotAxis) * tangent;
        return normal.normalized;
    }

    /// <summary>
    /// Get 2nd derivative to point on curve.
    /// </summary>
    /// <returns>The 2nd derivative to point on curve.</returns>
    /// <param name="t">Relative point on curve ranging from 0-1, also known as 'distance'.</param>
    public Vector3 Get2ndDerivativeToPointOnCurve(float t)
    {
        t = Mathf.Clamp01(t);

        Vector3 deriv2 = Vector3.zero;
        for (int i = 0; i <= (n - 2); i++)
        {
            deriv2 += Utilities.BinomialCoefficient(n - 2, i) * Mathf.Pow(1 - t, n - 2 - i) * Mathf.Pow(t, i) * n * (n - 1) * (p[i + 2] - (2 * p[i + 1]) + p[i]);
        }
        return deriv2;
    }

    /// <summary>
    /// Get minimally rotating normal to point on curve, with <paramref name="t"/> (0-1) reflecting equally sampled 
    /// distance along the curve. 
    /// </summary>
    /// <returns>Minimally rotating normal to point on curve.</returns>
    /// <param name="t">T.</param>
    public Vector3 GetMinimallyRotatingNormalToPointOnCurve(float t)
    {
        t = Mathf.Clamp01(t);
        // First, compute frames if not yet done so
        if (rotationMinimizingFrames.Frames == null) { ComputeRotationMinimizingFrames(); }
        // Then, get normal
        return rotationMinimizingFrames.GetMinimallyRotatingNormalToPointOnCurve(t);
    }

    /// <summary>
    /// Computes the Rotation Minimizing Frames based on current curve segments.
    /// </summary>
    public void ComputeRotationMinimizingFrames()
    {
        rotationMinimizingFrames.ComputeRotationMinimizingFrames();
    }
}

