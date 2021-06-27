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
    [HideInInspector] public bool gameHasStarted = false, disablePauseToggle = false;
    [HideInInspector] public int collectiblesCount = 0;
    [HideInInspector] public bool isInvisible = true;
    [HideInInspector] public float timer;
    public Intro intro;
    public TravelingHandler travelingHandler;
    public AudioTag backgroundMusic;
    public NavMeshSurface terrain;
    public PlayerCharacter player;
    public Material matEnemyPatrol, matEnemySearch, matEnemyAttack;
    public MenuOptions menuOptions;
    public CustomMenu winMenu;

    [SerializeField] Transform lowestPoint;
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
        if(!gameHasStarted) return;

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
        AudioManager.instance.PlayMusic(backgroundMusic, true);
        AudioManager.instance.AdjustMusicVolume(backgroundMusic, 1 / 3f);

        if (GameData.showIntro) {
            intro.StartIntro(() => { StartTraveling(); });
            GameData.showIntro = false;
            return;
        }
        StartTraveling();
    }
    public void StartTraveling() {
        disablePauseToggle = true;
        GamePaused = false;
        AudioManager.instance.AdjustMusicVolume(backgroundMusic, 1f);
        if(travelingHandler == null) {
            StartGame();
            return;
        }
        travelingHandler.StartTraveling(() => { StartGame(); });
    }
    public void StartGame() {
        //GamePaused = false;
        disablePauseToggle = false;
        stopTimer = false;
        timer = 0;
        gameHasStarted = true;

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
        UIManager.Instance.UpdateWinOrLoseLayout();
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
        UIManager.Instance.UpdateWinOrLoseLayout();
        Utils.HideCursor(false);
        winMenu.Show();
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
