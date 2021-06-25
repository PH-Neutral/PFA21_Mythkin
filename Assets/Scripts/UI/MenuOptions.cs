using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshkynCore.UI {
    public class MenuOptions : CustomMenu {
        [SerializeField] Slider sliderMusic, sliderSound;
        bool onStart = true;
        protected override void Start()
        {
            base.Start();
            if(sliderMusic != null) sliderMusic.SetValue01(AudioManager.instance.volumeMusic);
            if (sliderSound != null) sliderSound.SetValue01(AudioManager.instance.volumeSound);
            onStart = false;
        }

        public void SetMusicVolume(Slider slider) {
            AudioManager.instance.ChangeVolumeMusic(slider.GetValue01());
            if(!onStart) AudioManager.instance.PlaySound(Audio.AudioTag.uiClicClicMusic);
        }

        public void SetSoundVolume(Slider slider) {
            AudioManager.instance.ChangeVolumeSound(slider.GetValue01());
            if(!onStart) AudioManager.instance.PlaySound(Audio.AudioTag.uiClicClicSound);
        }

        protected override void OnHide() {
            base.OnHide();
            //AudioManager.instance.SaveSettings();
        }
    }
}
