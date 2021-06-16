using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombPlant : MonoBehaviour
{
    [SerializeField] float _timeToGrow = 5f;
    [SerializeField] MeshRenderer _fakeBomb;
    public bool _gotABomb = true;

    public void GrowBomb()
    {
        //play anim growBomb
        Invoke(nameof(FinishGrow), _timeToGrow);
    }
    void FinishGrow()
    {
        _gotABomb = true;
        _fakeBomb.enabled = true;
    }
    public void PickBomb()
    {
        //play anim PickBomb
        _fakeBomb.enabled = false;
    }
}
