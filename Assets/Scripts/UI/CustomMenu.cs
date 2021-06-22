using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace AshkynCore.UI {
    public class CustomMenu : MonoBehaviour {
        protected static CustomMenu current = null;
        [SerializeField] protected GameObject firstMenuItem;
        [SerializeField] protected CustomMenu returnMenu = null;
        [SerializeField] protected bool showOnStart = false;
        protected Button[] buttons;

        EventSystem sys;
        GameObject lastMenuItem, panel;
        bool isHidden;

        protected virtual void Awake() {
            sys = EventSystem.current;
            panel = transform.GetChild(0).gameObject;
            buttons = GetComponentsInChildren<Button>();
        }

        protected virtual void Start() {
            lastMenuItem = firstMenuItem;
            if(showOnStart) Show();
            else Hide();
        }

        protected virtual void Update() {
            if(returnMenu != null) {
                if(current != null && current == this && Input.GetKeyDown(KeyCode.Escape)) {
                    returnMenu.Show();
                    Hide();
                }
            }
        }

        private void LateUpdate() {
            if(!isHidden) {
                if(IsOnThisMenu(sys.currentSelectedGameObject)) lastMenuItem = sys.currentSelectedGameObject;
            }
        }

        protected virtual void OnShow() {}
        protected virtual void OnHide() {}
        public void Show() {
            isHidden = false;
            panel.SetActive(true);
            OnShow();
            SelectUIObject(lastMenuItem);
            current = this;
        }
        public void Hide() {
            isHidden = true;
            OnHide();
            panel.SetActive(false);
            SelectUIObject(null);
            current = null;
        }
        public void SelectUIObject(GameObject objToSelect) {
            EventSystem.current.SetSelectedGameObject(objToSelect);
        }
        public void ReloadScene(bool showLoadingScreen = false) 
        {
            //Destroy(GameManager.Instance.gameObject);
            LoadScene(SceneManager.GetActiveScene().buildIndex, showLoadingScreen);
        } 
        public void LoadScene(int index)
        {
            SceneManager.LoadScene(index);
        }
        public void LoadScene(int index, bool showLoadingScreen = false) {
            if(showLoadingScreen) {
                AudioManager.instance?.ClearAllSources();
                LoadingData.LoadScene(index);
            } else {
                SceneManager.LoadScene(index);
            }
        }
        public void QuitGame() {
            Application.Quit(0);
        }

        bool IsOnThisMenu(GameObject obj) {
            for(int i = 0; i < buttons.Length; i++) {
                if(buttons[i] == obj) return true;
            }
            return false;
        }
    }
}