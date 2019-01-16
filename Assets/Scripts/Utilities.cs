using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// General class containing utilities.
/// </summary>
public static class Utilities
{

    /// <summary>
    /// Resize mesh.
    /// </summary>
    /// <param name="scale">Scale vector mesh vertices will be multiplied with.</param>
    /// <param name="mesh">Mesh to be resized.</param>
    public static Mesh MeshResize(Mesh mesh, Vector3 scale)
    {
        // Resize mesh or resize gameobject by resizing mesh

        // Save old mesh ingredients and iterate over vertices to resize 
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals;
        Vector2[] uv = mesh.uv;
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            vertices[i].x *= scale.x;
            vertices[i].y *= scale.y;
            vertices[i].z *= scale.z;
        }

        // add resized mesh elements to mesh and insert
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uv;
        return mesh;
    }

    /// <summary>
    /// Resize object through mesh.
    /// </summary>
    /// <param name="scale">Scale vector mesh vertices will be multiplied with.</param>
    /// <param name="obj">GameObject to be resized.</param>
    public static void MeshResize(GameObject obj, Vector3 scale) // 
    {
        // Resize mesh or resize gameobject by resizing mesh
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        mesh = MeshResize(mesh, scale);
        obj.GetComponent<MeshFilter>().mesh = mesh;
    }


    /// <summary>
    /// Rotate mesh.
    /// </summary>
    /// <param name="rot">Quaternion mesh vertices will be rotated with.</param>
    /// <param name="mesh">Mesh to be rotated.</param>
    public static Mesh MeshRotate(Mesh mesh, Quaternion rot)
    {
        // Rotate mesh or rotate gameobject by rotating mesh (i.e. without affecting the transform)

        // Save old mesh ingredients and iterate over vertices to rotate 
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals;
        Vector2[] uv = mesh.uv;
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            vertices[i] = rot * vertices[i];
            normals[i] = rot * normals[i];
        }

        // add resized mesh elements to mesh and insert
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uv;
        return mesh;
    }

    /// <summary>
    /// Rotate object through mesh.
    /// </summary>
    /// <param name="rot">Quaternion mesh vertices will be rotated with.</param>
    /// <param name="obj">GameObject to be rotated.</param>
    public static void MeshRotate(GameObject obj, Quaternion rot)
    {
        // Rotate mesh or rotate gameobject by rotating mesh (i.e. without affecting the transform)
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        mesh = Utilities.MeshRotate(mesh, rot);
        obj.GetComponent<MeshFilter>().mesh = mesh;
    }

    /// <summary>
    /// Replicate mesh <paramref name="nTimes"/> along <paramref name="axis"/>
    /// </summary>
    /// <returns>A replicated mesh.</returns>
    /// <param name="mesh">Mesh to be used for replication.</param>
    /// <param name="nTimes">Number of times mesh will be replicated (excluding itself).</param>
    /// <param name="axis">Axis along which mesh will be replicated.</param>
    public static Mesh ReplicateAndMergeMeshes(Mesh mesh, int nTimes, Vector3 axis)
    {
        // Replicate mesh nTimes along axis and merge into one
        // Create clones of mesh and add to combine instance
        Vector3 meshOffset = Vector3.Scale(mesh.bounds.size, axis);
        CombineInstance[] combine = new CombineInstance[nTimes + 1];
        for (int i = 0; i < nTimes + 1; i++)
        {
            combine[i].mesh = Object.Instantiate(mesh);
            Vector3 translation = mesh.bounds.center + (meshOffset * i);
            Quaternion rotation = Quaternion.identity;
            Vector3 scaling = Vector3.one;
            combine[i].transform = Matrix4x4.TRS(translation, rotation, scaling);
        }
        mesh.CombineMeshes(combine, true, true);
        return mesh;
    }

    /// <summary>
    /// Replicate GameObject <paramref name="nTimes"/> along <paramref name="axis"/>
    /// </summary>
    /// <param name="obj">GameObject to be used for replication.</param>
    /// <param name="nTimes">Number of times GameObject will be replicated (excluding itself).</param>
    /// <param name="axis">Axis along which GameObject will be replicated.</param>
    public static void ReplicateAndMergeObjects(GameObject obj, int nTimes, Vector3 axis)
    {
        // Replicate gameobject nTimes along axis and merge into one

        // Save original position and rotation, and set obj to zero/identity
        Vector3 orgPos = obj.transform.position;
        Quaternion orgRot = obj.transform.rotation;
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        // Create clones of obj 
        List<GameObject> objects = new List<GameObject>(nTimes + 1) { obj };
        Vector3 objOffset = Vector3.Scale(obj.GetComponent<MeshRenderer>().bounds.size, axis);
        for (int i = 1; i <= nTimes; i++)
        {
            GameObject objClone = Object.Instantiate(obj);
            objClone.transform.position = obj.transform.position + (objOffset * i);
            objects.Add(objClone);
        }
        // Combine meshes of clones and obj and destroy clones
        CombineInstance[] combine = new CombineInstance[nTimes + 1];
        for (int i = 0; i < objects.Count; i++)
        {
            combine[i].mesh = objects[i].GetComponent<MeshFilter>().sharedMesh;
            combine[i].transform = objects[i].transform.localToWorldMatrix;
            if (i > 0) { Object.Destroy(objects[i]); }
        }
        obj.GetComponent<MeshFilter>().mesh.CombineMeshes(combine, true, true);
        // Revert to original position/rotation
        obj.transform.position = orgPos;
        obj.transform.rotation = orgRot;
    }

    /// <summary>
    /// Merges the meshes of children GameObjects of a <paramref name="parentObj"/>.
    /// </summary>
    /// <param name="parentObj">Parent object containing children with meshes.</param>
    /// <param name="useChildTranslation">If set to <c>true</c>, use child mesh translation.</param>
    /// <param name="useChildRotation">If set to <c>true</c>, use child mesh rotation.</param>
    /// <param name="useChildScaling">If set to <c>true</c>, use child mesh scaling.</param>
    public static void MergeChildrenOfParent(GameObject parentObj, bool useChildTranslation = true, bool useChildRotation = true, bool useChildScaling = true)
    {
        // Replace gameobject and its children by merging meshes into a new gameobject

        //// FIXME position merging for some reason doesn't work
        ////if (useChildTranslation)
        ////{ throw new System.Exception("Using child position for some reason doesn't work yet."); }

        // Save original position and rotation, and set obj to zero/identity
        Vector3 orgPos = parentObj.transform.position;
        Quaternion orgRot = parentObj.transform.rotation;
        parentObj.transform.position = Vector3.zero;
        parentObj.transform.rotation = Quaternion.identity;

        // Fetches meshes
        MeshFilter[] meshFilters = new MeshFilter[parentObj.transform.childCount];
        for (int i = 0; i < parentObj.transform.childCount; i++)
        { meshFilters[i] = parentObj.transform.GetChild(i).GetComponent<MeshFilter>(); }
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++) // for some reason, parentObj is included as the first meshFilter?
        {
            combine[i].mesh = meshFilters[i].mesh;
            Vector3 translation = useChildTranslation ? meshFilters[i].transform.localPosition : Vector3.zero;
            Quaternion rotation = useChildRotation ? meshFilters[i].transform.localRotation : Quaternion.identity;
            Vector3 scaling = useChildScaling ? meshFilters[i].transform.localScale : Vector3.one;
            combine[i].transform = Matrix4x4.TRS(translation, rotation, scaling);
            //combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            //meshFilters[i].gameObject.SetActive(false);
        }
        // Check whether submeshes should be merged
        bool mergeSubMeshes = true;
        //foreach (MeshFilter meshFilt1 in meshFilters)
        //{
        //    foreach (MeshFilter meshFilt2 in meshFilters)
        //    {
        //        if (meshFilt1.gameObject.GetComponent<Renderer>().sharedMaterial != meshFilt2.gameObject.GetComponent<Renderer>().sharedMaterial)
        //        { mergeSubMeshes = false; break; }
        //    }
        //    if (!mergeSubMeshes) { break; }
        //}
        // Check whether TRS should be used
        bool useTRS = true;
        //if (!useChildTranslation && !useChildRotation && !useChildScaling)
        //{ useTRS = false; }
        // Merges meshes into parent mesh
        if (parentObj.GetComponent<MeshFilter>() == null) { parentObj.AddComponent<MeshFilter>(); }
        if (parentObj.GetComponent<MeshRenderer>() == null) { parentObj.AddComponent<MeshRenderer>(); }
        parentObj.GetComponent<MeshFilter>().mesh.CombineMeshes(combine, mergeSubMeshes, useTRS);

        // Revert parent to original position/rotation
        parentObj.transform.position = orgPos;
        parentObj.transform.rotation = orgRot;

        // Destroy children
        foreach (Transform child in parentObj.transform)
        { Object.Destroy(child.gameObject); }


        //// Fetches meshes
        //MeshFilter[] meshFilters = parentObj.GetComponentsInChildren<MeshFilter>();
        //CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        //for (int i = 0; i < meshFilters.Length; i++)
        //{
        //    combine[i].mesh = meshFilters[i].mesh;
        //    Vector3 translation = useChildTranslation ? meshFilters[i].transform.position : Vector3.zero;
        //    Quaternion rotation = useChildRotation ? meshFilters[i].transform.rotation : Quaternion.identity;
        //    Vector3 scaling = useChildScaling ? meshFilters[i].transform.localScale : Vector3.one;
        //    combine[i].transform = Matrix4x4.TRS(translation, rotation, scaling);
        //    //combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        //    //meshFilters[i].gameObject.SetActive(false);
        //}

        //// Merges meshes into parent mesh
        //parentObj.GetComponent<MeshFilter>().mesh = new Mesh();
        //parentObj.GetComponent<MeshFilter>().mesh.CombineMeshes(combine, true, true);

        //// Destroy children
        //foreach (Transform child in parentObj.transform)
        //{ Object.Destroy(child.gameObject); }

    }


    /// <summary>
    /// Bends mesh along a curve (partial circle) along <paramref name="bendAngle"/> degrees along <paramref name="bendAxis"/> while wrapping around <paramref name="bendAroundAxis"/> (orthogonal to plane of bending).
    /// As such, the mesh is bended in the bendAxis-remainingAxis plane.
    /// </summary>
    /// <returns> Samples of the bending curve used, with <paramref name="curveOutSampleNum"/> samples.</returns>
    /// <param name="obj">Object whose mesh will be bended. It is assumed straight.</param>
    /// <param name="bendAngle">Angle of bending (>0 and >360), reflecting part of circle (e.g. 180 degrees is half circle)</param>
    /// <param name="bendAxis">Primary bending axis (vertex coordinates of this axis will be used as x-input to circle function).</param>
    /// <param name="bendAroundAxis">Axis orthogonal to plane of curve used for bending.</param>
    /// <param name="curveOutSampleNum">Number of samples to get from curve used for bending.</param>
    public static Vector3[] MeshBendCircle(GameObject obj, float bendAngle, int bendAxis, int bendAroundAxis, int curveOutSampleNum = 20)
    {
        // Mesh is assumed straight prior to bending.
        // 
        // After bending, the pivot point is at:
        //   -(angle<170) the interesection of the normals at the center of the bendAxis start/end of the object
        //   -(else)      the center of the starting side of the object (min(bendAxis))
        //
        // After bending, the bounds of the bendAxis are:
        // - (angle<170): such that the distance from the bendAxis start/end center to the intersection point of their 
        //                normals (see above)above is equal to half of the bendAxis bounds 
        //   -(else)      equal to the input object <180, and equal to the input object for the top 
        //                (0-180 degree part) half for >=180 (scaled proportionally for the bottom half)
        //
        //
        //
        //

        // Parse input
        if (bendAxis < 0 || bendAxis > 2 || bendAroundAxis < 0 || bendAroundAxis > 2 || bendAxis == bendAroundAxis)
        { throw new System.ArgumentException("bendAxis and bendAroundAxis cannot be identical and should be either 0, 1, 2"); }
        if (bendAngle < 0 || Mathf.Approximately(bendAngle, 0) || bendAngle > 360)
        { throw new System.ArgumentException("bendAngle should be >0 and <360"); }

        // Fetch mesh and check
        Mesh meshOrg = obj.GetComponent<MeshFilter>().mesh;
        if (Mathf.Approximately(meshOrg.bounds.size[bendAxis], 0f))
        { throw new System.ArgumentException("Mesh bounds cannot be zero for bendAxis."); }

        // Set remaining axis
        List<int> args = new List<int>(3) { 0, 1, 2 };
        args.Remove(bendAxis);
        args.Remove(bendAroundAxis);
        int remainingAxis = args[0];


        // Set offset to change pivot of mesh (based on unit circle until circleRadius is determined below)
        Vector3 offset;
        if (bendAngle < 170)
        {
            // set start pos and end pos of curve
            Vector3 curveStartPos = Vector3.zero;
            curveStartPos[remainingAxis] = Mathf.Cos(0);// * circleRadius;
            curveStartPos[bendAxis] = Mathf.Sin(0);// * circleRadius;
            Vector3 curveEndPos = Vector3.zero;
            curveEndPos[remainingAxis] = Mathf.Cos(2f * (float)System.Math.PI / (360f / bendAngle));// * circleRadius;
            curveEndPos[bendAxis] = Mathf.Sin(2f * (float)System.Math.PI / (360f / bendAngle));// * circleRadius;
                                                                                               // set start pos normal
            Vector3 startNorm = Vector3.zero;
            startNorm[bendAxis] = 1f; // normalized already
                                      // get end pos normal, as cross product of endDirToCircCent ((0,0,0)-endPos) and bendAroundAxis (i.e. away from the plane of bending)
            Vector3 axisBendAround = Vector3.zero;
            axisBendAround[bendAroundAxis] = 1f;
            Vector3 endDirToCircCent = Vector3.zero;
            endDirToCircCent[remainingAxis] = -Mathf.Cos(2f * (float)System.Math.PI / (360f / bendAngle));// * circleRadius;
            endDirToCircCent[bendAxis] = -Mathf.Sin(2f * (float)System.Math.PI / (360f / bendAngle));// * circleRadius;
            Vector3 endNorm = Vector3.Cross(endDirToCircCent.normalized, axisBendAround.normalized).normalized;
            // get intersection point
            Vector2 line1Pos = new Vector2(curveStartPos[remainingAxis], curveStartPos[bendAxis]);
            Vector2 line1Dir = new Vector2(startNorm[remainingAxis], startNorm[bendAxis]);
            Vector2 line2Pos = new Vector2(curveEndPos[remainingAxis], curveEndPos[bendAxis]);
            Vector2 line2Dir = new Vector2(endNorm[remainingAxis], endNorm[bendAxis]);
            Vector2 intersect = FindIntersectionTwoLines(line1Pos, line1Dir, line2Pos, line2Dir);
            offset = Vector3.zero;
            offset[remainingAxis] = intersect[0];
            offset[bendAxis] = intersect[1];
        }
        else
        {
            offset = Vector3.zero;
            offset[remainingAxis] = Mathf.Cos(0f);// * circleRadius;
            offset[bendAxis] = Mathf.Sin(0f);// * circleRadius;
        }

        // Calculate circle radius to determine bounds of bended object
        float circleRadius = float.NaN;
        if (bendAngle < 170)
        {
            // Calculate circle radius such that the distance from the bendAxis start/end center to the intersection point of their
            // normals (see above)above is equal to half of the bendAxis bounds (default when angle<170)
            // First, calculate distance to intersection when radius = 1
            // set start pos and end pos of curve (set again, as above is not true position)
            Vector3 curveStartPos = Vector3.zero;
            curveStartPos[remainingAxis] = Mathf.Cos(0);
            curveStartPos[bendAxis] = Mathf.Sin(0);
            Vector3 curveEndPos = Vector3.zero;
            curveEndPos[remainingAxis] = Mathf.Cos(2f * (float)System.Math.PI / (360f / bendAngle));
            curveEndPos[bendAxis] = Mathf.Sin(2f * (float)System.Math.PI / (360f / bendAngle));
            float endDist = (curveEndPos - offset).magnitude; // endDist and startDist should be identical, always
                                                              //float startDist = (curveStartPos - offset).magnitude;
            circleRadius = (meshOrg.bounds.size[bendAxis] / 2) / endDist;
        }
        else
        {
            // Calculate circle radius such that bendAxis bounds remain equal (up to 180 degree bend)
            // Do this by calculating the maximum extent of the bended object at the end of the curve
            // and set the circle radius such that this point equals the current bendAxis bounds
            Vector3 curveMaxPos = Vector3.zero;
            // use max at 90 degrees
            curveMaxPos[remainingAxis] = Mathf.Cos(0.5f * (float)System.Math.PI);
            curveMaxPos[bendAxis] = Mathf.Sin(0.5f * (float)System.Math.PI);
            circleRadius = (meshOrg.bounds.size[bendAxis] / curveMaxPos[bendAxis]);
            circleRadius = circleRadius * (1 - (meshOrg.bounds.extents[remainingAxis] / circleRadius)); // Shrink a little to account for object extent in remainingAxis
        }

        // Update offset based on new circleRadius
        offset = offset * circleRadius;

        // Create sampled curve for output
        Vector3[] curveSamples = new Vector3[curveOutSampleNum];
        for (int i = 0; i < curveOutSampleNum; i++)
        {
            float currCurvePos = 0f + ((1f / curveOutSampleNum) * (float)i);
            float theta = currCurvePos * 2f * (float)System.Math.PI / (360f / bendAngle);
            curveSamples[i] = Vector3.zero;
            curveSamples[i][remainingAxis] = Mathf.Cos(theta) * circleRadius;
            curveSamples[i][bendAxis] = Mathf.Sin(theta) * circleRadius;
            curveSamples[i] -= offset;
        }

        // Save old mesh ingredients and iterate over vertices to bend 
        Vector3[] vertices = meshOrg.vertices;
        int[] triangles = meshOrg.triangles;
        Vector3[] normals = meshOrg.normals;
        Vector2[] uv = meshOrg.uv;
        for (int i = 0; i < meshOrg.vertices.Length; i++)
        {

            // Set current vertex and get fractional position on bending curve (0-1; based solely on bendAxis value of current vertex)
            float fracCurvePos = (vertices[i][bendAxis] + Mathf.Abs(meshOrg.bounds.min[bendAxis])) / meshOrg.bounds.size[bendAxis]; // from 0 to 1

            // Compute current curve position in bendAxis/remainingAxis plane (in 3D ignoring bendAroundAxis)
            float theta = fracCurvePos * 2f * (float)System.Math.PI / (360f / bendAngle);
            Vector3 currCurvePos = Vector3.zero;
            currCurvePos[remainingAxis] = Mathf.Cos(theta) * circleRadius;
            currCurvePos[bendAxis] = Mathf.Sin(theta) * circleRadius;

            // Compute direction towards center from curve position 
            // (i.e. the new direction of a normal from an infinitesimal surface at newcurvepos)
            Vector3 newDir = Vector3.Normalize(new Vector3(0f, 0f, 0f) - currCurvePos);

            // Compute direction towards center from start of bending curve 
            // (i.e. the old direction of a normal from an infinitesimal surface at the center of the unbended object)
            Vector3 oldPos = Vector3.zero;
            oldPos[remainingAxis] = 1f; // the old direction is not dependent on the current vertex position, as the object is assumed straight
            Vector3 oldDir = Vector3.Normalize(new Vector3(0f, 0f, 0f) - oldPos);

            // Compute rotation to be aplied to current vertex from the above two directions
            Quaternion rot = Quaternion.FromToRotation(oldDir, newDir);

            // Convert vertex to direction from center, rotate by above compute rotation
            // vertex --> direction from center
            Vector3 currCenter = Vector3.zero;
            currCenter[bendAxis] = vertices[i][bendAxis];
            Vector3 vertDir = vertices[i] - currCenter;
            // rotate direction vertex
            Vector3 rotAxis = Vector3.zero;
            rotAxis[bendAroundAxis] = 1f;
            vertDir = rot * vertDir;
            // Convert back to position (vertex) using new position in bendAxis/remainingAxis plane
            Vector3 newVert = vertDir + currCurvePos;

            // rotate normals as well, displace vert to change center of object, and save both
            normals[i] = rot * normals[i];
            vertices[i] = newVert - offset;
        }
        // Finish up: add bended mesh elements to mesh and insert
        meshOrg.Clear();
        meshOrg.vertices = vertices;
        meshOrg.triangles = triangles;
        meshOrg.normals = normals;
        meshOrg.uv = uv;

        return curveSamples;
    }

    /// <summary>
    /// Find the intersecting point of two lines.
    /// </summary>
    /// <returns>The intersection of two lines in vector2.</returns>
    /// <param name="line1Pos">Position on line 1.</param>
    /// <param name="line1Dir">Line 1 direction.</param>
    /// <param name="line2Pos">Position on line 2.</param>
    /// <param name="line2Dir">Line 2 direction.</param>
    public static Vector2 FindIntersectionTwoLines(Vector2 line1Pos, Vector2 line1Dir, Vector2 line2Pos, Vector2 line2Dir)
    {
        //
        // Finds the intersection point of two lines, which are specified by a position and direction for each.
        //

        // convert to y = ax+b
        float[] ab1 = LinePosDirToEq(line1Pos, line1Dir);
        float a1 = ab1[0];
        float b1 = ab1[1];
        float[] ab2 = LinePosDirToEq(line2Pos, line2Dir);
        float a2 = ab2[0];
        float b2 = ab2[1];

        // check for parallel
        if (Mathf.Approximately(a1, a2) || (float.IsNaN(a1) && float.IsNaN(a2)))
        { throw new System.Exception("Lines are either parallel or identical, no (single) intersection exists."); }

        // find intersection
        Vector2 intersect = Vector2.zero;
        if (!float.IsNaN(a1) && !float.IsNaN(a2))
        {
            // x --> a1x+b1 = a2x+b2 ==  x = (b2-b1) / (a1-a2)
            // y --> a1x + b1 (or a2x + b2)
            intersect.x = (b2 - b1) / (a1 - a2);
            intersect.y = (a1 * intersect.x) + b1;
        }
        else if (float.IsNaN(a1))
        {
            // Line1 is vetical: 
            // x --> line1Pos.x
            // y --> a2x + b2
            intersect.x = line1Pos.x;
            intersect.y = (a2 * intersect.x) + b2;
        }
        else if (float.IsNaN(a2))
        {
            // Line1 is vetical: 
            // x --> line2Pos.x
            // y --> a1x + b1
            intersect.x = line2Pos.x;
            intersect.y = (a1 * intersect.x) + b1;
        }
        return intersect;
    }


    /// <summary>
    /// Convert line from position/direction description to a*x+b form.
    /// </summary>
    /// <returns>a and b from a*x+b form of line.</returns>
    /// <param name="linePos">Position on line.</param>
    /// <param name="lineDir">Direction of line.</param>
    public static float[] LinePosDirToEq(Vector2 linePos, Vector2 lineDir)
    {
        //
        // Convert line in form linePos/lineDir to form a*x + b
        // If line is vertical, a = NaN and b = 0.
        //

        Vector2 linePos2 = linePos + (100 * lineDir);
        float xdiff = linePos.x - linePos2.x;
        float ydiff = linePos.y - linePos2.y;
        float a;
        float b;
        if (Mathf.Approximately(xdiff, 0) && Mathf.Approximately(ydiff, 0)) { throw new System.ArgumentException("Line is a point in space."); }
        if (Mathf.Approximately(xdiff, 0)) // line is vertical
        {
            a = float.NaN;
            b = 0f;
        }
        else
        {
            a = ydiff / xdiff;
            b = linePos.y - (a * linePos.x);
        }
        float[] ab = { a, b };
        return ab;
    }



    /// <summary>
    /// Finds the closest point on a line defined by two points, of another point.
    /// Taken from wiki: http://wiki.unity3d.com/index.php/3d_Math_functions
    /// </summary>
    /// <returns>The closest point on line of position, clamped to the extrema of the line.</returns>
    /// <param name="point1">Starting point of line.</param>
    /// <param name="point2">Ending point of line.</param>
    /// <param name="position">Position whose closest point on the line is required.</param>
    public static Vector3 FindClosestPointOnLineOfPositionWithClamping(Vector3 point1, Vector3 point2, Vector3 position)
    {
        // From wiki: http://wiki.unity3d.com/index.php/3d_Math_functions
        // Find closest point of position on line drawn between point1 and point2
        Vector3 dir = Vector3.Normalize(point2 - point1);
        Vector3 Point1ToPosition = position - point1;
        float t = Vector3.Dot(Point1ToPosition, dir);
        Vector3 intersectPoint = point1 + dir * t;
        // Check whether it falls outside of line --> clamp to either end point
        Vector3 lineDir = point2 - point1;
        Vector3 intersectPointDir = intersectPoint - point1;
        if (Vector3.Dot(intersectPointDir, lineDir) > 0)
        {
            if (intersectPointDir.magnitude > lineDir.magnitude)
            {
                intersectPoint = point2; // outside, closest to point2
            }
        }
        else
        {
            intersectPoint = point1; // outside, closest to point1
        }
        return intersectPoint;
    }

    /// <summary>
    /// Finds the closest point on a line defined by two points, of another point.
    /// Taken from wiki: http://wiki.unity3d.com/index.php/3d_Math_functions
    /// </summary>
    /// <returns>The closest point on line of position, NaN if outside of extrema of line.</returns>
    /// <param name="point1">Starting point of line.</param>
    /// <param name="point2">Ending point of line.</param>
    /// <param name="position">Position whose closest point on the line is required.</param>
    public static Vector3 FindClosestPointOnLineOfPositionWithoutClamping(Vector3 point1, Vector3 point2, Vector3 position)
    {
        // From wiki: http://wiki.unity3d.com/index.php/3d_Math_functions
        // Find closest point of position on line drawn between point1 and point2
        Vector3 dir = Vector3.Normalize(point2 - point1);
        Vector3 Point1ToPosition = position - point1;
        float t = Vector3.Dot(Point1ToPosition, dir);
        Vector3 intersectPoint = point1 + dir * t;
        // Check whether it falls outside of line --> clamp to either end point
        Vector3 lineDir = point2 - point1;
        Vector3 intersectPointDir = intersectPoint - point1;
        if (Vector3.Dot(intersectPointDir, lineDir) > 0)
        {
            if (intersectPointDir.magnitude > lineDir.magnitude)
            {
                intersectPoint = intersectPoint.NaN(); // outside, closest to point2
            }
        }
        else
        {
            intersectPoint = intersectPoint.NaN(); // outside, closest to point1
        }
        return intersectPoint;
    }

    /// <summary>
    /// Find random integer in list with integer-specific probability specified by <paramref name="weightsArray"/>.
    /// </summary>
    /// <returns>Random integer from list.</returns>
    /// <param name="intArray">Array of integers to get random integer from.</param>
    /// <param name="weightsArray">Weights specifying integer-specific probability (can be bigger than 1) </param>
    public static int GetRandomIntWeightedProbability(int[] intArray, float[] weightsArray, ConsistentRandom randGen)
    {
        // Find random integer using integer-specific weights

        // check inputs
        if (intArray.Length != weightsArray.Length) { throw new System.ArgumentException("Input arrays should be of equal length."); }

        // Rescale weights
        float sum = 0;
        for (int i = 0; i < weightsArray.Length; i++) { sum = sum + weightsArray[i]; }
        for (int i = 0; i < weightsArray.Length; i++) { weightsArray[i] = weightsArray[i] / sum; }
        if (Mathf.Approximately(sum, 0)) { throw new System.ArgumentException("Sum of weights cannot be zero."); }

        // Find index using above probabilities
        float p = (float)randGen.NextDouble();
        int ind = -1;
        for (int i = 0; i < weightsArray.Length; i++)
        {
            p = p - weightsArray[i];
            if (p <= 0)
            {
                ind = i;
                break;
            }
        }
        if (ind == -1) { throw new System.Exception("Weighted probability selection failed."); }
        return ind;

        //// DEBUG
        //string str = "";
        //for (int i = 0; i < weightsArray.Length; i++) { str = str + "-" + weightsArray[i]; }
        //Debug.Log(str);
        //// DEBUG
    }


    /// <summary>
    /// Compute binomial coefficient given <paramref name="n"/> and <paramref name="k"/>.
    /// Taken from:  http://blog.plover.com/math/choose.html
    /// </summary>
    /// <returns>Binomial coefficient.</returns>
    /// <param name="n">N.</param>
    /// <param name="k">K.</param>
    public static int BinomialCoefficient(int n, int k)
    {
        // Taken from:  http://blog.plover.com/math/choose.html
        // coeff = n_factorial / (k_factorial * n-k_factorial)
        if (k < 0 || n < 0) { throw new System.ArgumentException("K and N should be bigger than 0."); }
        if (k > n || n == 0) { return 0; }
        if (n == 1) { return 1; }
        //
        int r = 1;
        int d;
        for (d = 1; d <= k; d++)
        {
            r *= n--;
            r /= d;
        }
        return r;

        //// n factorial
        //uint nFac = 1; uint tmp = n; while (tmp != 0) { nFac = nFac * tmp; tmp = tmp - 1; } // factorial
        //// k factorial
        //uint kFac = 2; // k is always 2, and has factorial 2
        //// n-k factorial 
        //uint nMkFac = 1; tmp = n - k; while (tmp != 0) { nMkFac = nMkFac * tmp; tmp = tmp - 1; }
        //uint coeff = nFac / (kFac * nMkFac);

        //return coeff;
    }

    /// <summary>
    /// Generates array of <paramref name="nIndices"/> random indices between 0
    /// and <paramref name="nIndices"/>-1.
    /// </summary>
    /// <returns>The indices.</returns>
    /// <param name="nIndices">N indices.</param>
    public static int[] RandomIndices(int nIndices)
    {
        int[] randomIndices = new int[nIndices];
        List<int> indices = new List<int>(nIndices);
        for (int i = 0; i < nIndices; i++) { indices.Insert(i, i); }
        for (int i = 0; i < nIndices; i++)
        {
            int randInd = indices[Random.Range(0, indices.Count)];
            randomIndices[i] = randInd;
            indices.Remove(randInd);
        }
        return randomIndices;
    }

    /// <summary>
    /// Gets sub divided rect objects.
    /// </summary>
    /// <returns>The sub divided rects.</returns>
    /// <param name="nRows">N rows.</param>
    /// <param name="nColumns">N columns.</param>
    public static Rect[] GetSubDividedRects(int nRows, int nColumns)
    {
        int nRects = nRows * nColumns;
        Rect[] rects = new Rect[nRects];
        float xOffset = 1f / nColumns;
        float yOffset = 1f / nRows;
        float width = 1f / nColumns;
        float height = 1f / nRows;
        for (int i = 0; i < nRects; i++)
        {
            rects[i] = new Rect(xOffset * (i % nColumns), yOffset * (i / nColumns), width, height);
        }
        return rects;
    }

}

