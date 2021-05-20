using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Root : MonoBehaviour
{
    Collider col;
    public float openDuration = 5f;
    private void Awake()
    {
        col = GetComponent<Collider>();
    }
    public void Open()
    {
        col.enabled = false;
        GetComponent<Renderer>().enabled = false;
        Invoke(nameof(Close), openDuration);
    }
    public void Close()
    {
        col.enabled = true;
        GetComponent<Renderer>().enabled = true;
    }
}
