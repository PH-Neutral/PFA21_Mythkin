using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AshkynCore.Audio;

public class Root : Interactable {
    public float openDuration = 5f;

    Collider _coll;
    GameObject _model;

    private void Awake()
    {
        _coll = GetComponentInChildren<Collider>();
        _model = transform.GetChild(0).gameObject;
    }
    public void Open()
    {
        AudioManager.instance.PlaySound(AudioTag.Roots, gameObject);
        _coll.enabled = false;
        _model.SetActive(false);
        Invoke(nameof(Close), openDuration);
        GameManager.Instance.UpdateNavMesh();
    }
    public void Close()
    {
        CancelInvoke(nameof(Close));
        _coll.enabled = true;
        _model.SetActive(true);
        GameManager.Instance.UpdateNavMesh();
    }
}
