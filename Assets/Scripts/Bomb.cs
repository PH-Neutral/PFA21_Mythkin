using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] float timeToExplode = 5f;
    private void Start()
    {
        Invoke(nameof(Explode), timeToExplode);
    }
    void Explode()
    {
        Destroy(gameObject);
        // play explosion anim
        // play explosion sound
        // can be heard by ennemies
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(Utils.layer_Terrain) || other.gameObject.layer == LayerMask.NameToLayer(Utils.layer_Interactibles))
        {
            Explode();
        }
    }
}
