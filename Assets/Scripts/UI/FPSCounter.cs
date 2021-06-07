using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] int precisionInFrames = 20;
    int frameIndex = 0;
    Text txt;
    float fps;
    float[] deltaTimeArray;
    private void Awake()
    {
        txt = GetComponentInChildren<Text>();
        deltaTimeArray = new float[precisionInFrames];
    }
    private void Start()
    {
        for (int i = 0; i < deltaTimeArray.Length; i++)
        {
            deltaTimeArray[i] = Time.deltaTime;
        }
    }
    void Update()
    {
        if (frameIndex < precisionInFrames)
        {
            deltaTimeArray[frameIndex] = Time.deltaTime;
            frameIndex++;
        }
        else
        {
            frameIndex = 0;
        }
        fps = precisionInFrames / deltaTimeArray.Sum();
        txt.text = fps.ChangePrecision(0).ToString();
    }
}
