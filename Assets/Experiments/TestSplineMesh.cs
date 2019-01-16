using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSplineMesh : MonoBehaviour
{
    GameObject obj;
    public int count = 0;

    private void Start()
    {
    }

    void Update()
    {

        List<Vector3> pList = new List<Vector3>();
        for (int i = 0; i < GetComponent<Transform>().childCount; i++)
        {
            if (GetComponent<Transform>().GetChild(i).gameObject.activeSelf)
            {
                pList.Add(GetComponent<Transform>().GetChild(i).position);
            }
        }
        Vector3[] pArray = pList.ToArray();

        Spline spline = new Spline(pArray);

        spline.SetRMFStartNormal(Vector3.up);
        spline.SetRMFEndNormal(Vector3.up);
        spline.ComputeRotationMinimizingFrames();

        int nSample = 100;
        Vector3 pOut;
        Vector3 prevpOut = pArray[0];
        for (int i = 0; i < nSample + 1; i++)
        {
            float t = ((1f / nSample) * i);
            pOut = spline.GetPointOnSpline(t);
            Debug.DrawLine(prevpOut, pOut, Color.yellow);
            prevpOut = pOut;
            Vector3 tOut = spline.GetTangentToPointOnSpline(t).normalized;
            Debug.DrawRay(pOut, tOut, Color.cyan);
            Vector3 nOut = spline.DefaultGetNormalAtT(t);
            Debug.DrawRay(pOut, nOut * 2, Color.red);
        }
        //Debug.DrawLine(spline.GetPointOnSpline(0.84f), Vector3.zero, Color.red);
        //Debug.DrawLine(spline.GetPointOnSpline(0.8f), Vector3.zero, Color.red);

        count++;
        if (count < 2)
        {
            string meshName = "mesh-" + gameObject.name;
            //GameObject prefabCyl = Resources.Load("Prefabs/" + "test/testz") as GameObject;
            GameObject prefabCyl = Resources.Load("Prefabs/MazeBar-20-X/MazeBar-20-1") as GameObject;
            if (GameObject.Find(meshName) != null) { Destroy(GameObject.Find(meshName)); }
            obj = Instantiate(prefabCyl, GameObject.Find("holderobj").transform) as GameObject;
            obj.name = meshName;

            Utilities.MeshResize(obj, new Vector3(2, .2f, 1));
            //Utilities.MeshReplicate(obj, 1, Vector3.forward);
            //obj.transform.rotation = Quaternion.Euler(0, 90, 0);
            obj.AddComponent(typeof(SplineFittedMesh));
            SplineFittedMesh sfMesh = obj.GetComponent<SplineFittedMesh>();
            //sfMesh.SetSplineAxis(new Vector3(0, 0, 1f));
            sfMesh.SetSplineAxis(new Vector3(0, 0, 1));
            sfMesh.SetSpline(spline);
            Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
            sfMesh.SetMeshBase(mesh);



        }
    }






}
