using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Root : MonoBehaviour {
    public float openDuration = 5f;

    Collider _coll;
    Renderer _rend;
    private void Awake()
    {
        _coll = GetComponent<Collider>();
        _rend = GetComponent<Renderer>();
    }
    public void Open()
    {
        _coll.enabled = _rend.enabled = false;
        Invoke(nameof(Close), openDuration);
        GameManager.Instance.UpdateNavMesh();
    }
    public void Close()
    {
        CancelInvoke(nameof(Close));
        _coll.enabled = _rend.enabled = true;
        GameManager.Instance.UpdateNavMesh();
    }
}
