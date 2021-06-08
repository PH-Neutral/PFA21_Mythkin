using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshkynCore.UI {
    public class MenuPause : CustomMenu {
        [SerializeField] GameObject panelMainMenu, panelMenuOptions;

        private void Start() {
            Hide();
        }
        protected override void OnShow() {
            // + display the cursor
            gameObject.SetActive(true);
            ShowMainMenu();
        }
        protected override void OnHide() {
            // + hide the cursor
            gameObject.SetActive(false);
            GameManager.Instance.GamePaused = false;
        }
        public void ShowOptionsMenu() {
            panelMenuOptions.SetActive(true);
            HideMainMenu();
        }
        public void HideOptionsMenu() {
            panelMenuOptions.SetActive(false);
        }
        public void ShowMainMenu() {
            panelMainMenu.SetActive(true);
            HideOptionsMenu();
        }
        public void HideMainMenu() {
            panelMainMenu.SetActive(false);
        }
        public void LoadMainMenu() {
            LoadScene(0);
        }
    }
}
