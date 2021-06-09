using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshkynCore.UI {
    public class MenuPause : CustomMenu {
        protected override void OnShow() {
            // + display the cursor
        }
        protected override void OnHide() {
            // + hide the cursor
            GameManager.Instance.GamePaused = false;
        }
        public void LoadMainMenu() {
            LoadScene(0);
        }
    }
}
