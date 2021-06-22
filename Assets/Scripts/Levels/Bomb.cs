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
        // play explosion anim
        // play explosion sound
        GameObject audioGO = new GameObject();
        audioGO.transform.position = transform.position;
        AudioManager.instance.PlaySound(AshkynCore.Audio.AudioTag.fruitBombImpact, audioGO, true, 1); // audio source should be destroyed when finished playing
        // can be heard by ennemies
        Utils.EmitSound(_soundRadius, transform.position, false);

        Destroy(gameObject); // destroy the bomb on impact
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(Utils.l_Terrain) || other.gameObject.layer == LayerMask.NameToLayer(Utils.l_Interactibles))
        {
            Explode();
        }
    }
}
