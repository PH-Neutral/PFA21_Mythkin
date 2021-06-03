using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public BombPlant parent;
    [SerializeField] float timeToExplode = 5f, _soundRadius = 15f;
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
            Utils.EmitSound(_soundRadius, transform.position, false);
        }
    }
}
