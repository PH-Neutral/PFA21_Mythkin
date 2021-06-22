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
    public bool isInvisible = true;
    public AudioTag backgroundMusic;
    public UnityEngine.AI.NavMeshSurface terrain;
    public PlayerCharacter player;
    public Material matEnemyPatrol, matEnemySearch, matEnemyAttack;
    public MenuOptions menuOptions;
    public CustomMenu gameOverMenu, winMenu;
    public int collectiblesCount = 0;

    [SerializeField] Transform lowestPoint;
    public float timer;
    bool wonOrLost = false, timerHasStarted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        UpdateNavMesh();
    }
    private void Start()
    {
        AudioManager.instance.PlayMusic(backgroundMusic, true);
        GamePaused = false;
    }
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            TogglePause();
        }
        if(timerHasStarted) timer += Time.deltaTime;
    }
    private void LateUpdate() {
        if(lowestPoint != null && player != null) {
            // if player falls from map
            if(player.transform.position.y < lowestPoint.position.y) GameOver();
        }
    }
    public void StartGame() {
        timer = 0;
        timerHasStarted = true;
    }
    public void TogglePause() {
        if(wonOrLost) return;
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
        GamePaused = true;
        wonOrLost = true;
        gameOverMenu?.Show();
    }
    public void Win()
    {
        GamePaused = true;
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
        wonOrLost = true;
        UIManager.Instance.timeTxt.text = "Time : " + timer.ChangePrecision(0).ToString() + "s";
        UIManager.Instance.collectiblesTxt.text = "Collectibles : " + collectiblesCount + "/" + collectiblesTotal;
        winMenu.Show();
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
