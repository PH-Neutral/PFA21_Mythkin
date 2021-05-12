using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] Transform head;
    [SerializeField] Transform target;
    [SerializeField] float grassVerticalViewAngleMax = 20;
    private void Update() {
        DetectPlayer();
    }
    void DetectPlayer() {
        Vector3 playerPos = target.position;
        Vector3 relativePos = playerPos - head.position;
        bool playerDetected = false;
        int layers = 1 << LayerMask.NameToLayer("Environment") | 1 << LayerMask.NameToLayer("Player");
        if (Physics.Raycast(head.position, relativePos.normalized, out RaycastHit hit, relativePos.magnitude, layers)) {
            if (hit.collider.CompareTag("grass")) {
                // vision is blocked by grass
                float dotProd = Vector3.Dot(relativePos.normalized, Vector3.down);
                if (dotProd > 1 - grassVerticalViewAngleMax / 90) {
                    // vertical enough to see target among the grass
                    playerDetected = true;
                }
            } else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player")) {
                // no environment is blocking the vision
                playerDetected = true;
            }
            //Debug.LogWarning("Raycast hit: " + hit.collider.name);
        }

        Debug.DrawLine(head.position, playerPos, playerDetected ? Color.green : Color.red);
    }
}
