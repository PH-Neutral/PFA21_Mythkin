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
        }
    }
    public Intro intro;
    public bool gameHasStarted = false, disablePauseToggle = false;
    public AudioTag backgroundMusic;
    public NavMeshSurface terrain;
    public PlayerCharacter player;
    public Material matEnemyPatrol, matEnemySearch, matEnemyAttack;
    public MenuOptions menuOptions;
    public CustomMenu winMenu;
    public int collectiblesCount = 0;
    public bool isInvisible = true;

    [SerializeField] Transform lowestPoint;
    public float timer;
    bool stopTimer = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        UpdateNavMesh();
        GamePaused = true;
        Utils.HideCursor(true);
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
        if (GameData.showIntro) {
            intro.StartIntro();
            GameData.showIntro = false;
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

        //Utils.HideCursor(false);
    }
    public void TogglePause() {
        //AudioManager.instance.PauseAudio(backgroundMusic, null);
        GamePaused = !GamePaused;
        UpdateCursor();
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
        UIManager.Instance.winOrLoseTxt.text = "Défaite...";
        GamePaused = true;
        disablePauseToggle = true;
        stopTimer = true;
        UpdateWinOrLoseLayout();
        Utils.HideCursor(false);
        winMenu.Show();
    }
    public void Win()
    {
        UIManager.Instance.winOrLoseTxt.text = "Victoire !";
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
        Utils.HideCursor(false);
        winMenu.Show();
    }

    public void UpdateWinOrLoseLayout()
    {
        UIManager.Instance.timeTxt.text = "Temps : " + TimeSpan.FromSeconds(timer).ToString("m\\:ss\\.fff") + "s";
        UIManager.Instance.collectiblesTxt.text = collectiblesCount + "/" + collectiblesTotal;
        UIManager.Instance.collectiblesImg.sprite = collectiblesCount == collectiblesTotal ? GameData.allCollectiblesSprt : GameData.notAllCollectiblesSprt;
        UIManager.Instance.invisibleImg.sprite = isInvisible ? GameData.invisibleSprt : GameData.notInvisibleSprt;
    }

    void UpdateCursor() {
        Utils.HideCursor(!GamePaused);
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
