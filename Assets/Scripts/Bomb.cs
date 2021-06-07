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
        AudioManager.instance.PlaySound(MythkinCore.Audio.AudioTag.fruitBombImpact, audioGO, 1);
        // can be heard by ennemies
        Utils.EmitSound(_soundRadius, transform.position, false);
        // destroy
        Destroy(gameObject);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(Utils.layer_Terrain) || other.gameObject.layer == LayerMask.NameToLayer(Utils.layer_Interactibles))
        {
            Explode();
        }
    }
}
