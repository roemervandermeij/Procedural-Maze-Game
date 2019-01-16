using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLerp : MonoBehaviour
{
    Vector3 aap;
    int count = 0;

    void Start()
    {

        //Debug.Log(aap);

    }

    void Update()
    {
        count++;
        if (count < 20)
        {
            Move(Vector3.one);
        }
        //Debug.Log(transform.position);}
        else if (count == 20)
        {
            Debug.Log("stopped");
        }
        Debug.Log(Time.frameCount + " - " + transform.position);
    }

    public void Move(Vector3 pos)
    {
        //pos = pos * Time.deltaTime;
        Vector3 target = transform.position + pos;
        //while (!transform.position.ComponentsAreEqualTo(target))
        //{
        transform.position = Vector3.Slerp(transform.position, target, .9f);

        //}

    }

}