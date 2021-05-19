using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public GameObject rootIndicator, bombIndicator;
    public PlayerCharacter player;
    Camera cam;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        cam = Camera.main;
    }

    private void Update()
    {
        CheckRoots();
    }
    void CheckRoots()
    {
        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 100, 1 << LayerMask.NameToLayer("Interactibles")))
        {
            if (Vector3.Distance(player.transform.position, hit.point) > player.interactionMaxDistance) return;

            if (hit.collider.CompareTag("Roots"))
            {
                player.CanOpenRoot = true;
                return;
            }
            else player.CanOpenRoot = false;
        }
        else player.CanOpenRoot = false;
    }
}
