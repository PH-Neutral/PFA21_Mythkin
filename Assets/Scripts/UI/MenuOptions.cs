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
            AudioManager.instance.ChangeVolumeMusic(slider.value);
        }

        public void SetSoundVolume(Slider slider) {
            AudioManager.instance.ChangeVolumeSound(slider.value);
        }
    }
}
