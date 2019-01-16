using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiCamRandomView : MindWarp
{
    private GameObject mainCamObj;
    private CameraController mainCamCont;
    private GameObject[] additionalCameras;
    private Transform mainCamObjTrans;
    private Transform mainCamPivotTrans;
    private Transform mainCamTrans;

    private int numberOfCameras;
    private float[] angleRange;
    private Rect[] camRect;
    private Resolution screenResolution;
    private FullScreenMode fullScreenMode;


    public MultiCamRandomView()
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
                angleRange = new float[] { 30f, 70f };
                camRect = Utilities.GetSubDividedRects(1, 3);
                break;
            case 2:
                numberOfCameras = 4;
                angleRange = new float[] { 50f, 90f };
                camRect = Utilities.GetSubDividedRects(2, 2);
                break;
            case 3:
                numberOfCameras = 6;
                angleRange = new float[] { 90f, 180f };
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

        // Create list of flips
        List<Quaternion> roll = new List<Quaternion>(numberOfCameras);
        List<Quaternion> yaw = new List<Quaternion>(numberOfCameras);
        //Vector3[] baseCamOffset = new Vector3[numberOfCameras];
        roll.Add(Quaternion.identity);
        yaw.Add(Quaternion.identity);
        for (int i = 1; i < numberOfCameras; i++)
        {
            float rollAngle = Random.Range(angleRange[0], angleRange[1]) - (Random.Range(0, 2) * 360);
            float yawAngle = Random.Range(angleRange[0], angleRange[1]) - (Random.Range(0, 2) * 360);
            roll.Add(Quaternion.AngleAxis(rollAngle, mainCamTrans.InverseTransformDirection(playerCont.ForwardDir)));
            yaw.Add(Quaternion.AngleAxis(yawAngle, mainCamObjTrans.InverseTransformDirection(playerCont.UpDir)));
        }

        // Create additional cameras
        additionalCameras = new GameObject[numberOfCameras];
        int[] randCamRectIndices = Utilities.RandomIndices(numberOfCameras);
        for (int i = 0; i < numberOfCameras; i++)
        {
            // Create new cam and set viewport
            GameObject camObj = Object.Instantiate(mainCamObj);
            Camera cam = camObj.GetComponentInChildren<Camera>();
            cam.depth = 1;
            cam.rect = camRect[randCamRectIndices[i]];

            // Activate
            CameraController camCont = camObj.GetComponent<CameraController>();
            camCont.ActivateCamera(); // FIXME there probably is a one frame lag in which the camera is already lerping due to the activate. Changing how cam activation works with resetting position and moving it down here would solve it likely

            // Make positions/rotations identical to main cam
            Transform camObjTrans = camObj.transform;
            Transform camPivotTrans = camObjTrans.GetChild(0);
            Transform camTrans = camPivotTrans.GetChild(0);
            camCont.SetBaseCamRot(mainCamCont.GetBaseCamRot());
            camCont.SetBasePivotRot(mainCamCont.GetBasePivotRot());
            camCont.SetBaseCamOffset(mainCamCont.GetBaseCamOffset());
            camObjTrans.position = mainCamObjTrans.position;
            camPivotTrans.transform.position = mainCamPivotTrans.position;
            camPivotTrans.transform.localRotation = mainCamPivotTrans.localRotation;
            camTrans.transform.position = mainCamTrans.position;

            // Apply rotations
            camCont.SetBaseCamRot(mainCamCont.GetBaseCamRot() * yaw[i]);
            camObjTrans.rotation = mainCamObjTrans.rotation * yaw[i];
            camTrans.transform.localRotation = mainCamTrans.localRotation * roll[i];

            // Save
            additionalCameras[i] = camObj;
        }

    }

    protected override void CancelEffect()
    {
        foreach (GameObject go in additionalCameras)
        { Object.Destroy(go); }
        mainCamCont.LockMouseCursor();
        // Restore resolution
        Screen.SetResolution(screenResolution.width, screenResolution.height, fullScreenMode, screenResolution.refreshRate);
    }

}
