using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshkynCore.Audio;

namespace AshkynCore.UI {
    public class MainMenu : CustomMenu {
        public AudioTag backgroundMusic;

        protected override void Start() {
            base.Start();
            AudioManager.instance.PlayMusic(backgroundMusic, true);
        }
        public void LoadGame() {
            AudioManager.instance.StopAudio(backgroundMusic, null);
            LoadScene(1, true);
        }
    }
}