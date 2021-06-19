using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshkynCore.UI {
    public class MenuOptions : CustomMenu {
        [SerializeField] Slider sliderMusic, sliderSound;
        protected override void Awake() {
            base.Awake();
            if(sliderMusic != null) sliderMusic.value = AudioManager.instance.volumeMusic;
            if(sliderSound != null) sliderSound.value = AudioManager.instance.volumeSound;
        }

        public void SetMusicVolume(Slider slider) {
            AudioManager.instance.ChangeVolumeMusic(slider.GetValue01());
        }

        public void SetSoundVolume(Slider slider) {
            AudioManager.instance.ChangeVolumeSound(slider.GetValue01());
        }

        protected override void OnHide() {
            base.OnHide();
            //AudioManager.instance.SaveSettings();
        }
    }
}
