using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshkynCore.UI {
    public class MenuOptions : CustomMenu {
         
        protected override void Awake() {
            base.Awake();
        }

        public void SetMusicVolume(Slider slider) {
            AudioManager.instance.volumeMusic = slider.value;
        }

        public void SetSoundVolume(Slider slider) {
            AudioManager.instance.volumeSound = slider.value;
        }
    }
}
