using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    [TextArea] public string txtStr;
    [SerializeField] float timeUntilSkipable = 0.5f;
    Text txtObj;
    bool isInTutorial = false;
    bool isSkipable = false;
    float timer = 0;

    private void Start()
    {
        txtObj = UIManager.Instance.tutoTxt;
    }

    private void Update()
    {
        if (isInTutorial) timer += Time.unscaledDeltaTime;
        if (timer >= timeUntilSkipable) isSkipable = true;
        if (Input.GetKeyDown(KeyCode.Space) && isInTutorial && isSkipable)
        {
            GameManager.Instance.player.canJump = false;
            txtObj.enabled = isInTutorial = GameManager.Instance.GamePaused = GameManager.Instance.disablePauseToggle = false;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!GameData.showTutorial) return;
        if (other.gameObject.layer == LayerMask.NameToLayer(Utils.l_Player))
        {
            txtObj.enabled = isInTutorial = GameManager.Instance.GamePaused = GameManager.Instance.disablePauseToggle = true;
            txtObj.text = txtStr + "\n\n(Appuyez sur \"Espace\")";
        }
    }
}
