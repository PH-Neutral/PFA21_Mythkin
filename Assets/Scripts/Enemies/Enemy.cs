using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] Transform eyeLeft, eyeRight;
    [SerializeField] float fieldOfView = 90;
    [SerializeField] Transform target;
    [SerializeField] float grassVerticalViewAngleMax = 45;
    [SerializeField] bool canMove = true;
    NavMeshAgent _agent;
    float _pathCheckRate = 0.05f; // in seconds

    private void Awake() {
        _agent = GetComponentInChildren<NavMeshAgent>();
    }

    private void Update() {
        Character player = GameManager.Instance.player;
        GameObject target = player.model.gameObject;

        bool detectionLeft = DetectTarget(eyeLeft, target.gameObject);
        bool detectionRight = DetectTarget(eyeRight, target.gameObject);

        if ((detectionLeft || detectionRight) && canMove) {
            if (GetSurfacePoint(target.transform.position, out Vector3 surfacePoint)) {
                SetDestinationPoint(surfacePoint);
            }
        }
    }

    bool DetectTarget(Transform origin, GameObject target) {
        Vector3 playerPos = target.transform.position;
        Vector3 relativePos = playerPos - origin.position;
        Vector3 originFwd = origin.TransformDirection(Vector3.forward);
        // debug FOV
        Vector3 fovLimitLeft = origin.TransformDirection(Quaternion.Euler(0, fieldOfView * -0.5f, 0) * Vector3.forward);
        Vector3 fovLimitRight = origin.TransformDirection(Quaternion.Euler(0, fieldOfView * 0.5f, 0) * Vector3.forward);
        Debug.DrawRay(origin.position, fovLimitLeft * 10, Color.blue); //left
        Debug.DrawRay(origin.position, fovLimitRight * 10, Color.blue); //right
        Debug.DrawRay(origin.position, originFwd * 10, Color.cyan); //forward
        // method
        bool invalidAngleSight = false;
        bool detected = false;
        // check if angle of sight is valid
        float sightAngle = Vector3.Angle(relativePos.normalized, originFwd.normalized);
        //Debug.Log(sightAngle);
        if (sightAngle > fieldOfView * 0.5f) {
            invalidAngleSight = true;
        } else {
            // Check if target is in sight or not
            int layers = 1 << LayerMask.NameToLayer("Terrain") | 1 << LayerMask.NameToLayer("Environment") | 1 << target.layer;
            if(Physics.Raycast(origin.position, relativePos.normalized, out RaycastHit hit, relativePos.magnitude, layers)) {
                if(hit.collider.CompareTag("grass")) {
                    // vision is blocked by grass
                    float verticalAngle = Vector3.Angle(relativePos.normalized, Vector3.down);
                    //Debug.Log($"verticalAngle: {verticalAngle}");
                    if(verticalAngle < grassVerticalViewAngleMax) {
                        // vertical enough to see target among the grass
                        detected = true;
                    }
                } else if(hit.collider.gameObject.layer == target.layer) {
                    // no environment is blocking the vision
                    detected = true;
                }
                //Debug.LogWarning("Raycast hit: " + hit.collider.name);
            }
        }

        Debug.DrawLine(origin.position, target.transform.position, detected ? Color.green : (invalidAngleSight ? Color.red : Color.yellow));

        return detected;
    }
    bool SetDestinationPoint(Vector3 destination) {
        bool destinationCorrect = _agent.SetDestination(destination);
        if (destinationCorrect) {
            CancelInvoke(nameof(CheckIfArrivedAtDestination));
            InvokeRepeating(nameof(CheckIfArrivedAtDestination), _pathCheckRate, _pathCheckRate);
        }
        return destinationCorrect;
    }
    void CheckIfArrivedAtDestination() {
        // check if arrived at destination
    }
    bool GetSurfacePoint(Vector3 worldPos, out Vector3 surfacePoint) {
        surfacePoint = Vector3.zero;
        if (Physics.Raycast(worldPos, Vector3.down, out RaycastHit hit, 10, 1 << LayerMask.NameToLayer("Terrain"))) {
            surfacePoint = hit.point;
            return true;
        }
        return false;
    }
}
