using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer ==LayerMask.NameToLayer(Utils.l_Player))
        {
            Debug.Log("yay");
            GameManager.Instance.Win();
        }
    }
}
