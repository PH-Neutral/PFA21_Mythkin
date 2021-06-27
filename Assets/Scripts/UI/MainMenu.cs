using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshkynCore.Audio;

namespace AshkynCore.UI {
    public class MainMenu : CustomMenu {
        public AudioTag backgroundMusic;

        public Text bestTimeTxt, collectiblesTxt;
        public Image invisibleImg, collectiblesImg;
        protected override void Start() {
            base.Start();
            AudioManager.instance.PlayMusic(backgroundMusic, true);

            UIManager.UpdateHighscoreLayout(bestTimeTxt, collectiblesTxt, collectiblesImg, invisibleImg, false);
        }
        public void LoadGame() {
            AudioManager.instance.StopAudio(backgroundMusic, null);
            GameData.showIntro = GameData.showTutorial = true;
            LoadScene(1, true);
        }
    }
}