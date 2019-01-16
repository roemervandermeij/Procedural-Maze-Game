using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndPortalController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        { GameEventManager.PlayerHasWon(); }
    }
}
