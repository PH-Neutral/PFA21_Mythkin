using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshkynCore.UI {
    public class MainMenu : CustomMenu {
        [SerializeField] GameObject panelMainMenu, panelMenuOptions;

        protected override void OnHide() {
            panelMainMenu.SetActive(false);
        }
        protected override void OnShow() {
            panelMainMenu.SetActive(true);
        }
        public void LoadGame() {
            LoadScene(1);
        }
    }
}