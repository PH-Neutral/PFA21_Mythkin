using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshkynCore.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public MenuPause menuPause;
    public Text invisibleTxt, timeTxt, collectiblesTxt;
    public Text pauseInvisibleTxt, pauseTimeTxt, pauseCollectiblesTxt;

    GameManager gm;

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

    private void Start()
    {
        gm = GameManager.Instance;
    }

    public void DisplayPauseMenu(bool display) {
        if (display)
        {
            pauseInvisibleTxt.text = "Invisible : " + (gm.isInvisible ? "yes" : "no");
            pauseTimeTxt.text = "Timer : " + gm.timer.ChangePrecision(0).ToString() + "s";
            pauseCollectiblesTxt.text = "Collectibles : " + gm.collectiblesCount.ToString() + "/" + GameManager.collectiblesTotal.ToString();
            menuPause.Show();
        }
        else menuPause.Hide();
    }
}
 