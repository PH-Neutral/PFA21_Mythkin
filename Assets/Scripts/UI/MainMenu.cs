using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshkynCore.Audio;

namespace AshkynCore.UI {
    public class MainMenu : CustomMenu {
        public AudioTag backgroundMusic;

        public Text invisible, bestTime, collectibles;
        protected override void Start() {
            base.Start();
            AudioManager.instance.PlayMusic(backgroundMusic, true);

            bestTime.text = "Best time : " + (GameData.bestTime!=-1? TimeSpan.FromSeconds(GameData.bestTime).ToString("m\\:ss\\.fff"):"???");
            invisible.text = "Invisible : " + (GameData.invisible == 1 ? "Yes" : "No");
            collectibles.text = "Collectibles : " + GameData.maxCollectiblesCount + "/" + GameManager.collectiblesTotal;
        }
        public void LoadGame() {
            AudioManager.instance.StopAudio(backgroundMusic, null);
            GameData.showIntro = true;
            LoadScene(1, true);
        }
    }
}