using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour 
{
    public EnemyState State {
        get {
            return _state;
        }
        set {
            if(value == _state) return;
            Material newMat = GameManager.Instance.matEnemyPatrol;
            switch(value) {
                case EnemyState.Patrol: 
                    newMat = GameManager.Instance.matEnemyPatrol;
                    _patrolJustStarted = true;
                    break;
                case EnemyState.Search: 
                    newMat = GameManager.Instance.matEnemySearch; 
                    break;
                case EnemyState.Attack: 
                    newMat = GameManager.Instance.matEnemyAttack; 
                    break;
            }
            ChangeMaterial(newMat);
            CancelDestination();
            _state = value;
            debugState = value;
        }
    }
    public Transform target;

    [SerializeField] Transform eyeLeft, eyeRight;
    [SerializeField] float fieldOfView = 90;
    [SerializeField] float grassVerticalViewAngleMax = 45;
    [SerializeField] float hearingLevelMin = 20f;
    [SerializeField] bool debugMode = true, explicitDebug = true;
    [SerializeField] EnemyState debugState = EnemyState.Patrol;
    [SerializeField] PatrolPath patrolPath = null;
    EnemyState _state = EnemyState.Idle;
    Vector3 _lastPositionOnPath;
    int _patPathIndex = 0;
    bool _patrolJustStarted = true, _targetPointReached = true, _patrolAscending = true;
    float _patrolWaitTimer = 0;
    float _agentPathCheckRate = 1 / 30f; // in seconds
    bool _isAlerted = false;
    NavMeshAgent _agent;
    MeshRenderer[] _renderers;

    private void Awake() {
        _agent = GetComponentInChildren<NavMeshAgent>();
        _renderers = GetComponentsInChildren<MeshRenderer>();
    }
    private void Start() {
        State = debugState;
    }

    private void Update() {
        // ---- debug ---- //
        if (debugState != State) {
            State = debugState;
        }
        // --------------- //
        if (Look(out Vector3 targetPos)) {
            State = EnemyState.Attack;
        }

        if (State == EnemyState.Patrol) {
            Patrol();
        } else if(State == EnemyState.Search) {
            Search(targetPos);
        } else if(State == EnemyState.Attack) {
            Attack(targetPos);
        }
    }
    public void HearSound(Vector3 soundVector, float soundLevel) {
        // make enemy go into search mode
        if(debugMode) {
            Debug.Log($"The enemy \"{name}\" heard a sound of {soundLevel.ChangePrecision(4)}dB coming from {soundVector.magnitude.ChangePrecision(2)}m away."
                + $"\nIts minimum hearing level is {hearingLevelMin.ChangePrecision(4)}dB so "
                + (soundLevel > hearingLevelMin ? " IT HEARD IT!" : "it didn't hear it..."));
        }
    }
    void Search(Vector3 targetPos) {

    }
    void Attack(Vector3 targetPos) {

    }
    void Patrol() {
        if(patrolPath != null && patrolPath.wayPoints.Length > 0) {
            if(_targetPointReached || _patrolJustStarted) {
                // when the target point has been reached or before starting the patrol
                if(!_patrolJustStarted) {
                    // if not at the begining of the patrol
                    if(patrolPath.wayPoints[_patPathIndex].waitingDuration > 0) {
                        _patrolWaitTimer += Time.deltaTime;
                        if(_patrolWaitTimer < patrolPath.wayPoints[_patPathIndex].waitingDuration) {
                            return;
                        }
                        _patrolWaitTimer = 0;
                    }
                    if(patrolPath.mode == PatrolPath.Mode.BackAndForth) {
                        if(_patPathIndex + 1 == patrolPath.wayPoints.Length) {
                            _patrolAscending = false;
                        } else if(_patPathIndex == 0) {
                            _patrolAscending = true;
                        }
                    }
                    _patPathIndex += (_patrolAscending ? 1 : -1); // next wayPoint index
                    if(_patPathIndex < 0) _patPathIndex = patrolPath.wayPoints.Length - 1;
                    else if(_patPathIndex >= patrolPath.wayPoints.Length) _patPathIndex = 0;
                } else {
                    _patrolJustStarted = false;
                }

                _agent.speed = patrolPath.wayPoints[_patPathIndex].speedToPoint;
                SetDestinationPoint(patrolPath.wayPoints[_patPathIndex].point.position);
            }
        }
    }
    bool Look(out Vector3 targetPos) {
        bool detectionLeft = DetectTarget(eyeLeft, target.gameObject);
        bool detectionRight = DetectTarget(eyeRight, target.gameObject);
        targetPos = target.position;
        return (detectionLeft || detectionRight) && !debugMode;
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
    void CancelDestination() {
        CancelInvoke(nameof(CheckIfPointReached));
        _agent.ResetPath();
    }
    bool SetDestinationPoint(Vector3 destination) {
        if(GetSurfacePoint(destination, out Vector3 surfacePoint)) {
            if((surfacePoint - transform.position).magnitude < 0.05f) {
                _targetPointReached = true;
                return true;
            }
            bool destinationCorrect = _agent.SetDestination(surfacePoint);
            if(destinationCorrect) {
                _targetPointReached = false;
                CheckIfPointReached();
            } else {
                Debug.LogWarning("The destination provided is incorrect. " + surfacePoint);
            }
            return destinationCorrect;
        }
        return false;
    }
    void CheckIfPointReached() {
        CancelInvoke(nameof(CheckIfPointReached));
        // check if arrived at destination
        if(Vector3.Distance(_agent.destination, _agent.transform.position) <= _agent.stoppingDistance) {
            if(!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f) {
                CancelInvoke(nameof(CheckIfPointReached));
                OnPointReached();
                return;
            }
        }
        Invoke(nameof(CheckIfPointReached), _agentPathCheckRate);
    }
    void OnPointReached() {
        _targetPointReached = true;
    }
    bool GetSurfacePoint(Vector3 worldPos, out Vector3 surfacePoint) {
        surfacePoint = Vector3.zero;
        if (Physics.Raycast(worldPos, Vector3.down, out RaycastHit hit, 10, 1 << LayerMask.NameToLayer("Terrain"))) {
            surfacePoint = hit.point;
            return true;
        }
        return false;
    }
    void ChangeMaterial(Material mat) {
        for(int i = 0; i < _renderers.Length; i++) {
            _renderers[i].material = mat;
        }
    }
}

[System.Serializable]
public enum EnemyState {
    Idle, Patrol, Search, Attack
}
