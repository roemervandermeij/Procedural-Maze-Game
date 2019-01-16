using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System;

public class MindWarpController : MonoBehaviour
{
    public static int IntensityLevel { get; set; }
    public static float TriggerProb { get; set; }
    public Vector3[] NeighborPosition { get; set; }

    private MindWarp mindWarp;


    private void SelectMindWarp()
    {
        List<System.Type> mindWarpList = new List<System.Type>(){
            typeof(MultiCamRandomView),
            typeof(MultiCamPathView)
        };
        System.Type selMindWarp = mindWarpList[Random.Range(0, mindWarpList.Count)];
        mindWarp = (System.Activator.CreateInstance(selMindWarp)) as MindWarp;
    }

    private void ApplyMindWarp()
    {
        SelectMindWarp();
        mindWarp.Activate(IntensityLevel, gameObject.transform.position, NeighborPosition);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered trigger");
        if (other.tag == "Player")
        {
            if (Random.value < TriggerProb + Mathf.Epsilon)
            { ApplyMindWarp(); }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Exited trigger");
        if (other.tag == "Player" && mindWarp != null)
        {
            mindWarp.Deactivate();
            mindWarp = null;
        }
    }

    private void OnDestroy()
    {
        if (mindWarp != null)
        {
            mindWarp.Deactivate();
            //Destroy(mindWarp);
            mindWarp = null;
        }
    }

}
