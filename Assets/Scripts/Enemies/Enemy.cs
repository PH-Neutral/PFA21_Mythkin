using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour 
{
    public Transform target;

    [SerializeField] Transform eyeLeft, eyeRight;
    [SerializeField] float fieldOfView = 90;
    [SerializeField] float grassVerticalViewAngleMax = 45;
    [SerializeField] float hearingLevelMin = 20f;
    [SerializeField] bool debugMode = true, explicitDebug = true;
    NavMeshAgent _agent;
    float _pathCheckRate = 0.05f; // in seconds
    bool _isAlerted = false;

    private void Awake() {
        _agent = GetComponentInChildren<NavMeshAgent>();
    }

    private void Update() {
        if (Look(out Vector3 targetPos)) {
            Move(targetPos);
        }
    }
    public void HearSound(Vector3 soundVector, float soundLevel) {
        Debug.Log($"The enemy \"{name}\" heard a sound of {soundLevel.ChangePrecision(4)}dB coming from {soundVector.magnitude.ChangePrecision(2)}m away."
            + $"\nIts minimum hearing level is {hearingLevelMin.ChangePrecision(4)}dB so " + (soundLevel > hearingLevelMin ? " IT HEARD IT!" : "it didn't hear it..."));
    }
    bool Look(out Vector3 targetPos) {
        bool detectionLeft = DetectTarget(eyeLeft, target.gameObject);
        bool detectionRight = DetectTarget(eyeRight, target.gameObject);
        targetPos = target.position;
        return (detectionLeft || detectionRight) && !debugMode;
    }
    void Move(Vector3 targetPoint) {
        if(GetSurfacePoint(targetPoint, out Vector3 surfacePoint)) {
            SetDestinationPoint(surfacePoint);
        }
    }
    void OnPathComplete() {

    }
    bool DetectTarget(Transform origin, GameObject target) {
        Vector3 playerPos = target.transform.position;
        Vector3 relativePos = playerPos - origin.position;
        Vector3 originFwd = origin.TransformDirection(Vector3.forward);
        // -------------- debug FOV ------------------- //
        if (debugMode && explicitDebug) {
            Vector3 fovLimitLeft = origin.TransformDirection(Quaternion.Euler(0, fieldOfView * -0.5f, 0) * Vector3.forward);
            Vector3 fovLimitRight = origin.TransformDirection(Quaternion.Euler(0, fieldOfView * 0.5f, 0) * Vector3.forward);
            Debug.DrawRay(origin.position, fovLimitLeft * 10, Color.blue); //left
            Debug.DrawRay(origin.position, fovLimitRight * 10, Color.blue); //right
            Debug.DrawRay(origin.position, originFwd * 10, Color.cyan); //forward
        }
        // -------------------------------------------- //
        bool invalidAngleSight = false;
        bool detected = false;
        // check if angle of sight is valid
        float sightAngle = Vector3.Angle(relativePos.normalized, originFwd.normalized);
        if (sightAngle > fieldOfView * 0.5f) {
            invalidAngleSight = true;
        } else {
            // Check if target is in sight or not
            int layers = 1 << LayerMask.NameToLayer("Terrain") | 1 << LayerMask.NameToLayer("Environment") | 1 << target.layer;
            if(Physics.Raycast(origin.position, relativePos.normalized, out RaycastHit hit, relativePos.magnitude, layers)) {
                if(hit.collider.CompareTag("grass")) {
                    // vision is blocked by grass
                    float verticalAngle = Vector3.Angle(relativePos.normalized, Vector3.down);
                    if(verticalAngle < grassVerticalViewAngleMax) {
                        // vertical enough to see target inside the grass
                        detected = true;
                    }
                } else if(hit.collider.gameObject.layer == target.layer) {
                    // nothing is blocking the vision
                    detected = true;
                }
            }
        }
        if (debugMode) {
            Debug.DrawLine(origin.position, target.transform.position, detected ? Color.green : (invalidAngleSight ? Color.red : Color.yellow));
        }
        return detected;
    }
    bool SetDestinationPoint(Vector3 destination) {
        bool destinationCorrect = _agent.SetDestination(destination);
        if (destinationCorrect) {
            CancelInvoke(nameof(CheckIfPathComplete));
            InvokeRepeating(nameof(CheckIfPathComplete), _pathCheckRate, _pathCheckRate);
        }
        return destinationCorrect;
    }
    void CheckIfPathComplete() {
        // check if arrived at destination
        if(Vector3.Distance(_agent.destination, _agent.transform.position) <= _agent.stoppingDistance) {
            if(!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f) {
                CancelInvoke(nameof(CheckIfPathComplete));
                OnPathComplete();
            }
        }
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

public enum EnemyState {
    Patrol, Search, Attack
}
