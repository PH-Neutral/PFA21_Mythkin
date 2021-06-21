using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshkynCore.Audio;

namespace AshkynCore.UI {
    public class MainMenu : CustomMenu {
        protected override void Start() {
            base.Start();
            AudioManager.instance.PlayMusic(AudioTag.musicMenu01, true);
        }
        public void LoadGame() {
            AudioManager.instance.StopAudio(AudioTag.musicMenu01, null);
            LoadScene(1, true);
        }
    }
}