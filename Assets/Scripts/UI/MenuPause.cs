using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace MythkinCore.UI {
    public class MenuPause : MonoBehaviour {
        [SerializeField] GameObject panelMainMenu, panelMenuOptions;
        [SerializeField] GameObject firstMenuItem;

        private void Start() {
            Hide();
        }

        public void SelectUIObject(GameObject objToSelect) {
            EventSystem.current.SetSelectedGameObject(objToSelect);
        }
        public void Show() {
            // + display the cursor
            gameObject.SetActive(true);
            ShowMainMenu();
            SelectUIObject(firstMenuItem);
        }
        public void Hide() {
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
        public void ReloadScene() {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        public void QuitGame() {
            Application.Quit(0);
        }
    }
}
