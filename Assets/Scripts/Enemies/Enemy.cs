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
                    UIManager.Instance.invisibleTxt.enabled = false;
                    GameManager.Instance.isInvisible = false;
                    OnAggro();
                    break;
            }
            ChangeMaterial(newMat, 1);
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
    [SerializeField] public float minHeightDetection = 0, maxHeightDetection = 6;
    [SerializeField] protected PatrolPath patrolPath = null;
    [SerializeField] protected Transform head;
    protected Transform target;
    protected Vector3 destinationPoint, lastDestinationPoint;
    protected Vector3 lastSoundVector;
    protected bool soundHeard, lastSoundIsPlayer;
    protected float lastSoundLevel;
    protected bool destinationReached = true;

    [SerializeField] Renderer[] _renderers;
    EnemyState _state = EnemyState.Idle;
    bool _patrolJustStarted = true, _patrolAscending = true;
    float _speed, _patrolWaitTimer = 0;
    int _patPathIndex = 0;
    Vector3 _dummyV3;

    protected virtual void Awake()
    {
        //_renderers = GetComponentsInChildren<MeshRenderer>();
    }
    protected virtual void Start() {
        target = GameManager.Instance.player?.head;
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
        if(GameManager.Instance.GamePaused) return;
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

    public virtual void HitPlayer(Vector3 direction) {
        if(debugLogs) Debug.LogWarning($"Player was hit by {name}!");
        GameManager.Instance.player.Die();
    }
    public void HearSound(Vector3 soundVector, float soundLevel, bool isPlayer)
    {
        //Debug.LogWarning($"{name} heard the fucking sound!!!");
        if (soundLevel > hearingLevelMin)
        {
            // if sound is loud enough for the enemy to hear, then raise flag and save sound direction
            lastSoundVector = soundVector;
            lastSoundIsPlayer = isPlayer;
            soundHeard = true;
            if (debugLogs)
            {
                //Debug.LogWarning($"The enemy \"{name}\" heard a sound of intensity {soundLevel.ChangePrecision(0)} coming from {soundVector.magnitude.ChangePrecision(2)}m away.");
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
    protected bool Look() => Look(out _dummyV3);
    protected bool Look(out Vector3 targetPos) {
        targetPos = Vector3.zero;
        if(target == null) return false;
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
        //float floorHeightMin = (floorLevel - 1 / 6f) * Utils.floorHeight;
        //float floorHeightmax = (floorLevel + 5 / 6f) * Utils.floorHeight;
        if((target.transform.position - origin.position).magnitude > rangeOfSight || !target.transform.position.y.IsBetween(minHeightDetection, false, maxHeightDetection, true)) {
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
            int layers = Utils.l_Terrain.ToLayerMask() | Utils.l_Environment.ToLayerMask() | Utils.l_Interactibles.ToLayerMask() | Utils.l_Enemies.ToLayerMask();
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
            if(destinationReached || _patrolJustStarted) {
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
    protected virtual void OnDestinationReached() {
        destinationReached = true;
    }
    protected virtual bool SetDestinationPoint(Vector3 destination, bool raycastGround = true) {
        lastDestinationPoint = destinationPoint;
        destinationPoint = destination;
        if(raycastGround) Utils.GetSurfacePoint(destination, out destinationPoint, 1.5f);

        if(Vector3.Distance(transform.position, destinationPoint) == 0) destinationReached = true;
        else destinationReached = false;
        return true;
    }
    protected void ChangeMaterial(Material mat, int index)
    {
        Material[] mats;
        for (int i = 0; i < _renderers.Length; i++) {
            mats = _renderers[i].sharedMaterials;
            if (index >= mats.Length) continue;
            mats[index] = mat;
            _renderers[i].sharedMaterials = mats;
        }
    }
}

[System.Serializable]
public enum EnemyState
{
    Idle = -1, Passive = 0, Search = 1, Aggro = 2
}