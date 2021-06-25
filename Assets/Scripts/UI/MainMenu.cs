using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshkynCore.Audio;

namespace AshkynCore.UI {
    public class MainMenu : CustomMenu {
        public AudioTag backgroundMusic;

        public Text bestTime, collectibles;
        public Image invisibleImg, collectiblesImg;
        protected override void Start() {
            base.Start();
            AudioManager.instance.PlayMusic(backgroundMusic, true);

            bestTime.text = "Best time : " + (GameData.bestTime!=-1? TimeSpan.FromSeconds(GameData.bestTime).ToString("m\\:ss\\.fff") + "s" : "???");
            invisibleImg.sprite = GameData.invisible == 1 ? GameData.invisibleSprt : GameData.notInvisibleSprt;
            collectibles.text = GameData.maxCollectiblesCount + "/" + GameManager.collectiblesTotal;
            collectiblesImg.sprite = GameData.maxCollectiblesCount == GameManager.collectiblesTotal ? GameData.allCollectiblesSprt : GameData.notAllCollectiblesSprt;
        }
        public void LoadGame() {
            AudioManager.instance.StopAudio(backgroundMusic, null);
            GameData.showIntro = true;
            LoadScene(1, true);
        }
    }
}