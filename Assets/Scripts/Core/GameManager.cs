using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

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
    public NavMeshSurface terrain;

    public Material matEnemyPatrol, matEnemySearch, matEnemyAttack;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            TogglePause();
        }
    }
    public void TogglePause() {
        GamePaused = !GamePaused;
        UIManager.Instance.DisplayPauseMenu(GamePaused);
    }
    public void UpdateNavMesh() {
        terrain.BuildNavMesh();
    }
}
