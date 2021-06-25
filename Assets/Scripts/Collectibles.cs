using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectibles : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(Utils.l_Player))
        {
            AudioManager.instance.PlaySound(AshkynCore.Audio.AudioTag.collectiblePickUp, true);
            GameManager.Instance.collectiblesCount++;
            Destroy(gameObject);
        }
    }
}
