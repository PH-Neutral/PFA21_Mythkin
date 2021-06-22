using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshkynCore.UI {
    public class MenuPause : CustomMenu {
        public float musicVolumeRatio = 0.5f;

        protected override void OnShow() {
            AudioManager.instance.AdjustMusicVolume(GameManager.Instance.backgroundMusic, musicVolumeRatio);
            
        }
        protected override void OnHide() {
            AudioManager.instance.AdjustMusicVolume(GameManager.Instance.backgroundMusic, 1);
            GameManager.Instance.GamePaused = false;
        }
        public void LoadMainMenu() {
            LoadScene(0);
        }
    }
}
