using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class GameController : MonoBehaviour
{

    public LevelController levelController;
    private PlayerController playerController;
    private CameraController cameraController;
    private Canvas mainUI, loadUI;
    private bool levelShouldStart;

    private void Start()
    {
        // Pause game at start
        Time.timeScale = 0;

        mainUI = GameObject.Find("MainUIScreen").GetComponent<Canvas>();
        loadUI = GameObject.Find("LoadingScreen").GetComponent<Canvas>();
        mainUI.gameObject.SetActive(false);
        loadUI.gameObject.SetActive(false);
        levelController = gameObject.AddComponent<LevelController>();

        CreateNewLevelAndStart();
    }

    private void Update()
    {
        if (playerController == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            { playerController = player.GetComponent<PlayerController>(); }
        }
        if (cameraController == null)
        {
            GameObject cameraRig = GameObject.Find("CameraRig");
            if (cameraRig != null)
            { cameraController = cameraRig.GetComponent<CameraController>(); }
        }

        if (levelShouldStart)
        {
            loadUI.GetComponentInChildren<Slider>().value = levelController.LevelBuiltProgressPercentage;
            if (levelController.LevelIsPresent)
            {
                loadUI.gameObject.SetActive(false);
                loadUI.GetComponentInChildren<Slider>().value = 0;
                levelShouldStart = false;
                StartLevel();
            }
        }
    }


    public void GameOver()
    {
        StopLevel();
        mainUI.transform.Find("DeadOrAlive").GetComponent<Text>().text = "DEATH!";
        mainUI.gameObject.SetActive(true);

    }

    public void GameFinished()
    {
        StopLevel();
        mainUI.transform.Find("DeadOrAlive").GetComponent<Text>().text = "LIFE!";
        mainUI.gameObject.SetActive(true);
    }

    public void RestartLevel()
    {
        levelController.RestartLevel();
        levelShouldStart = true;
    }

    public void CreateNewLevelAndStart()
    {
        mainUI.gameObject.SetActive(false);
        loadUI.gameObject.SetActive(true);
        loadUI.GetComponentInChildren<Slider>().value = levelController.LevelBuiltProgressPercentage;

        levelController.BuildNewLevel();
        levelShouldStart = true;
    }

    public void StopLevel()
    {
        Time.timeScale = 0;
        playerController.DeactivatePlayer();
        cameraController.DeactivateCamera(); // FIXME should this be a static thing too?
        CubeOfDeathController.cubesActive = false;
    }

    public void StartLevel()
    {
        mainUI.gameObject.SetActive(false);
        loadUI.gameObject.SetActive(false);
        playerController.ActivatePlayer(); // FIXME should this be a static thing too?
        cameraController.ActivateCamera(); // FIXME should this be a static thing too?
        CubeOfDeathController.cubesActive = true;
        Time.timeScale = 1;
    }


    void OnEnable()
    {
        GameEventManager.OnPlayerDeath += GameOver;
        GameEventManager.OnPlayerWin += GameFinished;
    }

    void OnDisable()
    {
        GameEventManager.OnPlayerDeath -= GameOver;
        GameEventManager.OnPlayerWin -= GameFinished;
    }


}
