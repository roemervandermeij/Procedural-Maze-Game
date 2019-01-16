using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MindWarp
{
    [Range(1, 3)] protected int intensity;
    protected Rigidbody playerRB;
    protected PlayerController playerCont;
    protected Vector3 centerPosition;
    protected Vector3[] neighborPosition;

    public void Activate(int intensity, Vector3 centerPosition, Vector3[] neighborPosition)
    {
        this.intensity = intensity;
        this.centerPosition = centerPosition;
        this.neighborPosition = neighborPosition;
        playerRB = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody>();
        playerCont = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        UseIntensity();
        ApplyEffect();
    }

    public void Deactivate()
    { CancelEffect(); }

    protected abstract void UseIntensity();

    protected abstract void ApplyEffect();

    protected abstract void CancelEffect();

}
