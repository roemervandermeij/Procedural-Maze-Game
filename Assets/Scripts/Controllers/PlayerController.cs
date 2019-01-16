using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 125;
    public float turnSpeed = 4;
    public float gravity = 100;
    [Range(0, 1)] public float axisAlignSpeed = .2f;
    [Range(0, 1)] public float directionAlignSpeed = .15f;
    public float alignVelocityThreshold = 4f; // should be about 1/30th of movespeed

    public Vector3 ForwardDir { get; private set; }
    public Vector3 UpDir { get; private set; }
    public Vector3 RightDir { get; private set; }

    private Vector3 currentFramePlaneSurfaceNormal;
    private bool playerActive = false;

    private Rigidbody rb;
    //private CameraController camController;
    private GravityFrameController gravityFrameController;


    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        //camController = FindObjectOfType<CameraController>();
        //rb.drag = 10f;
        //rb.useGravity = false;
        //cameraController = FindObjectOfType<CameraController>();

        //// Find gravity frame and intialize
        //gravityFrameController = FindObjectOfType<LevelController>().gravityFrameController;
        //gravityFrameController.Initialize(rb.position);

        // Initialize x,y,z axis base 
        UpDir = Vector3.up;
        RightDir = Vector3.right;
        ForwardDir = Vector3.forward;
    }


    public void ActivatePlayer()
    {
        playerActive = true;

        // Find gravity frame and re-intialize
        gravityFrameController = FindObjectOfType<LevelController>().gravityFrameController;
        gravityFrameController.Initialize(rb.position);

        // Initialize current surface variable
        currentFramePlaneSurfaceNormal = FindSurfaceNormalTowardsFramePlane();

        // Reset x,y,z axis base 
        UpDir = Vector3.up;
        RightDir = Vector3.right;
        ForwardDir = Vector3.forward;
    }
    public void DeactivatePlayer()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        playerActive = false;
    }

    private void FixedUpdate()
    {
        if (!playerActive) return;

        // First update current position on frame
        gravityFrameController.UpdatePlayerBasedElements(rb.position);

        // Find normal to current surface
        Vector3 newNormal = FindSurfaceNormalTowardsFramePlane();
        if (!newNormal.IsNaN())
        { currentFramePlaneSurfaceNormal = Vector3.Lerp(currentFramePlaneSurfaceNormal, newNormal, axisAlignSpeed); }
        //CurrentSurfaceNormal = FindPathObjSurfaceNormal();

        //ApplyGravityTowardsSurface();
        //ApplyPlayerCentricGravityTowardsFramePlane();

        MovePlayer();
        TurnPlayer();
        UpdatePlayerAxis();
        AlignPlayerForwardAxisToFrameWhenMoving();
        AlignPlayerPositionToCenterPositionWhenMoving();

        //ProjectVelocityOnSurface();

        //cameraController.UpdateCamPosandRot();
    }

    public void SetForwardDir(Vector3 direction)
    {
        ForwardDir = direction;
        RightDir = Vector3.Cross(UpDir, ForwardDir).normalized;
    }

    private void MovePlayer()
    {
        // Get desired movement from player, turn sideways only when intending to move forward as well
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 moveSideways = (Mathf.Abs(moveVertical)) * (moveHorizontal / 2.5f) * RightDir;
        Vector3 moveForward = moveVertical * ForwardDir;
        // Move using physics force push
        rb.AddForce((moveForward + moveSideways) * moveSpeed, ForceMode.Acceleration);

    }

    private void TurnPlayer()
    {
        // Turn when requested and not intending to move forward
        float moveVertical = Input.GetAxis("Vertical");
        float turnToSide = Input.GetAxis("Horizontal");
        // Rotate forward/right player axes.
        Quaternion rot = Quaternion.AngleAxis(turnToSide * turnSpeed * (1 - Mathf.Abs(moveVertical)), UpDir);
        RightDir = rot * RightDir;
        ForwardDir = rot * ForwardDir;
        rb.rotation = rot * rb.rotation;
    }


    private void UpdatePlayerAxis()
    {
        // Get normal of object surface and rotate player axes based on it
        Quaternion rot = Quaternion.FromToRotation(UpDir, currentFramePlaneSurfaceNormal);
        UpDir = rot * UpDir;
        ForwardDir = rot * ForwardDir;
        RightDir = Vector3.Cross(UpDir, ForwardDir).normalized;
        //UpDir = Vector3.Slerp(UpDir, rot * UpDir, axisAlignSpeed);
        //ForwardDir = Vector3.Slerp(ForwardDir, rot * ForwardDir, axisAlignSpeed);
        //RightDir = Vector3.Slerp(RightDir, Vector3.Cross(UpDir, ForwardDir).normalized, axisAlignSpeed);

        //// DEBUG
        //Debug.DrawRay(rb.position, ForwardDir, Color.blue);
        //Debug.DrawRay(rb.position, UpDir, Color.green);
        //Debug.DrawRay(rb.position, RightDir, Color.red);
        //// DEBUG
    }

    private void AlignPlayerForwardAxisToFrameWhenMoving()
    {
        // Align forward direction towards forward gravity line
        if (rb.velocity.magnitude > alignVelocityThreshold)
        {
            Vector3 forwardDirOnFrame = gravityFrameController.FindFrameAlignedDirection(gravityFrameController.ClosestPointOnFrameLine, ForwardDir);
            Quaternion rot = Quaternion.FromToRotation(ForwardDir, forwardDirOnFrame);
            //rightDir = rot * rightDir;
            //forwardDir = rot * forwardDir;
            ForwardDir = Vector3.Slerp(ForwardDir, rot * ForwardDir, directionAlignSpeed);
            RightDir = Vector3.Slerp(RightDir, rot * RightDir, directionAlignSpeed);
        }
    }
    private void AlignPlayerPositionToCenterPositionWhenMoving()
    {
        // Align player position direction towards forward center position on frame
        //Debug.Log(rb.velocity.magnitude);
        //float normedDistFromCenter = 1 - Vector3.Distance(gravityFrameController.ClosestPointOnFramePlane, gravityFrameController.ClosestPointOnFrameLine);
        if (rb.velocity.magnitude > alignVelocityThreshold)
        {
            ApplyPlayerCentricGravityTowardsFrameCenter(1f);
            ApplyGravityTowardsSurface(.5f);
        }
        else
        {
            ApplyGravityTowardsSurface();
        }
    }



    private void ApplyGravityTowardsSurface(float factor = 1f)
    {
        Vector3 surfNormal = FindDownwardsSurfaceNormal();
        if (!surfNormal.IsNaN())
        { ApplyGravityInDirection(-surfNormal, factor); }
        else
        { ApplyPlayerCentricGravityTowardsFrameCenter(factor); }
    }
    private void ApplyPlayerCentricGravityTowardsFrameCenter(float factor = 1f)
    {
        Vector3 gravCenter = gravityFrameController.ClosestPointOnFrameLine;
        Vector3 gravDir = gravCenter - rb.position;
        ApplyGravityInDirection(gravDir.normalized, factor);
    }
    private void ApplyPlayerCentricGravityTowardsFramePlane(float factor = 1f)
    {
        Vector3 gravCenter = gravityFrameController.ClosestPointOnFramePlane;
        Vector3 gravDir = gravCenter - rb.position;
        ApplyGravityInDirection(gravDir.normalized, factor);
    }
    private void ApplyGravityInDirection(Vector3 direction, float factor = 1f)
    {
        rb.AddForce(direction * gravity * factor, ForceMode.Acceleration);
    }

    //private void ProjectVelocityOnSurface()
    //{
    //    if (previousSurfaceNormal != CurrentSurfaceNormal)
    //    {
    //        //DEBUG
    //        //Debug.DrawRay(rb.position, rb.velocity, Color.blue);
    //        //Debug.DrawRay(rb.position, rb.angularVelocity, Color.cyan);
    //        //Debug.DrawRay(hit.point, hit.normal, Color.green);
    //        //Debug.DrawRay(rb.position, Vector3.ProjectOnPlane(rb.velocity, hit.normal), Color.white);
    //        //Debug.DrawRay(rb.position, Vector3.ProjectOnPlane(rb.angularVelocity, hit.normal), Color.gray);
    //        ////Debug.Break();
    //        ////DEBUG
    //        rb.velocity = Vector3.ProjectOnPlane(rb.velocity, CurrentSurfaceNormal); // Does surprisingly little?
    //        rb.angularVelocity = Vector3.ProjectOnPlane(rb.angularVelocity, CurrentSurfaceNormal); // Does surprisingly little?
    //    }
    //}


    private Vector3 FindSurfaceNormalTowardsFramePlane()
    {
        Vector3 gravPos = gravityFrameController.ClosestPointOnFramePlane;
        Vector3 castDir = gravPos - rb.position;
        Ray ray = new Ray(rb.position, castDir);
        LayerMask mask = 1 << 10;
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Vector3.Distance(rb.position, gravPos) * 1.5f, mask))
        { return hit.normal; }
        else
        {
            Debug.Log("Cast did not encounter object towards frame plane");
            return new Vector3(float.NaN, float.NaN, float.NaN);
        }
        //DEBUG
        //Debug.DrawLine(rb.position, gravPos, Color.yellow);
        //Debug.DrawRay(hit.point, hit.normal, Color.white);
        //Debug.Break();
        //DEBUG
    }
    private Vector3 FindDownwardsSurfaceNormal()
    {
        Vector3 castDir = -currentFramePlaneSurfaceNormal;
        Ray ray = new Ray(rb.position, castDir);
        LayerMask mask = 1 << 10;
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rb.GetComponent<MeshRenderer>().bounds.size.ComponentMax() * 2f, mask))
        { return hit.normal; }
        else
        {
            Debug.Log("Cast did not encounter downwards object");
            return new Vector3(float.NaN, float.NaN, float.NaN);
        }
    }
}






//public IEnumerator TranslatePlayerAlongSurface(Vector3 movement)
//{
//    isLerpMoving = true;
//    Vector3 startPos = tf.transform.position;
//    Vector3 targetPos = startPos + movement;
//    // Compute closest position above the object near the target position
//    // First check whether target position isn't penetrating an object
//    // Then check whether new position isn't penetrating an object
//    Vector3 newPos = Vector3.positiveInfinity;
//    bool done = false;
//    while (!done)
//    {
//        targetPos = startPos + movement;
//        while (Physics.OverlapSphere(targetPos, (tf.localScale.ComponentMax() / 2f) * 1.1f, 1 << 10).Length != 0) // if radius is a bit bigger, cast from targetpos to frame will certainly work
//        {
//            movement = movement * .9f;
//            targetPos = startPos + movement;
//            Debug.Log("Reducing movement vector for target position.");
//            if (movement.magnitude < .1)
//            {
//                Debug.Log("Movement vector could not be reduced such that target position is outside of objects.");
//                Debug.DrawLine(tf.position, targetPos, Color.red);
//                Debug.Break();
//                yield break;
//            }
//        }
//        newPos = ConvertToLegalPosition(targetPos);
//        if (Physics.OverlapSphere(newPos, (tf.localScale.ComponentMax() / 2f) * 1.1f, 1 << 10).Length != 0)
//        {
//            movement = movement * .9f;
//            Debug.Log("Reducing movement vector for new position.");
//        }
//        else
//        { done = true; }
//        if (movement.magnitude < .1)
//        {
//            Debug.Log("Movement vector could not be reduced such that new position is outside of objects.");
//            Debug.DrawLine(tf.position, newPos, Color.red);
//            Debug.Break();
//            yield break;
//        }
//    }

//    float lerpTotalTime = .75f;
//    float lerpStartTime = Time.time;
//    float currentLerpPerc = 0;
//    while (currentLerpPerc < 1)
//    {
//        currentLerpPerc = (Time.time - lerpStartTime) / lerpTotalTime;
//        Debug.DrawLine(startPos, newPos, Color.magenta);
//        tf.position = Vector3.Slerp(startPos, newPos, currentLerpPerc);
//        yield return new WaitForEndOfFrame();
//    }
//}

//// Move using translation
//moveForward = moveVertical * gravityFrameController.FindFrameAlignedDirection(rb.position, forwardDir);
//Vector3 movement = (moveForward + moveSideways);// (moveSpeed / 15) * Time.deltaTime;
//rb.transform.position = Vector3.Lerp(rb.transform.position, rb.transform.position + movement, (moveSpeed / 15) * Time.deltaTime);
////rb.transform.Translate(movement, Space.World);
//float playerCircumf = 2 * Mathf.PI * rb.transform.localScale.x / 2;
//Vector3 rotationAxis = Vector3.Cross(moveForward.normalized, upDir);
//rb.transform.Rotate(Quaternion.AngleAxis(-(movement.magnitude / playerCircumf * 360.0f), rotationAxis).eulerAngles);
//Debug.Log(rb.velocity.magnitude);