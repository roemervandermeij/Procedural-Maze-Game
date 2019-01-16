using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornerObjectBendingTest : MonoBehaviour
{

    Vector3 poscent = new Vector3(5, 5, 5);
    Vector3 posnb1 = new Vector3(5, 5, 1);
    Vector3 posnb2 = new Vector3(4, 5, 9);
    Vector3 posnb3 = new Vector3(9, 4, 5);
    //Vector3 posnb4 = new Vector3(5, 4, 9);
    //Vector3 posnb5 = new Vector3(6, 9, 5);


    void Start()
    {
        GameObject go = Resources.Load("Prefabs/" + "MazeCylinderConnection") as GameObject;
        GameObject parentObj = new GameObject("Prefabs/" + "connparent");
        parentObj.AddComponent<MeshFilter>();
        parentObj.AddComponent<MeshRenderer>();
        parentObj.GetComponent<MeshRenderer>().material = go.GetComponent<MeshRenderer>().sharedMaterial;
        parentObj.transform.position = poscent;

        float angle;
        float connSpace = 1;
        GameObject connobj;
        Vector3 currdirA;
        Vector3 currdirB;
        Vector3 currUp;
        Vector3 currForward;
        Vector3 nb1Dir = posnb1 - poscent;
        Vector3 nb2Dir = posnb2 - poscent;
        Vector3 nb3Dir = posnb3 - poscent;
        //Vector3 nb4Dir = posnb4 - poscent;
        //Vector3 nb5Dir = posnb5 - poscent;

        for (int i = 0; i < 2; i++)
        {
            if (i == 0)
            {
                currdirA = nb1Dir;
                currdirB = nb2Dir;
            }
            else
            {
                currdirA = nb1Dir;
                currdirB = nb3Dir;
            }
            // MazeCylinderConnection is always bended in Z in ZX plane to the left, around Y
            // Create a new axis system, where:
            // forward = the direction of the first neighbor pointing the center node
            // right = pointing in the opposite of the general direction of the second neighbor
            // up = the direction such that the above holds
            // The connection object is placed in this axis system centered on the center node, and 
            // then bended along in the forward/right plane.
            // The unsigned angle is never bigger than 180 degrees due to the 180 degree rotation of the
            // right/up plane along the forward axis when a connection is on the left vs the right
            currForward = -currdirA.normalized;
            currUp = Vector3.Cross(currForward, -currdirB.normalized); // the general direction of the object should be left for easy bending
            angle = Vector3.Angle(-currdirA, currdirB);
            connobj = Instantiate(go, parentObj.transform);
            Utilities.MeshResize(connobj, new Vector3(1, 1, connSpace));
            Utilities.MeshBendCircle(connobj, angle, 2, 1);
            connobj.transform.rotation = Quaternion.LookRotation(currForward, currUp);
            //connobj.transform.position = poscent;
        }

        Utilities.MergeChildrenOfParent(parentObj, false);

        //Utilities.MeshResize(base.gameObject, new Vector3(1, 1, 3));
        //Utilities.MeshBendCircle(base.gameObject, 290f, 2, 1);

    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(poscent, .1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(posnb1, .1f);
        Gizmos.DrawSphere(posnb2, .1f);
        //Gizmos.DrawSphere(posnb3, .1f);
        //Gizmos.DrawSphere(posnb4, .1f);
        //Gizmos.DrawSphere(posnb5, .1f);

        Gizmos.DrawLine(poscent, posnb1);
        Gizmos.DrawLine(poscent, posnb2);
        //Gizmos.DrawLine(poscent, posnb3);
        //Gizmos.DrawLine(poscent, posnb4);
        //Gizmos.DrawLine(poscent, posnb5);

    }

}
