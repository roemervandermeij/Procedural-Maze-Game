using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float updateSpeed = 0.5f;
    public float zoomSpeed = 10f;
    public float freeLookSensitivity = 5f;
    public float zoomWait = 1.5f;
    public bool freeLook;

    private GameObject player;
    private PlayerController playerCont;
    private Transform camTransform;
    private Transform pivotTransform;
    private Rigidbody playerRB;
    private Transform endPortalT;

    private Vector3 baseCamOffset;
    private Quaternion baseCamRot;
    private Quaternion basePivotRot;

    private bool lerpToPosAndRot;
    private Coroutine camLerp;
    private enum LerpFunction { linear, exponentialEase, sinEase, smoothStep, smootherStep };
    private float timeWithoutInput;
    private bool zooming;
    private bool cameraActive;


    private Vector3 currCamVelocity;


    private void OnEnable()
    {
        player = GameObject.Find("Player");
        playerCont = player.GetComponent<PlayerController>();
        playerRB = player.GetComponent<Rigidbody>();
        camTransform = gameObject.GetComponent<Transform>();
        pivotTransform = camTransform.GetChild(0);

        // Initializd base camera rotation and position, which will be used when updated.
        baseCamOffset = playerRB.position - camTransform.position;
        baseCamRot = camTransform.rotation;
        basePivotRot = pivotTransform.localRotation;
    }


    private void OnDisable()
    {
        UnlockMouseCursor();
    }

    public void LockMouseCursor()
    {
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockMouseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ActivateCamera()
    {
        cameraActive = true;
        // Reset camera/pivot transforms
        camTransform.position = playerRB.position - baseCamOffset;
        camTransform.rotation = baseCamRot;
        pivotTransform.rotation = basePivotRot;
        LockMouseCursor();
    }

    public void DeactivateCamera()
    {
        cameraActive = false;
        UnlockMouseCursor();
    }


    private void Update()
    {
        if (endPortalT == null)
        {
            if (GameObject.Find("EndPortal") != null)
            { endPortalT = GameObject.Find("EndPortal").GetComponent<Transform>(); }
        }
        // DEBUG
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (Cursor.visible)
            { LockMouseCursor(); }
            else if (!Cursor.visible)
            { UnlockMouseCursor(); }
        }
        // DEBUG

    }

    private void LateUpdate()
    {
        if (!cameraActive) return;
        ZoomToEndPortal();
        UpdateCamPosandRotBasedOnPlayer();
        FreeLook();
    }

    public void ZoomToEndPortal()
    {
        if (freeLook) return;
        // See if we need to zoom
        if (Mathf.Approximately(Mathf.Abs(Input.GetAxis("Vertical")), 0) && Mathf.Approximately(Mathf.Abs(Input.GetAxis("Horizontal")), 0))
        {
            timeWithoutInput += Time.deltaTime;
        }
        else
        {
            timeWithoutInput = 0;
            zooming = false;
        }

        // Zoom to vantage point
        if (!zooming && timeWithoutInput > zoomWait)
        {
            Vector3 endPortalDir = (endPortalT.position - playerRB.position).normalized;
            Vector3 targetPos = playerRB.position - baseCamOffset + (endPortalDir * 3);
            Quaternion targetRot = Quaternion.LookRotation(endPortalDir, playerCont.UpDir);
            if (lerpToPosAndRot) { StopCoroutine(camLerp); }
            camLerp = StartCoroutine(LerpToPositionAndRotation(camTransform, targetPos, targetRot, LerpFunction.smootherStep, zoomSpeed));
            zooming = true;
        }
    }

    public void UpdateCamPosandRotBasedOnPlayer()
    {
        if (zooming) return;
        // Set new camera follow position
        Vector3 targetPos = playerRB.position - ((baseCamOffset.x * playerCont.RightDir) + (baseCamOffset.y * playerCont.UpDir) + (baseCamOffset.z * playerCont.ForwardDir));
        Quaternion targetRot = Quaternion.LookRotation(playerCont.ForwardDir, playerCont.UpDir) * baseCamRot;
        if (Vector3.Distance(camTransform.position, targetPos) > 0.1f || Quaternion.Angle(camTransform.rotation, targetRot) > 3f)
        {
            if (lerpToPosAndRot) { StopCoroutine(camLerp); }
            camLerp = StartCoroutine(LerpToPositionAndRotation(camTransform, targetPos, targetRot, LerpFunction.exponentialEase, updateSpeed));
        }
    }

    public void FreeLook()
    {
        if (!freeLook) return;
        if (Cursor.visible) return;

        // Get cursor input
        float tilt = -Input.GetAxis("Mouse Y");
        float turn = Input.GetAxis("Mouse X");
        // turn player forwardDir
        Vector3 newForwardDir = Quaternion.AngleAxis(turn * freeLookSensitivity, playerCont.UpDir) * playerCont.ForwardDir;
        playerCont.SetForwardDir(newForwardDir);
        // tilt pivot 
        pivotTransform.localRotation = pivotTransform.localRotation * Quaternion.Euler(tilt * freeLookSensitivity, 0, 0);
        // Return pivot to base rotation while moving
        if (Mathf.Approximately(Mathf.Abs(tilt), 0) && playerRB.velocity.magnitude > playerCont.alignVelocityThreshold)
        {
            pivotTransform.localRotation = Quaternion.RotateTowards(pivotTransform.localRotation, basePivotRot, 30f * Time.deltaTime / updateSpeed);
        }
    }


    private IEnumerator LerpToPositionAndRotation(Transform transfOfObject, Vector3 lerpTargetPos, Quaternion lerpTargetRot, LerpFunction function, float timeFactor)
    {
        lerpToPosAndRot = true;
        Vector3 lerpStartPos = transfOfObject.position;
        Quaternion lerpStartRot = transfOfObject.rotation;
        float timeSinceLerpStarted = 0;//Time.fixedDeltaTime;
        //float deltaTime = 0;
        float t = 0;
        while (t <= 1)
        {
            //float currLoopStart = Time.time;
            timeSinceLerpStarted += Time.deltaTime;
            t = timeSinceLerpStarted / timeFactor;
            switch (function)
            {
                case LerpFunction.linear:
                    break;
                case LerpFunction.exponentialEase:
                    t = 1f - Mathf.Pow(0.2f, t); break;
                case LerpFunction.sinEase:
                    t = Mathf.Sin(t * Mathf.PI * 0.5f); break;
                case LerpFunction.smoothStep:
                    t = t * t * (3f - 2f * t); break;
                case LerpFunction.smootherStep:
                    t = t * t * t * (t * (6f * t - 15f) + 10f); break;
            }

            // Update camera rotation and position.
            transfOfObject.position = Vector3.Lerp(lerpStartPos, lerpTargetPos, t);
            transfOfObject.rotation = Quaternion.Slerp(lerpStartRot, lerpTargetRot, t);
            yield return null;
            //deltaTime = Time.time - currLoopStart;
            //Debug.Log(deltaTime);
        }
        lerpToPosAndRot = false;
    }

    /// <summary>
    /// Sets the base cameram offset.
    /// </summary>
    /// <param name="offset">Offset.</param>
    public void SetBaseCamOffset(Vector3 offset)
    { baseCamOffset = offset; }
    /// <summary>
    /// Sets the base camera rotation.
    /// </summary>
    /// <param name="rotation">Rotation.</param>
    public void SetBaseCamRot(Quaternion rotation)
    { baseCamRot = rotation; }
    /// <summary>
    /// Sets the base pivot rotation.
    /// </summary>
    /// <param name="rotation">Rotation.</param>
    public void SetBasePivotRot(Quaternion rotation)
    { basePivotRot = rotation; }
    /// <summary>
    /// Gets the base cameram offset.
    /// </summary>
    public Vector3 GetBaseCamOffset()
    { return baseCamOffset; }
    /// <summary>
    /// Gets the base camera rotation.
    /// </summary>
    public Quaternion GetBaseCamRot()
    { return baseCamRot; }
    /// <summary>
    /// Gets the base pivot rotation.
    /// </summary>
    public Quaternion GetBasePivotRot()
    { return basePivotRot; }


    //public void UpdateCamPosandRot()
    //{
    //    // Determine new camera position/rotation
    //    Vector3 newCamPos;
    //    Quaternion newCamRot;
    //    if (Mathf.Approximately(Mathf.Abs(Input.GetAxis("Vertical")), 0) && Mathf.Approximately(Mathf.Abs(Input.GetAxis("Horizontal")), 0))
    //    { timeWithoutInput += Time.deltaTime; }
    //    else
    //    { timeWithoutInput = 0; }
    //    Debug.Log(timeWithoutInput);
    //    if (timeWithoutInput > zoomWait)
    //    {
    //        Vector3 endPortalDir = (endPortalT.position - playerRB.position).normalized;
    //        newCamPos = Vector3.Lerp(camTransform.position, (playerRB.position - baseOffset) + (endPortalDir * 3), zoomSpeed);
    //        newCamRot = Quaternion.Slerp(camTransform.rotation, Quaternion.LookRotation(endPortalDir, playerCont.UpDir), zoomSpeed);
    //    }
    //    else
    //    {
    //        newCamPos = playerRB.position - (baseOffset.x * playerCont.RightDir) + (baseOffset.y * playerCont.UpDir) + (baseOffset.z * playerCont.ForwardDir);
    //        newCamRot = Quaternion.LookRotation(playerCont.ForwardDir, playerCont.UpDir) * baseRot;
    //    }


    //    // Update camera rotation and position.
    //    camTransform.position = Vector3.Lerp(camTransform.position, newCamPos, updSpeed);
    //    //camTransform.position = Vector3.SmoothDamp(camTransform.position, newCamPos, ref currCamVelocity, updSpeed);
    //    camTransform.rotation = Quaternion.Slerp(camTransform.rotation, newCamRot, updSpeed);
    //}


}
