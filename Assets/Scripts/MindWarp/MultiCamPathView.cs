using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiCamPathView : MindWarp
{
    private GameObject mainCamObj;
    private CameraController mainCamCont;
    private GameObject[] additionalCameras;
    private Transform mainCamObjTrans;
    private Transform mainCamPivotTrans;
    private Transform mainCamTrans;

    private int numberOfCameras;
    private Rect[] camRect;
    private Resolution screenResolution;
    private FullScreenMode fullScreenMode;


    public MultiCamPathView()
    {
        mainCamObj = GameObject.FindWithTag("MainCamera");
        mainCamCont = mainCamObj.GetComponent<CameraController>();
        mainCamObjTrans = mainCamObj.transform;
        mainCamPivotTrans = mainCamObjTrans.GetChild(0);
        mainCamTrans = mainCamPivotTrans.GetChild(0);
        screenResolution = Screen.currentResolution;
        fullScreenMode = Screen.fullScreenMode;
    }

    protected override void UseIntensity()
    {
        switch (intensity)
        {
            case 1:
                numberOfCameras = 3;
                camRect = Utilities.GetSubDividedRects(1, 3);
                break;
            case 2:
                numberOfCameras = 4;
                camRect = Utilities.GetSubDividedRects(2, 2);
                break;
            case 3:
                numberOfCameras = 6;
                camRect = Utilities.GetSubDividedRects(2, 3);
                break;
            default:
                throw new System.ArgumentException("Intensity level not implemented.");
        }
    }

    protected override void ApplyEffect()
    {
        // Downscale resolution to closest one applicable FIXME render textures are supposedly better for this
        Resolution[] supportedResolutions = Screen.resolutions;
        int currDiff = int.MaxValue;
        int resInd = -1;
        for (int i = 0; i < supportedResolutions.Length; i++)
        {
            Resolution res = supportedResolutions[i];
            int heighDiff = Mathf.Abs(res.height - (screenResolution.height / numberOfCameras));
            if (heighDiff < currDiff) { currDiff = heighDiff; resInd = i; }
            int widthDiff = Mathf.Abs(res.width - (screenResolution.width / numberOfCameras));
            if (widthDiff < currDiff) { currDiff = widthDiff; resInd = i; }
        }
        Screen.SetResolution(supportedResolutions[resInd].width, supportedResolutions[resInd].height, fullScreenMode, screenResolution.refreshRate);

        // First get all directions from junction to neighbor
        List<Vector3> neighborToCenterDir = new List<Vector3>(neighborPosition.Length);
        foreach (Vector3 neighPos in neighborPosition)
        { neighborToCenterDir.Add((centerPosition - neighPos).normalized); }
        // Then, remove the one closest to that of the player
        Vector3 playerToCenterDir = (centerPosition - playerRB.position).normalized;
        int ind = 0;
        for (int i = 0; i < neighborToCenterDir.Count; i++)
        {
            if (Vector3.Angle(neighborToCenterDir[i], playerToCenterDir) < Vector3.Angle(neighborToCenterDir[ind], playerToCenterDir))
            { ind = i; }
        }
        Vector3 approxPlayerToCenterDir = neighborToCenterDir[ind];
        // Ensure approximate player direction is used at least once
        neighborToCenterDir.RemoveAt(ind);
        neighborToCenterDir.RandomizeOrder();
        neighborToCenterDir.Insert(0, approxPlayerToCenterDir);

        // Then, create list of rotations/positions 
        List<Vector3> camPosition = new List<Vector3>(numberOfCameras);
        List<Quaternion> camRotation = new List<Quaternion>(numberOfCameras);
        int neighborInd = 0;
        int[] randNeighborInd = Utilities.RandomIndices(neighborPosition.Length);
        Vector3 playerBounds = playerRB.GetComponent<MeshRenderer>().bounds.size;
        for (int i = 0; i < numberOfCameras; i++)
        {
            if (neighborInd > neighborToCenterDir.Count - 1) { neighborInd = 0; neighborToCenterDir.RandomizeOrder(); }
            camPosition.Add(centerPosition - neighborToCenterDir[neighborInd] * 3 + 1 * playerCont.UpDir * playerBounds.ComponentMax());
            camRotation.Add(Quaternion.LookRotation(neighborToCenterDir[neighborInd], playerCont.UpDir));
            neighborInd++;
        }


        // Set placing
        float xOffset = 1f / numberOfCameras;
        float width = 1f / numberOfCameras; ;

        // Create additional cameras
        additionalCameras = new GameObject[numberOfCameras];
        int[] randCamRectIndices = Utilities.RandomIndices(numberOfCameras);
        for (int i = 0; i < numberOfCameras; i++)
        {
            // Create new cam and set viewport
            GameObject camObj = new GameObject();
            Camera cam = camObj.AddComponent<Camera>();
            cam.depth = 1;
            cam.rect = camRect[randCamRectIndices[i]];
            camObj.transform.position = camPosition[i];
            camObj.transform.rotation = camRotation[i];
            //
            // Save
            additionalCameras[i] = camObj;
        }
    }

    protected override void CancelEffect()
    {
        foreach (GameObject go in additionalCameras)
        { Object.Destroy(go); }
        //mainCamCont.LockMouseCursor();
        // Restore resolution
        Screen.SetResolution(screenResolution.width, screenResolution.height, fullScreenMode, screenResolution.refreshRate);
    }

}
