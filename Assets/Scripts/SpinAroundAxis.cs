using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinAroundAxis : MonoBehaviour
{

    private float rotSpeed = 45;
    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, rotSpeed * Time.deltaTime, 0);
    }
}
