using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AshkynCore.Audio;

public class Root : Interactable {
    const string animNameOpen = "Open"; 
    public float openDuration = 5f;
    public bool isInteractable = true;

    Collider _coll;
    GameObject _model;

    protected override void Awake()
    {
        base.Awake();
        _coll = GetComponentInChildren<Collider>();
        _model = transform.GetChild(0).gameObject;
    }
    public void Open()
    {
        animator.Play(animNameOpen, 0, 0);
        animator.SetFloat("OpenRatio", 1);
        AudioManager.instance.PlaySound(AudioTag.Roots, gameObject);
        isInteractable = false;
        ShowOutline(false);
        _coll.enabled = false;
        // _model.SetActive(false);
        Invoke(nameof(Close), openDuration);
        GameManager.Instance.UpdateNavMesh();
    }
    public void Close()
    {
        animator.Play(animNameOpen, 0, 1);
        animator.SetFloat("OpenRatio", -1);
        CancelInvoke(nameof(Close));
        _coll.enabled = true;
        // _model.SetActive(true);
        isInteractable = true;
        GameManager.Instance.UpdateNavMesh();
    }
}
