using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Intro : MonoBehaviour
{
    [System.Serializable]
    public struct Slide
    {
        public Sprite sprite;
        public Color color;
        public string text;
    }
    public Slide[] slides = new Slide[0];


    public bool hasFinished;

    bool CanBeSkipped {
        get { return skipTimer >= timeUntilSkipable; }
    }
    [SerializeField] Image currentImage;
    [SerializeField] Text currentText;
    [SerializeField] float timeUntilSkipable = 0.5f, fadeDuration = 1;
    CanvasGroup cGroup;
    int i = 0;
    bool hasStarted = false;
    float skipTimer = 0;

    private void Awake() {
        cGroup = GetComponentInChildren<CanvasGroup>();
        if(GameData.showIntro) ShowFirstSlide();
        else Hide();
    }
    void ShowFirstSlide() {
        Show();
        i = 0;
        ShowSlide();
    }
    public void StartIntro() {
        ShowFirstSlide();
        hasStarted = true;
        hasFinished = false;
    }

    private void Update()
    {
        if (!hasStarted) return;

        skipTimer = Mathf.Clamp(skipTimer + Time.unscaledDeltaTime, 0, timeUntilSkipable);
        if (CanBeSkipped && Input.GetKeyDown(KeyCode.Space))
        {
            if (i < slides.Length)
            {
                ShowSlide();
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }
    }

    public void Hide()
    {
        Show(false);
    }
    public void Show(bool show = true) {
        cGroup.alpha = show ? 1 : 0;
        cGroup.blocksRaycasts = show;
    }

    void ShowSlide() {
        currentImage.color = slides[i].color;
        currentImage.sprite = slides[i].sprite;
        currentText.text = slides[i].text;

        skipTimer = 0;
        i++;
    }
    IEnumerator FadeOut() {
        while(cGroup.alpha > 0) {
            cGroup.alpha = Mathf.Lerp(cGroup.alpha, 0, Time.unscaledDeltaTime / (cGroup.alpha * fadeDuration));
            yield return null;
        }
        Hide();
        hasFinished = true;
        yield break;
    }
}
