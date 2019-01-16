using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
public class ObjectExpander : MonoBehaviour
{
    string objName = "MazeBar";
    bool on = false;
    void Start()
    {
        if (!on) return;
        GameObject prefabCyl = Resources.Load("Prefabs/" + objName) as GameObject;
        for (int i = 0; i <= 49; i++)
        {
            int rep = i;
            GameObject obj = Instantiate(prefabCyl) as GameObject;
            obj.name = prefabCyl.name + "-20-" + (rep + 1);
            Utilities.ReplicateAndMergeObjects(obj, rep, Vector3.forward);
            AssetDatabase.CreateAsset(obj.GetComponent<MeshFilter>().mesh, "Assets/Resources/Prefabs/" + objName + "-20-X/" + obj.name + "-mesh" + ".asset");
            PrefabUtility.CreatePrefab("Assets/Resources/Prefabs/" + objName + "-20-X/" + obj.name + ".prefab", obj);
        }
    }

}
#endif
