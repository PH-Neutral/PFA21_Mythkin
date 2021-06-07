using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : MonoBehaviour
{
    public EnemyState State
    {
        get
        {
            return _state;
        }
        protected set
        {
            if (value == _state) return;
            Material newMat = GameManager.Instance.matEnemyPatrol;
            switch (value)
            {
                case EnemyState.Passive:
                    newMat = GameManager.Instance.matEnemyPatrol;
                    OnPassive();
                    break;
                case EnemyState.Search:
                    newMat = GameManager.Instance.matEnemySearch;
                    OnSearch();
                    break;
                case EnemyState.Aggro:
                    newMat = GameManager.Instance.matEnemyAttack;
                    OnAggro();
                    break;
            }
            ChangeMaterial(newMat);
            OnStateChange();
            _state = value;
            debugState = value;
        }
    }
    public Transform sightCenter;

    protected float Speed {
        get { return GetSpeed(); }
        set { SetSpeed(value); }
    }
    protected float SprintSpeed {
        get { return moveSpeed * sprintRatio; }
    }
    [SerializeField] protected EnemyState debugState = EnemyState.Passive;
    [SerializeField] protected bool debugLogs, debugDraws;
    [SerializeField] protected float moveSpeed = 3, sprintRatio = 2, rotationSpeed = 50;
    [SerializeField] protected float searchDuration = 1f;
    [Range(0, 100)]
    [SerializeField] protected float hearingLevelMin = 0f;
    [SerializeField] protected float rangeOfSight = 50, fieldOfView = 120, grassVerticalViewAngleMax = 50;
    [SerializeField] protected int floorLevel = 0;
    [SerializeField] protected PatrolPath patrolPath = null;
    [SerializeField] protected Transform head;
    [SerializeField] protected MeshRenderer[] _renderers;
    protected Transform target;
    protected Vector3 destinationPoint, lastDestinationPoint;
    protected Vector3 lastSoundVector;
    protected bool soundHeard, lastSoundIsPlayer;
    protected float lastSoundLevel;
    protected bool _patrolJustStarted = true, _targetPointReached = true, _patrolAscending = true;
    protected float _patrolWaitTimer = 0;
    protected int _patPathIndex = 0;
    protected float _speed;

    protected EnemyState _state = EnemyState.Idle;

    protected virtual void Awake()
    {
        //_renderers = GetComponentsInChildren<MeshRenderer>();
    }
    protected virtual void Start() {
        target = GameManager.Instance.player.head;
        //ChangeMaterial(GameManager.Instance.matEnemyPatrol); //if starts in passive state
        State = debugState;

        // set initial position and orientation forced by patrolPath
        if(patrolPath != null && patrolPath.wayPoints.Length > 0) {
            Utils.GetSurfacePoint(patrolPath.wayPoints[0].point.position, out Vector3 startPos, 1.5f);
            transform.position = startPos;
            if(patrolPath.wayPoints.Length > 1) {
                Utils.GetSurfacePoint(patrolPath.wayPoints[1].point.position, out Vector3 lookPos, 1.5f);
                transform.LookAt(new Vector3(lookPos.x, transform.position.y, lookPos.z), Vector3.up); // look in dir of next point
            }
        }
    }
    protected virtual void Update()
    {
        // ------ debug ------
        if (debugState != State)
        {
            State = debugState;
        }
        // -------------------

        OnUpdate();

        if (State == EnemyState.Passive)
        {
            Passive();
        }
        else if (State == EnemyState.Search)
        {
            Search();
        }
        else if (State == EnemyState.Aggro)
        {
            Aggro();
        }
    }

    public void HearSound(Vector3 soundVector, float soundLevel, bool isPlayer)
    {
        if (soundLevel > hearingLevelMin)
        {
            // if sound is loud enough for the enemy to hear, then raise flag and save sound direction
            lastSoundVector = soundVector;
            lastSoundIsPlayer = isPlayer;
            soundHeard = true;
            if (debugLogs)
            {
                Debug.Log($"The enemy \"{name}\" heard a sound of intensity {soundLevel.ChangePrecision(0)} coming from {soundVector.magnitude.ChangePrecision(2)}m away.");
            }
            OnSoundHeard();
        }
    }
    protected abstract void OnUpdate();
    protected abstract void Passive();
    protected abstract void Search();
    protected abstract void Aggro();
    protected abstract void OnSoundHeard();
    protected virtual void OnStateChange() {}
    protected virtual void OnPassive() {
        _patrolJustStarted = true;
    }
    protected virtual void OnSearch() {}
    protected virtual void OnAggro() { }
    protected virtual float GetSpeed() {
        return _speed;
    }
    protected virtual void SetSpeed(float speed) {
        _speed = speed;
    }
    protected bool Look(out Vector3 targetPos) {
        targetPos = target.position;

        //bool detectionLeft = DetectTarget(eyeLeft, target.gameObject);
        //bool detectionRight = DetectTarget(eyeRight, target.gameObject);
        //return (detectionLeft || detectionRight);
        return DetectTarget(sightCenter, target.gameObject);
    }
    bool DetectTarget(Transform origin, GameObject target) {
        Vector3 playerPos = target.transform.position;
        Vector3 relativePos = playerPos - origin.position;
        Vector3 originFwd = origin.TransformDirection(Vector3.forward);
        // -------------- debug FOV ------------------- //
        if(debugDraws) {
            Vector3 fovLimitLeft = origin.TransformDirection(Quaternion.Euler(0, fieldOfView * -0.5f, 0) * Vector3.forward);
            Vector3 fovLimitRight = origin.TransformDirection(Quaternion.Euler(0, fieldOfView * 0.5f, 0) * Vector3.forward);
            Debug.DrawRay(origin.position, fovLimitLeft * 10, Color.blue); //left
            Debug.DrawRay(origin.position, fovLimitRight * 10, Color.blue); //right
            Debug.DrawRay(origin.position, originFwd * 10, Color.cyan); //forward
        }
        // -------------------------------------------- //
        // check if target is in range of sight and at correct height
        float floorHeightMin = (floorLevel - 1 / 6f) * Utils.floorHeight;
        float floorHeightmax = (floorLevel + 5 / 6f) * Utils.floorHeight;
        if((target.transform.position - origin.position).magnitude > rangeOfSight || !target.transform.position.y.IsBetween(floorHeightMin, false, floorHeightmax, true)) {
            return false;
        }
        bool invalidAngleSight = false;
        bool detected = false;
        // check if angle of sight is valid
        float sightAngle = Vector3.Angle(relativePos.normalized, originFwd.normalized);
        if(sightAngle > fieldOfView * 0.5f) {
            invalidAngleSight = true;
        } else {
            // Check if target is in sight or not
            int layers = Utils.layer_Terrain.ToLayerMask() | Utils.layer_Environment.ToLayerMask() | Utils.layer_Interactibles.ToLayerMask();
            if(Physics.Raycast(origin.position, relativePos.normalized, out RaycastHit hit, relativePos.magnitude, layers)) {
                if(hit.collider.CompareTag("grass")) {
                    // vision is blocked by grass
                    float verticalAngle = Vector3.Angle(relativePos.normalized, Vector3.down);
                    if(verticalAngle < grassVerticalViewAngleMax) {
                        // vertical enough to see target inside the grass
                        detected = true;
                    }
                }
            } else {
                detected = true;
                // nothing is blocking the vision
            }
        }
        if(debugDraws) {
            Debug.DrawLine(origin.position, target.transform.position, detected ? Color.green : (invalidAngleSight ? Color.red : Color.yellow));
        }
        return detected;
    }
    protected void FollowPatrol() {
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
                    _patPathIndex = patrolPath.GetNextIndex(_patPathIndex, ref _patrolAscending);
                } else {
                    _patrolJustStarted = false;
                }

                Speed = patrolPath.wayPoints[_patPathIndex].sprintSpeed ? SprintSpeed : moveSpeed;
                SetDestinationPoint(patrolPath.wayPoints[_patPathIndex].point.position);
            }
        }
    }
    protected void OnDestinationReached() {
        _targetPointReached = true;
    }
    protected virtual bool SetDestinationPoint(Vector3 destination, bool raycastGround = true) {
        lastDestinationPoint = destinationPoint;
        destinationPoint = destination;
        if(raycastGround) Utils.GetSurfacePoint(destination, out destinationPoint, 1.5f);

        if(Vector3.Distance(transform.position, destinationPoint) == 0) _targetPointReached = true;
        else _targetPointReached = false;
        return true;
    }
    protected void ChangeMaterial(Material mat)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].material = mat;
        }
    }
}

[System.Serializable]
public enum EnemyState
{
    Idle = -1, Passive = 0, Search = 1, Aggro = 2
}