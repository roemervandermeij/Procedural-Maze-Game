using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class implementing fitting mesh to spline. A mesh is taken, fitted, and put into the 
/// GameObject this script is attached to.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class SplineFittedMesh : MonoBehaviour
{
    /// <summary>
    /// Whether fitting coroutine has finished.
    /// </summary>
    /// <value><c>true</c> if fitting coroutine finished; otherwise, <c>false</c>.</value>
    public bool FitFinished { get; private set; }
    /// <summary>
    /// The spline to which the mesh will be fitted
    /// </summary>
    /// <value>Spline.</value>
    public Spline Spline { get; private set; }
    /// <summary>
    /// Number of the given meshes to use per segment.
    /// </summary>
    /// <value>The number of meshes per segment.</value>
    public int NMeshesPerSegment { get; private set; } = -1;

    private Mesh[] mesh;
    private Vector3 splineAxis = Vector3.forward;
    private bool hasSpline = false;
    private bool hasMeshBase = false;
    private bool[] segmentFitFinished;

    /// <summary>
    /// Add mesh to be fitted to spline.
    /// </summary>
    /// <param name="mesh">Mesh.</param>
    public void SetMeshBase(Mesh mesh)
    {
        this.mesh = new Mesh[] { mesh };
        hasMeshBase = true;
        if (hasSpline) { FitMeshToSpline(); }
    }

    /// <summary>
    /// Add meshes to be fitted to spline. 
    /// <see cref="NMeshesPerSegment"/> will be set accordingly.
    /// </summary>
    /// <param name="meshes">Meshes.</param>
    public void SetMeshBase(Mesh[] meshes)
    {
        this.mesh = meshes;
        hasMeshBase = true;
        if (NMeshesPerSegment == -1)
        { NMeshesPerSegment = meshes.Length; }
        if (hasSpline) { FitMeshToSpline(); }
    }

    /// <summary>
    /// Sets the spline the mesh will be fitted to.
    /// </summary>
    /// <param name="spline">Spline.</param>
    public void SetSpline(Spline spline)
    {
        this.Spline = spline;
        hasSpline = true;
        if (hasMeshBase) { FitMeshToSpline(); }
    }

    /// <summary>
    /// Sets the spline axis of the mesh, along which it will be fitted.
    /// Supports non-integer axes.
    /// </summary>
    /// <param name="splineAxis">Vector3 describing spline axis.</param>
    public void SetSplineAxis(Vector3 splineAxis)
    {
        this.splineAxis = splineAxis.normalized;
        if (hasMeshBase && hasSpline) { FitMeshToSpline(); }
    }

    /// <summary>
    /// Sets the number of meshes to use per segment.
    /// </summary>
    /// <param name="nMeshes">Number of meshes to use per segment.</param>
    public void SetNMeshesPerSegment(int nMeshes)
    {
        if (this.mesh != null && this.mesh.Length != 1 && this.mesh.Length != nMeshes)
        { throw new System.ArgumentException("If multiple meshes are given, nMeshes needs to be equal to the number of meshes."); }
        //if (this.mesh != null && this.mesh.Length != 1 && (this.mesh.Length % nMeshes) == 0)
        //{ throw new System.ArgumentException("If multiple meshes are given, nMeshes needs to be a multiple ofthe number of meshes."); }
        this.NMeshesPerSegment = nMeshes;
        if (hasMeshBase && hasSpline) { FitMeshToSpline(); }
    }

    /// <summary>
    /// Fits the mesh to spline. If a spline axis was specified that is not equal to Vector3.forward,
    /// the mesh will be rotated to face forward prior to fitting.
    /// </summary>
    public void FitMeshToSpline()
    {
        FitFinished = false;
        segmentFitFinished = new bool[Spline.NCurveSegments * NMeshesPerSegment];

        // Rotate mesh/splineAxis if not forward, as we set look rotation from perspective of forward below
        if (!splineAxis.ComponentsAreApproxEqualTo(Vector3.forward))
        {
            // FIXME expensive and unnecessary if I make an accurate prediction of the bounds of the rotated mesh.
            Quaternion axisRot = Quaternion.FromToRotation(splineAxis, Vector3.forward);
            for (int i = 0; i < mesh.Length; i++)
            { mesh[i] = Utilities.MeshRotate(mesh[i], axisRot); }
            splineAxis = Vector3.forward;
            Debug.Log("Expensively rotating mesh...");
        }

        // Start mesh fitting coroutines
        List<Mesh> meshes = new List<Mesh>(Spline.NCurveSegments);
        for (int iSegment = 0; iSegment < Spline.NCurveSegments; iSegment++)
        {
            for (int iMesh = 0; iMesh < NMeshesPerSegment; iMesh++)
            {
                Mesh currMesh;
                if (mesh.Length == 1)
                { currMesh = Instantiate(mesh[0]); }
                else
                { currMesh = Instantiate(mesh[iMesh]); }
                meshes.Add(currMesh);
                StartCoroutine(MeshFittingCoroutine(currMesh, iSegment, iMesh));
            }
        }
        // Start waiting for merge coroutine
        StartCoroutine(WaitToMergeMeshesCoroutine(meshes));
    }


    /// <summary>
    /// Mesh fitting coroutine.
    /// </summary>
    private IEnumerator MeshFittingCoroutine(Mesh currMesh, int curveSegmentInd, int meshInd)
    {
        // Compute some ingredients
        Vector3 absMinInSplineAxis = Vector3.Scale(currMesh.bounds.min, splineAxis).ComponentAbs();
        Vector3 sizeInSplineAxis = Vector3.Scale(currMesh.bounds.size, splineAxis);

        // Save currMesh elements and adjust each vertex and normal 
        Vector3[] vertices = currMesh.vertices;
        Vector3[] normals = currMesh.normals;
        int[] triangles = currMesh.triangles;
        Vector2[] uv = currMesh.uv;
        for (int i = 0; i < currMesh.vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            Vector3 n = normals[i];

            // get normalized t
            Vector3 vInSplineAxis = Vector3.Scale(v, splineAxis);
            float t = Vector3.Scale(vInSplineAxis + absMinInSplineAxis, sizeInSplineAxis.ComponentInverse()).ComponentSum();
            if (NMeshesPerSegment != 1)
            { t = (t / NMeshesPerSegment) + (meshInd * 1f / NMeshesPerSegment); }

            // get rotation as looking from current tangent with minimally rotating normal as 'up' direction
            float splineWiseT = Spline.GetTFromSectionIndAndCorrectedT(t, curveSegmentInd);
            Quaternion rot = Quaternion.LookRotation(Spline.CurveSegments[curveSegmentInd].curve.GetTangentToPointOnCurve(t).normalized, Spline.DefaultGetNormalAtT(splineWiseT));

            // apply rotation and new position
            v = rot * (v - Vector3.Scale(v, splineAxis)) + Spline.CurveSegments[curveSegmentInd].curve.GetPointOnCurve(t);
            n = rot * n;

            // remove object world position and rotation --> take the spline as world coordinates
            v = v - gameObject.transform.position;
            Quaternion rotInv = Quaternion.Inverse(gameObject.transform.rotation);
            v = rotInv * v;
            n = rotInv * n;

            // save out!
            vertices[i] = v;
            normals[i] = n;

            if ((i % 1000) == 0)
            { yield return null; }
        }
        // Add adjusted elements to mesh 
        currMesh.Clear();
        currMesh.vertices = vertices;
        currMesh.triangles = triangles;
        currMesh.normals = normals;
        currMesh.uv = uv;
        // Set current mesh to finished
        int ind = curveSegmentInd * NMeshesPerSegment + meshInd;
        segmentFitFinished[ind] = true;
    }

    /// <summary>
    /// Coroutine waiting for the meshes to merge.
    /// </summary>
    /// <returns>The merging coroutine.</returns>
    /// <param name="meshes">Meshes.</param>
    private IEnumerator WaitToMergeMeshesCoroutine(List<Mesh> meshes)
    {
        bool done = false;
        while (!done)
        {
            done = true;
            foreach (bool flg in segmentFitFinished)
            { done = done && flg; }
            yield return null;
        }
        // Combine meshes into one mesh of only one was given, or submeshes per given mesh
        Mesh finalMesh = new Mesh();
        if (mesh.Length == 1)
        {
            CombineInstance[] combine = new CombineInstance[meshes.Count];
            for (int i = 0; i < meshes.Count; i++)
            {
                combine[i].mesh = meshes[i];
            }
            finalMesh.CombineMeshes(combine, true, false);
        }
        else
        {
            // First combine individual meshes into 'sub' meshes
            Mesh[] subMeshes = new Mesh[NMeshesPerSegment];
            for (int iSubMesh = 0; iSubMesh < NMeshesPerSegment; iSubMesh++)
            {
                subMeshes[iSubMesh] = new Mesh();
                CombineInstance[] subCombine = new CombineInstance[Spline.NCurveSegments];
                for (int iSeg = 0; iSeg < Spline.NCurveSegments; iSeg++)
                {
                    int ind = iSeg * NMeshesPerSegment + iSubMesh;
                    subCombine[iSeg].mesh = meshes[ind];
                }
                subMeshes[iSubMesh].CombineMeshes(subCombine, true, false);
            }
            CombineInstance[] combine = new CombineInstance[NMeshesPerSegment];
            for (int iSubMesh = 0; iSubMesh < NMeshesPerSegment; iSubMesh++)
            {
                combine[iSubMesh].mesh = subMeshes[iSubMesh];
            }
            finalMesh.CombineMeshes(combine, false, false);
        }
        // Add mesh to game object and call it a day
        gameObject.GetComponent<MeshFilter>().mesh = finalMesh;
        FitFinished = true;
    }


    ///// <summary>
    ///// Fits the mesh to spline. If a spline axis was specified that is not equal to Vector3.forward,
    ///// the mesh will be rotated to face forward prior to fitting.
    ///// </summary>
    //public void FitMeshToSpline()
    //{
    //    fitFinished = false;
    //    // Expand mesh
    //    //Mesh mesh = Utilities.ReplicateAndMergeMeshes(meshBase, Mathf.RoundToInt(spline.NControlPoints * MeshesPerControlPointPair), splineAxis);

    //    // Rotate mesh/splineAxis if not forward, as we set look rotation from perspective of forward below
    //    //Mesh mesh = mesh;
    //    if (!splineAxis.ComponentsAreApproxEqualTo(Vector3.forward))
    //    {
    //        // FIXME expensive and probably unnecessary if I figure out how to predict the bounds of the rotated mesh properly.
    //        Quaternion axisRot = Quaternion.FromToRotation(splineAxis, Vector3.forward);
    //        mesh = Utilities.MeshRotate(mesh, axisRot);
    //        splineAxis = Vector3.forward;
    //        Debug.Log("Expensively rotating mesh...");
    //    }

    //    // Start mesh fitting coroutine
    //    StartCoroutine(MeshFittingCoroutine());
    //}

    ///// <summary>
    ///// Mesh fitting coroutine.
    ///// </summary>
    //private IEnumerator MeshFittingCoroutine()
    //{
    //    // Compute some ingredients
    //    Vector3 absMinInSplineAxis = Vector3.Scale(mesh.bounds.min, splineAxis).ComponentAbs();
    //    Vector3 sizeInSplineAxis = Vector3.Scale(mesh.bounds.size, splineAxis);

    //    // Save mesh elements and adjust each vertex and normal 
    //    Vector3[] vertices = mesh.vertices;
    //    Vector3[] normals = mesh.normals;
    //    int[] triangles = mesh.triangles;
    //    Vector2[] uv = mesh.uv;
    //    for (int i = 0; i < mesh.vertices.Length; i++)
    //    {
    //        Vector3 v = vertices[i];
    //        Vector3 n = normals[i];

    //        // get normalized t
    //        Vector3 vInSplineAxis = Vector3.Scale(v, splineAxis);
    //        float t = Vector3.Scale(vInSplineAxis + absMinInSplineAxis, sizeInSplineAxis.ComponentInverse()).ComponentSum();

    //        // get rotation as looking from current tangent with minimally rotating normal as 'up' direction
    //        Quaternion rot = Quaternion.LookRotation(Spline.GetTangentToPointOnSpline(t).normalized, Spline.DefaultGetNormalAtT(t));

    //        // apply rotation and new position
    //        v = rot * (v - Vector3.Scale(v, splineAxis)) + Spline.GetPointOnSpline(t);
    //        n = rot * n;

    //        // remove object world position and rotation --> take the spline as world coordinates
    //        v = v - gameObject.transform.position;
    //        Quaternion rotInv = Quaternion.Inverse(gameObject.transform.rotation);
    //        v = rotInv * v;
    //        n = rotInv * n;

    //        // save out!
    //        //Vector3 noise = new Vector3((Random.value - 0.5f) * 2, (Random.value - 0.5f) * 2, (Random.value - 0.5f) * 2);
    //        //noise = noise * 0.01f;
    //        //v = v + noise;
    //        vertices[i] = v;
    //        normals[i] = n;

    //        if ((i % 1000) == 0)
    //        { yield return null; }
    //    }
    //    // Add adjusted elements to mesh and add mesh to meshfilter
    //    mesh.Clear();
    //    mesh.vertices = vertices;
    //    mesh.triangles = triangles;
    //    mesh.normals = normals;
    //    mesh.uv = uv;
    //    gameObject.GetComponent<MeshFilter>().mesh = mesh;
    //    fitFinished = true;
    //}
}
