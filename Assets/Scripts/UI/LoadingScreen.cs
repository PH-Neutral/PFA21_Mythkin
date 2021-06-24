using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScreen : MonoBehaviour {
    [SerializeField] float offsetDelay = 2.5f;
    [SerializeField] Slider progressBar;
    [SerializeField] float fadeDuration = 1;
    [SerializeField] Transform circle;
    [Tooltip("In turn per second.")]
    [SerializeField] float rotateSpeed = 1;
    CanvasGroup cGroup;
    AsyncOperation loadingOperation;
    float progress = 0, offsetTimer = 0;

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
    private void Update() {
        if(circle != null) circle.Rotate(Vector3.forward, rotateSpeed * 360 / Time.unscaledTime);
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
        offsetTimer = 0;
        while(offsetTimer < offsetDelay) {
            offsetTimer += Time.unscaledDeltaTime;
            yield return null;
        }
        while(cGroup.alpha > 0) {
            cGroup.alpha = Mathf.Lerp(cGroup.alpha, 0, Time.unscaledDeltaTime / fadeDuration);
            yield return null;
        }
        GameManager.Instance?.StartScene();
        Destroy(gameObject);
        yield break;
    }
    void UpdateProgressBar(float progress) {
        if(progressBar != null)  progressBar.value = progress;
    }
}