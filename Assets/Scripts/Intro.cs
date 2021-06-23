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

    public Image currentImage;
    public Text currentText;

    int i = 0;
    float timer = 0f;

    private void Start()
    {
        ShowSlide();
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer >= slides[i - 1].duration)
        {
            if (slides.Length > i)
            {
                timer = 0;
                ShowSlide();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (slides.Length > i)
            {
                timer = 0;
                CancelInvoke();
                ShowSlide();
            }
        }
    }

    void ShowSlide()
    {
        currentImage.sprite = slides[i].sprite;
        currentText.text = slides[i].text;


        i++;
    }
}
