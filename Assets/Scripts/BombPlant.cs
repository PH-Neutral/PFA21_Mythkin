using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombPlant : MonoBehaviour
{
    [SerializeField] float _timeToGrow = 5f;
    bool _gotABomb = false, _canGrow = false;

    void GrowBomb()
    {
        //play anim growBomb
        _gotABomb = true;
        _canGrow = false;
    }
    public void PickBomb()
    {
        //play anim PickBomb
    }
}
