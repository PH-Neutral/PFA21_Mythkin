using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshkynCore.Audio;

namespace AshkynCore.UI {
    public class MainMenu : CustomMenu {
        public Text invisible, bestTime, collectibles;
        protected override void Start() {
            base.Start();
            bestTime.text = "Best time : " + (GameData.bestTime!=-1? GameData.bestTime.ChangePrecision(1) + "s":"???");
            invisible.text = "Invisible : " + (GameData.invisible == 1 ? "Yes" : "No");
            collectibles.text = "Collectibles : " + GameData.maxCollectiblesCount + "/" + GameManager.collectiblesTotal;
            AudioManager.instance.PlayMusic(AudioTag.musicMenu01, true);
        }
        public void LoadGame() {
            AudioManager.instance.StopAudio(AudioTag.musicMenu01, null);
            LoadScene(1, true);
        }
    }
}