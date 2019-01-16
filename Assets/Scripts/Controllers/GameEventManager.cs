using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEventManager : MonoBehaviour
{
    public delegate void PlayerDeathAction();
    public static event PlayerDeathAction OnPlayerDeath;
    public static void PlayerHasDied()
    {
        OnPlayerDeath();
    }


    public delegate void PlayerWinAction();
    public static event PlayerWinAction OnPlayerWin;
    public static void PlayerHasWon()
    {
        OnPlayerWin();
    }
}
