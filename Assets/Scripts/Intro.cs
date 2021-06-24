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
        public float duration;
        public string text;
    }
    public Slide[] slides = new Slide[0];


    public bool hasFinished;
    public Image currentImage;
    public Text currentText;

    int i = 0;
    float timer = 0f;
    bool hasStarted = false;


    public void StartIntro()
    {
        currentImage.color = Color.white;
        gameObject.SetActive(true);
        i = 0;
        timer = 0f;
        hasStarted = true;
        hasFinished = false;
        ShowSlide();
    }

    private void Update()
    {
        if (!hasStarted) return;

        timer += Time.unscaledDeltaTime;

        if (timer >= slides[i - 1].duration || Input.GetKeyDown(KeyCode.Space))
        {
            if (i < slides.Length)
            {
                timer = 0f;
                ShowSlide();
            }
            else
            {
                hasFinished = true;
            }
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void ShowSlide()
    {
        currentImage.sprite = slides[i].sprite;
        currentText.text = slides[i].text;


        i++;
    }
}
