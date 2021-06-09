using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace AshkynCore.UI {
    public abstract class CustomMenu : MonoBehaviour {
        [SerializeField] protected GameObject firstMenuItem;

        protected abstract void OnShow();
        protected abstract void OnHide();
        public void Show() {
            OnShow();
            SelectUIObject(firstMenuItem);
        }
        public void Hide() {
            OnHide();
            SelectUIObject(null);
        }
        public void SelectUIObject(GameObject objToSelect) {
            EventSystem.current.SetSelectedGameObject(objToSelect);
        }
        public void ReloadScene() => LoadScene(SceneManager.GetActiveScene().buildIndex);
        public void LoadScene(int index) {
            SceneManager.LoadScene(index);
        }
        public void QuitGame() {
            Application.Quit(0);
        }
    }
}