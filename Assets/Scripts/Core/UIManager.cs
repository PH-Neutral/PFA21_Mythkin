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
            pauseInvisibleImg.sprite = gm.isInvisible ? GameData.invisibleSprt : GameData.notInvisibleSprt;
            pauseTimeTxt.text = "Timer : " + TimeSpan.FromSeconds(gm.timer).ToString("m\\:ss\\.fff");
            pauseCollectiblesTxt.text = gm.collectiblesCount.ToString() + "/" + GameManager.collectiblesTotal.ToString();
            PauseCollectiblesImg.sprite = gm.collectiblesCount == GameManager.collectiblesTotal ? GameData.allCollectiblesSprt : GameData.notAllCollectiblesSprt;
            menuPause.Show();
        }
        else menuPause.Hide();
    }
}
 