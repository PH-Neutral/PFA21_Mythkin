using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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
            Application.Quit();
        } else if(Input.GetKeyDown(KeyCode.Backspace)) {
            SceneManager.LoadScene(0);

        }
    }
    public void UpdateNavMesh() {
        terrain.BuildNavMesh();
    }
}
