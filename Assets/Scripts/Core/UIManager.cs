using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshkynCore.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public MenuPause menuPause;
    public Text invisibleTxt, timeTxt;

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

    public void DisplayPauseMenu(bool display) {
        if(display) menuPause.Show();
        else menuPause.Hide();
    }
}
 