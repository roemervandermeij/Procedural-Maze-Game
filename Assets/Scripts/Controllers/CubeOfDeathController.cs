using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// FIXME reimplement this, with one controller instance, and a smaller script per object that does the trigger and the eating
/// <summary>
/// Class for controlling Cube(s) of death.
/// </summary>
public class CubeOfDeathController : MonoBehaviour
{
    private static readonly float moveSpeed = 3f;
    private static readonly int maxSpeedFactor = 5;
    private static readonly float growingSpeed = 5f;
    private static readonly float suction = 5f;
    private static readonly float eventHorizon = 1.5f; // in units of mazeFrameScale
    public static Vector3 cubeScale;
    public static Vector3 MazeScale { get; set; }
    public static MazeFrameSplines MazeFrameSplines { get; set; }
    public static bool cubesActive;

    private static float currEventHorizon;
    private static Rigidbody playerRB;
    private static GravityFrameController gravityFrameController;
    private static PostProcessVolume volume;
    private static Vignette vignette;
    private static Dictionary<MazeFrameSplines.SplineSegment, bool> cubeHasBeenHere;
    private static float currMoveSpeed = moveSpeed;

    public MazeFrameSplines.SplineSegment CurrSplineSegment { get; set; }
    public bool ReversedSpline { get; set; }
    public bool IsOnSpline { get; set; }
    private bool goingForward = true;
    private Transform CODTrans;


    private void OnEnable()
    {
        CODTrans = gameObject.transform;
        playerRB = GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        gravityFrameController = FindObjectOfType<GameController>().levelController.gravityFrameController;
        currEventHorizon = eventHorizon * MazeScale.ComponentMax();
    }

    public void Activate()
    {
        // (Re)Set volume for post process vignette effect
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
            Destroy(vignette);
            vignette = null;
        }
        vignette = ScriptableObject.CreateInstance<Vignette>();
        vignette.enabled.Override(true);
        vignette.intensity.Override(0f);
        vignette.roundness.Override(1f);
        vignette.smoothness.Override(1f);
        volume = PostProcessManager.instance.QuickVolume(8, 100f, vignette);

        // (Re)Set cubeHasBeenHere
        cubeHasBeenHere = new Dictionary<MazeFrameSplines.SplineSegment, bool>(MazeFrameSplines.SplineSegments.Count);
        foreach (MazeFrameSplines.SplineSegment segment in MazeFrameSplines.SplineSegments)
        { cubeHasBeenHere.Add(segment, false); }

        // Reset some initials, as we need to guarantee initial behavior
        CurrSplineSegment = null;
        currEventHorizon = eventHorizon * MazeScale.ComponentMax();

        // activate
        cubesActive = true;
    }

    private void Update()
    {
        if (!cubesActive) return;
        if (gameObject == null) return;

        if (!IsOnSpline)
        { InitialPhase(); }
        if (IsOnSpline)
        { MoveCubeAlongSplineAndReplicate(); }
    }


    private void FixedUpdate()
    {
        if (!cubesActive) return;
        if (gameObject == null) return;
        EatPlayer();
    }


    /// <summary>
    /// Translate cube along splines to follow player, and replicate itself at junctions
    /// </summary>
    private void MoveCubeAlongSplineAndReplicate()
    {
        // Get current distance value
        float currD = CurrSplineSegment.spline.GetDistanceOnSplineFromPosition(CODTrans.position, ReversedSpline);
        // Project position along spline
        float projD;
        if (goingForward) { projD = currD + (moveSpeed * Time.deltaTime); }
        else { projD = currD - (moveSpeed * Time.deltaTime); }
        // Check whether end is reached
        bool reachedEnd = false;
        if (goingForward) { if (projD > CurrSplineSegment.spline.SplineTotalDistance) { reachedEnd = true; } }
        else { if (projD < 0) { reachedEnd = true; } }
        // If end is reached, reverse direction if no untouched segments are found, otherwise, continue along first 
        // segment and replicate cube and move them along the others 
        if (reachedEnd)
        {
            // See if other segment is present
            List<MazeFrameSplines.SplineSegment> splineNeighbors;
            if (!ReversedSpline) { splineNeighbors = new List<MazeFrameSplines.SplineSegment>(CurrSplineSegment.endNeighbors); }
            else { splineNeighbors = new List<MazeFrameSplines.SplineSegment>(CurrSplineSegment.startNeighbors); }
            // Discard splines already being travelled
            for (int i = splineNeighbors.Count - 1; i >= 0; i--)
            { if (cubeHasBeenHere[splineNeighbors[i]]) { splineNeighbors.RemoveAt(i); } }
            // Turn around if none present
            if (splineNeighbors.Count == 0)
            {
                if (goingForward) { projD = CurrSplineSegment.spline.SplineTotalDistance; goingForward = false; }
                else { projD = 0; goingForward = true; }
            }
            else
            {
                // Instantiate new cube for every neighbor that hasn't been visisted yet, except the first
                for (int i = 0; i < splineNeighbors.Count; i++)
                { cubeHasBeenHere[splineNeighbors[i]] = true; }
                for (int i = 1; i < splineNeighbors.Count; i++)
                {
                    GameObject cube = Instantiate(gameObject, CODTrans.position, CODTrans.rotation, gameObject.transform.parent);
                    CubeOfDeathController CODcont = cube.GetComponent<CubeOfDeathController>();
                    CODcont.CurrSplineSegment = splineNeighbors[i];
                    CODcont.IsOnSpline = true;
                    if (Vector3.Distance(CODTrans.position, CODcont.CurrSplineSegment.EndPoint) < Vector3.Distance(CODTrans.position, CODcont.CurrSplineSegment.StartPoint))
                    { CODcont.ReversedSpline = true; }
                    else
                    { CODcont.ReversedSpline = false; }
                }
                // For this cube, continue on new segment with remainder of projected distance
                projD = projD - CurrSplineSegment.spline.SplineTotalDistance;
                CurrSplineSegment = splineNeighbors[0];
                if (Vector3.Distance(CODTrans.position, CurrSplineSegment.EndPoint) < Vector3.Distance(CODTrans.position, CurrSplineSegment.StartPoint))
                { ReversedSpline = true; }
                else
                { ReversedSpline = false; }
            }
        }
        float projT = CurrSplineSegment.spline.GetTAtDistance(projD, ReversedSpline);
        CODTrans.position = CurrSplineSegment.spline.GetPointOnSpline(projT);
        CODTrans.rotation = Quaternion.LookRotation(CurrSplineSegment.spline.GetTangentToPointOnSpline(projT), CurrSplineSegment.spline.DefaultGetNormalAtT(projT));
    }

    /// <summary>
    /// Grow cube to full size and move towards player
    /// </summary>
    private void InitialPhase()
    {
        // Find starting spline if necessary
        if (CurrSplineSegment == null)
        {
            // Find starting spline(s)
            foreach (MazeFrameSplines.SplineSegment segment in MazeFrameSplines.SplineSegments)
            { if (!segment.isJunction) { if (segment.NodeIdentifiers.Contains("start")) { CurrSplineSegment = segment; } } }
            if (Vector3.Distance(CODTrans.position, CurrSplineSegment.EndPoint) < Vector3.Distance(CODTrans.position, CurrSplineSegment.StartPoint))
            { ReversedSpline = true; }
            cubeHasBeenHere[CurrSplineSegment] = true;
        }

        // Grow until at full scale, then start moving
        if (!CODTrans.localScale.ComponentsAreApproxEqualTo(cubeScale))
        {
            float step = (Time.deltaTime / growingSpeed) * cubeScale.ComponentMax();
            CODTrans.localScale = Vector3.MoveTowards(CODTrans.localScale, cubeScale, step);
            vignette.intensity.value = 0f;
        }
        else
        {
            // Move cube to starting spline until we're on it
            float currD = CurrSplineSegment.spline.GetDistanceOnSplineFromPosition(CODTrans.position, ReversedSpline);
            if (Mathf.Approximately(currD, 0))
            {
                float currT = CurrSplineSegment.spline.GetTAtDistance(currD, ReversedSpline);
                Vector3 currDistBasedPoint = CurrSplineSegment.spline.GetPointOnSpline(currT);
                if (!CODTrans.position.ComponentsAreApproxEqualTo(currDistBasedPoint))
                {
                    Vector3 moveDirection = (CurrSplineSegment.StartPoint - CODTrans.position).normalized;
                    CODTrans.position = CODTrans.position + moveDirection * moveSpeed * Time.deltaTime;
                    CODTrans.rotation = Quaternion.LookRotation(CODTrans.forward, CODTrans.up);
                }
                else
                { IsOnSpline = true; }
            }
            else
            { IsOnSpline = true; }
        }
    }


    /// <summary>
    /// Eats the player, by sucking into the cube and starting vignette effect.
    /// </summary>
    private void EatPlayer()
    {
        // Check whether we are the closest cube, and if so, eat!
        float distToPlayer = Vector3.Distance(CODTrans.position, playerRB.position);
        if (distToPlayer < currEventHorizon)
        {
            foreach (Transform child in CODTrans.parent)
            {
                if (child == CODTrans) { continue; }
                float otherDistToPlayer = Vector3.Distance(child.position, playerRB.position);
                if (otherDistToPlayer < distToPlayer) { return; }
            }
            // Eat
            Vector3 suctionDirection = gravityFrameController.FindFrameAlignedDirection(gravityFrameController.ClosestPointOnFramePlane, CODTrans.position - playerRB.position);
            vignette.intensity.value = (1f - (distToPlayer / currEventHorizon)) * .75f;
            if (distToPlayer < (currEventHorizon * .66f))
            { playerRB.AddForce(suctionDirection * (1 - (distToPlayer / (currEventHorizon * .66f))) * suction, ForceMode.Acceleration); }

            // Increase speed based on player distance to end
            float propPlayerDistToEnd = Mathf.Clamp01(Vector3.Distance(playerRB.position, MazeFrameSplines.EndPoint) / Vector3.Distance(MazeFrameSplines.StartPoint, MazeFrameSplines.EndPoint));
            float newMoveSpeed = moveSpeed + maxSpeedFactor * moveSpeed * propPlayerDistToEnd;
            if (newMoveSpeed > currMoveSpeed) { currMoveSpeed = newMoveSpeed; }
        }
    }

    /// <summary>
    /// Calls player death event when player touches cube
    /// </summary>
    /// <param name="other">Other.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        { GameEventManager.PlayerHasDied(); }
    }

    private void OnDestroy()
    {
        if (volume != null)
        { RuntimeUtilities.DestroyVolume(volume, true, true); }
    }

}
