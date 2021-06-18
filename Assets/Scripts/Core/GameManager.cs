using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AshkynCore.Audio;
using AshkynCore.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool GamePaused {
        get {
            return Time.timeScale == 0;
        }
        set {
            Time.timeScale = value ? 0 : 1;
        }
    }
    public UnityEngine.AI.NavMeshSurface terrain;
    public PlayerCharacter player;
    public Material matEnemyPatrol, matEnemySearch, matEnemyAttack;
    public MenuOptions menuOptions;
    public CustomMenu gameOverMenu, winMenu;

    [SerializeField] Transform lowestPoint;
    float timer; 
    bool wonOrLost = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        UpdateNavMesh();
    }
    private void Start()
    {
        AudioManager.instance.PlayMusic(AudioTag.musicLevel01, true);
        GamePaused = false;
        timer = 0;
    }
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            TogglePause();
        }
        timer += Time.deltaTime;

        if(lowestPoint != null) {
            // if player falls from map
            if(player.transform.position.y < lowestPoint.position.y) GameOver();
        }
    }
    public void TogglePause() {
        if(wonOrLost) return;
        AudioManager.instance.PauseAudio(AudioTag.musicLevel01, null);
        GamePaused = !GamePaused;
        UIManager.Instance.DisplayPauseMenu(GamePaused);
        if (GamePaused)
        {
            AudioManager.instance.PlayMusic(AudioTag.musicMenu02);
        }
        else
        {
            AudioManager.instance.StopAudio(AudioTag.musicMenu02, null);
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
        wonOrLost = true;
        UIManager.Instance.timeTxt.text = "Time : " + timer.ChangePrecision(0).ToString() + "s";
        winMenu.Show();
    }
}
