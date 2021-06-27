using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshkynCore.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public MenuPause menuPause;
    public Text timeTxt, collectiblesTxt;
    public Text pauseTimeTxt, pauseCollectiblesTxt;
    public Text tutoTxt;
    public Text winOrLoseTxt;

    public Image pauseInvisibleImg, PauseCollectiblesImg, invisibleImg, collectiblesImg;

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
            UpdateHighscoreLayout(pauseTimeTxt, pauseCollectiblesTxt, PauseCollectiblesImg, pauseInvisibleImg, true);
            menuPause.Show();
        }
        else menuPause.Hide();
    }

    public void UpdateWinOrLoseLayout() {
        UpdateHighscoreLayout(timeTxt, collectiblesTxt, collectiblesImg, invisibleImg, true);
    }
    public static void UpdateHighscoreLayout(Text timeTxt, Text collectibleTxt, Image collectibleImg, Image invisibleImg, bool inGame) {
        GameManager gm = GameManager.Instance;
        string time = TimeSpan.FromSeconds(inGame ? gm.timer : GameData.bestTime).ToString("m\\:ss\\.fff") + "s";
        timeTxt.text = (inGame ? "Temps" : "Meilleur temps") + " : " + (inGame ? time : (GameData.bestTime > -1 ? time : "---"));
        collectibleTxt.text = (inGame ? gm.collectiblesCount : GameData.maxCollectiblesCount) + "/" + GameManager.collectiblesTotal;
        collectibleImg.sprite = (inGame ? gm.collectiblesCount : GameData.maxCollectiblesCount) == GameManager.collectiblesTotal ? GameData.allCollectiblesSprt : GameData.notAllCollectiblesSprt;
        invisibleImg.sprite = (inGame ? gm.isInvisible : GameData.invisible == 1) ? GameData.invisibleSprt : GameData.notInvisibleSprt;
    }
}
 