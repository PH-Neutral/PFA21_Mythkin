using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    public string txtStr;
    Text txtObj;
    bool isInTutorial = false;

    private void Start()
    {
        txtObj = UIManager.Instance.tutoTxt;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && isInTutorial)
        {
            txtObj.enabled = isInTutorial = GameManager.Instance.GamePaused = GameManager.Instance.isInTutorial = false;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(Utils.l_Player))
        {
            txtObj.enabled = isInTutorial = GameManager.Instance.GamePaused = GameManager.Instance.isInTutorial = true;
            txtObj.text = txtStr;
        }
    }
}
