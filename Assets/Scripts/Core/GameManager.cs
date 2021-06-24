using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AshkynCore.Audio;
using AshkynCore.UI;

public class GameManager : MonoBehaviour
{
    public const int collectiblesTotal = 3;
    public static GameManager Instance;

    public bool GamePaused {
        get {
            return Time.timeScale == 0;
        }
        set {
            Time.timeScale = value ? 0 : 1;
            UpdateCursor();
        }
    }
    public Intro intro;
    public bool disablePauseToggle = false;
    public AudioTag backgroundMusic;
    public UnityEngine.AI.NavMeshSurface terrain;
    public PlayerCharacter player;
    public Material matEnemyPatrol, matEnemySearch, matEnemyAttack;
    public MenuOptions menuOptions;
    public CustomMenu winMenu;
    public int collectiblesCount = 0;
    public bool isInvisible = true;

    [SerializeField] Transform lowestPoint;
    public float timer;
    bool gameHasStarted = false, stopTimer = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        UpdateNavMesh();
    }
    private void Start()
    {
        
    }
    private void Update() {
        if (!gameHasStarted)
        {
            if (intro.hasFinished)
            {
                intro.Hide();
                StartGame();
            }
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape) && !disablePauseToggle) {
            TogglePause();
        }
        if(!stopTimer) timer += Time.deltaTime;
    }
    private void LateUpdate() {
        if(lowestPoint != null && player != null) {
            // if player falls from map
            if(player.transform.position.y < lowestPoint.position.y) GameOver();
        }
    }
    public void StartScene() {
        if (GameData.showIntro)
        {
            intro.StartIntro();
            GameData.showIntro = false;
            GamePaused = true;
        }
        else
        {
            intro.Hide();
            StartGame();
        }
    }
    public void StartGame() {
        GamePaused = false;
        stopTimer = false;
        timer = 0;
        gameHasStarted = true;
        AudioManager.instance.PlayMusic(backgroundMusic, true);
    }
    public void TogglePause() {
        //AudioManager.instance.PauseAudio(backgroundMusic, null);
        GamePaused = !GamePaused;
        UIManager.Instance.DisplayPauseMenu(GamePaused);
        if (!GamePaused) {
            menuOptions.Hide();
        }
    }
    public void UpdateNavMesh() {
        terrain.BuildNavMesh();
    }
    public void GameOver()
    {
        UIManager.Instance.winOrLoseTxt.text = "Game Over";
        GamePaused = true;
        disablePauseToggle = true;
        stopTimer = true;
        UpdateWinOrLoseLayout();
        winMenu.Show();
    }
    public void Win()
    {
        UIManager.Instance.winOrLoseTxt.text = "You Win";
        GamePaused = true;
        disablePauseToggle = true;
        stopTimer = true;
        if (collectiblesCount > GameData.maxCollectiblesCount)
        {
            GameData.maxCollectiblesCount = collectiblesCount;
        }
        if (GameData.bestTime < 0 || timer < GameData.bestTime)
        {
            GameData.bestTime = timer;
        }
        if (isInvisible)
        {
            GameData.invisible = 1;
        }
        UpdateWinOrLoseLayout();
        winMenu.Show();
    }

    public void UpdateWinOrLoseLayout()
    {
        UIManager.Instance.timeTxt.text = "Time : " + TimeSpan.FromSeconds(timer).ToString("m\\:ss\\.fff");
        UIManager.Instance.collectiblesTxt.text = collectiblesCount + "/" + collectiblesTotal;
        UIManager.Instance.collectiblesImg.sprite = collectiblesCount == collectiblesTotal ? GameData.allCollectiblesSprt : GameData.notAllCollectiblesSprt;
        UIManager.Instance.invisibleImg.sprite = isInvisible ? GameData.invisibleSprt : GameData.notInvisibleSprt;
    }

    void UpdateCursor() {
        if(GamePaused) {
            Cursor.lockState = CursorLockMode.None;
        } else {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnApplicationQuit() {
        PlayerPrefs.Save();
    }
    private void OnApplicationFocus(bool focus) {
        if(focus) {
            UpdateCursor();
        }
    }
}
