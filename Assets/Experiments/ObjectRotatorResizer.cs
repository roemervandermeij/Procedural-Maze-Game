using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
public class ObjectRotatorResizer : MonoBehaviour
{
    string objName = "PathObjectLightBar";
    bool on = false;
    void Start()
    {
        if (!on) return;
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        //Utilities.MeshRotate(mesh, Quaternion.Euler(0, 0, 0));
        Utilities.MeshResize(mesh, new Vector3(.1f, .1f, .4f));


        AssetDatabase.CreateAsset(gameObject.GetComponent<MeshFilter>().mesh, "Assets/Resources/Prefabs/" + objName + "-mesh" + ".asset");
        PrefabUtility.CreatePrefab("Assets/Resources/Prefabs/" + objName + ".prefab", gameObject);
    }

}
#endif
