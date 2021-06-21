using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScreen : MonoBehaviour {
    [SerializeField] Slider progressBar;
    [SerializeField] float fadeDuration = 1;
    CanvasGroup cGroup;
    AsyncOperation loadingOperation;
    float progress = 0;

    private void Awake() {
        DontDestroyOnLoad(gameObject);
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void Start() {
        cGroup = GetComponent<CanvasGroup>();
        if(progressBar != null) progressBar.interactable = false;
        loadingOperation = SceneManager.LoadSceneAsync(LoadingData.sceneToLoadIndex);
        StartCoroutine(Load());
    }

    IEnumerator Load() {
        while(progress < 1) {
            progress = Mathf.Clamp01(loadingOperation.progress / 0.9f);
            //Debug.Log($"Progress: {(progress * 100).ChangePrecision(1)}%");
            UpdateProgressBar(progress);
        }
        while(!loadingOperation.isDone) {
            yield return null;
        }
        while(cGroup.alpha > 0) {
            cGroup.alpha = Mathf.Lerp(cGroup.alpha, 0, Time.unscaledTime / fadeDuration);
            yield return null;
        }
        GameManager.Instance?.StartGame();
        Destroy(gameObject);
        yield break;
    }
    void UpdateProgressBar(float progress) {
        if(progressBar != null)  progressBar.value = progress;
    }
}