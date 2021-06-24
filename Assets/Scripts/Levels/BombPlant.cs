using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombPlant : Interactable
{
    [SerializeField] float _timeToGrow = 5f;
    [SerializeField] MeshRenderer _fakeBomb;
    public bool gotABomb = true;

    public void GrowBomb()
    {
        //play anim growBomb
        Invoke(nameof(FinishGrow), _timeToGrow);
    }
    void FinishGrow()
    {
        gotABomb = true;
        _fakeBomb.enabled = true;
        UpdateOutline();
    }
    public void PickBomb()
    {
        //play anim PickBomb
        gotABomb = false;
        _fakeBomb.enabled = false;
        UpdateOutline();
    }
}
