using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public EnemyState State
    {
        get
        {
            return _state;
        }
        set
        {
            if (value == _state) return;
            Material newMat = GameManager.Instance.matEnemyPatrol;
            switch (value)
            {
                case EnemyState.Patrol:
                    newMat = GameManager.Instance.matEnemyPatrol;
                    _patrolJustStarted = true;
                    break;
                case EnemyState.Search:
                    newMat = GameManager.Instance.matEnemySearch;
                    _searchStart = true;
                    break;
                case EnemyState.Attack:
                    newMat = GameManager.Instance.matEnemyAttack;
                    _attackReloadTimer = attackReloadTime;
                    //_beforeAttackTimer = 0;
                    break;
            }
            ChangeMaterial(newMat);
            CancelDestination();
            _state = value;
            debugState = value;
        }
    }
    public float Speed
    {
        get { return _agent.speed; }
        set
        {
            _agent.speed = value;
        }
    }
    public Transform target;

    float SprintSpeed
    {
        get { return moveSpeed * sprintRatio; }
    }
    [SerializeField] float moveSpeed = 3, sprintRatio = 2, rotationSpeed = 50;
    [SerializeField] Transform eyeLeft, eyeRight;
    [SerializeField] float rangeOfSight = 50, fieldOfView = 120, grassVerticalViewAngleMax = 50;
    [SerializeField] float searchLookDuration = 1f;
    [SerializeField] float beforeAttackTime = 1, attackReloadTime = 2.5f;
    [Range(0,100)]
    [SerializeField] float hearingLevelMin = 0.5f;
    [SerializeField] Transform _attackCenter;
    [SerializeField] PatrolPath patrolPath = null;
    [SerializeField] EnemyState debugState = EnemyState.Patrol;
    [SerializeField] bool debugLogs = true, debugDraws = true;
    EnemyState _state = EnemyState.Idle;
    Vector3 _lastDestination, _lastSoundVector, _searchVectorLeft, _searchVectorRight;
    bool _soundHeard = false;
    int _patPathIndex = 0;
    bool _patrolJustStarted = true, _targetPointReached = true, _patrolAscending = true, _searchStart = true, _searchRight = false, _searchLeft = false;
    float _patrolWaitTimer = 0, _beforeAttackTimer = 0, _attackReloadTimer = 0, _searchLookTimer = 0;
    float _agentPathCheckRate = 1 / 30f; // in seconds
    bool _isAlerted = false, _isCollidingWithTarget = false;
    NavMeshAgent _agent;
    MeshRenderer[] _renderers;

    private void Awake()
    {
        _agent = GetComponentInChildren<NavMeshAgent>();
        _renderers = GetComponentsInChildren<MeshRenderer>();
    }
    private void Start()
    {
        State = debugState;
        // set initial position and orientation
        if(patrolPath != null && patrolPath.wayPoints.Length > 0) {
            if(GetSurfacePoint(patrolPath.wayPoints[0].point.position, out Vector3 startPos)) {
                transform.position = startPos;
                if(patrolPath.wayPoints.Length > 1) {
                    if(GetSurfacePoint(patrolPath.wayPoints[1].point.position, out Vector3 lookPos)) {
                        transform.LookAt(lookPos, Vector3.up);
                    }
                }
            }
        }
    }

    private void Update()
    {
        // ---- debug ---- //
        if (debugState != State)
        {
            State = debugState;
        }
        if (_agent.angularSpeed != rotationSpeed)
        {
            _agent.angularSpeed = rotationSpeed;
        }
        // --------------- //
        if (Look(out Vector3 targetPos))
        {
            State = EnemyState.Attack;
        }
        else if (_soundHeard)
        {
            if (State == EnemyState.Patrol)
            {
                State = EnemyState.Search;
            }
        }

        if (State == EnemyState.Patrol)
        {
            Patrol();
        }
        else if (State == EnemyState.Search)
        {
            Search();
        }
        else if (State == EnemyState.Attack)
        {
            Attack();
        }
    }
    public void HearSound(Vector3 soundVector, float soundLevel)
    {
        if (debugLogs)
        {
            Debug.Log($"The enemy \"{name}\" encountered a sound of {soundLevel.ChangePrecision(4)}dB coming from {soundVector.magnitude.ChangePrecision(2)}m away."
                + $"\nIts minimum hearing level is {hearingLevelMin.ChangePrecision(4)}dB so "
                + (soundLevel > hearingLevelMin ? " IT HEARD IT!" : "it didn't hear it..."));
        }
        if (soundLevel > hearingLevelMin)
        {
            // if sound is loud enough for the enemy to hear, then raise flag and save sound direction
            _lastSoundVector = soundVector;
            _soundHeard = true;
        }
    }
    void Attack()
    {
        if (Look(out Vector3 targetPos))
        {
            // if target is seen
            if (_targetPointReached)
            {
                // if enemy isn't moving (start of Attack mode or destination reached)
                if (_beforeAttackTimer < beforeAttackTime)
                {
                    // if during the pre-attack wait time
                    //Debug.Log("Attack: pre-attack");
                    _beforeAttackTimer += Time.deltaTime;
                    SlerpRotation(targetPos - transform.position, rotationSpeed); // turn toward target at defined speed
                    return;
                }
            }
            // if pre-attack wait time is over, sprint toward target and instantly prepare to attack
            Speed = SprintSpeed;
            if (_attackReloadTimer >= attackReloadTime)
            {
                //Debug.Log("Attack: set destination");
                SetDestinationPoint(targetPos);
            }
        }
        else
        {
            // if target is not seen anymore
            if (_beforeAttackTimer < beforeAttackTime)
            {
                // if during pre-attack wait time, then switch to Search state
                //Debug.Log("State => Search: pre-attack");
                State = EnemyState.Search;
                return;
            }
            else if (_targetPointReached)
            {
                // if arrived at destination of previously seen target
                //Debug.Log("State => Search: targetPoint reached");
                State = EnemyState.Search;
                _beforeAttackTimer = 0;
                return;
            }
        }
        if (_attackReloadTimer >= attackReloadTime)
        {
            float attackRadius = 2, attackRange = 3;
            if (Physics.SphereCast(new Ray(_attackCenter.position, transform.forward), attackRadius, attackRange, 1<<target.gameObject.layer)) {
                // if attack prepared and target in collision, reset the attack and do the damage
                // TODO: Stop moving for a while
                CancelDestination();
                //_beforeAttackTimer = 0;
                _attackReloadTimer = 0;
                if(debugLogs) Debug.LogWarning($"{name} punched your stupid face! Ya dead.");
            }
            //if (_isCollidingWithTarget) {}
        }
        else
        {
            // prepare the attack
            _attackReloadTimer += Time.deltaTime;
        }
    }
    void Search()
    {
        if (_soundHeard)
        {
            // if a sound was heard while searching for the target, then move toward the source of the sound
            _soundHeard = false;
            Vector3 searchPoint = transform.position + _lastSoundVector * 0.75f;
            Speed = moveSpeed;
            SetDestinationPoint(searchPoint);
            _searchStart = true;
            return;
        }
        if (!_targetPointReached) return; // if still moving
        // if arrived at destination point
        if (_searchStart)
        {
            _searchStart = false;
            _searchLeft = _searchRight = false;
            _searchLookTimer = 0;
            _searchVectorLeft = transform.TransformDirection(Vector3.left);
            _searchVectorRight = transform.TransformDirection(Vector3.right);
        }
        // turn around to try and see the little bugger
        if (!_searchLeft)
        {
            if (SlerpRotation(_searchVectorLeft, rotationSpeed * 2))
            {
                // if finished turning, wait a bit before turning the other way
                if (_searchLookTimer < searchLookDuration)
                {
                    _searchLookTimer += Time.deltaTime;
                    return;
                }
                _searchLookTimer = 0;
                _searchLeft = true;
            }
        }
        else if (!_searchRight)
        {
            if (SlerpRotation(_searchVectorRight, rotationSpeed * 2))
            {
                // if finished turning, wait a bit before going back to patrolling
                if (_searchLookTimer < searchLookDuration)
                {
                    _searchLookTimer += Time.deltaTime;
                    return;
                }
                _searchRight = true;
            }
        }
        else
        {
            // if looked left and right and still no one in sight, then go back to patrolling
            State = EnemyState.Patrol;
        }
    }
    void Patrol()
    {
        if (patrolPath != null && patrolPath.wayPoints.Length > 0)
        {
            if (_targetPointReached || _patrolJustStarted)
            {
                // when the target point has been reached or before starting the patrol
                if (!_patrolJustStarted)
                {
                    // if not at the begining of the patrol
                    if (patrolPath.wayPoints[_patPathIndex].waitingDuration > 0)
                    {
                        _patrolWaitTimer += Time.deltaTime;
                        if (_patrolWaitTimer < patrolPath.wayPoints[_patPathIndex].waitingDuration)
                        {
                            return;
                        }
                        _patrolWaitTimer = 0;
                    }
                    _patPathIndex = patrolPath.GetNextIndex(_patPathIndex, ref _patrolAscending);
                }
                else
                {
                    _patrolJustStarted = false;
                }

                _agent.speed = patrolPath.wayPoints[_patPathIndex].speedToPoint;
                SetDestinationPoint(patrolPath.wayPoints[_patPathIndex].point.position);
            }
        }
    }
    bool Look(out Vector3 targetPos)
    {
        bool detectionLeft = DetectTarget(eyeLeft, target.gameObject);
        bool detectionRight = DetectTarget(eyeRight, target.gameObject);
        targetPos = target.position;
        return (detectionLeft || detectionRight);
    }
    bool DetectTarget(Transform origin, GameObject target)
    {
        Vector3 playerPos = target.transform.position;
        Vector3 relativePos = playerPos - origin.position;
        Vector3 originFwd = origin.TransformDirection(Vector3.forward);
        // -------------- debug FOV ------------------- //
        if (debugDraws)
        {
            Vector3 fovLimitLeft = origin.TransformDirection(Quaternion.Euler(0, fieldOfView * -0.5f, 0) * Vector3.forward);
            Vector3 fovLimitRight = origin.TransformDirection(Quaternion.Euler(0, fieldOfView * 0.5f, 0) * Vector3.forward);
            Debug.DrawRay(origin.position, fovLimitLeft * 10, Color.blue); //left
            Debug.DrawRay(origin.position, fovLimitRight * 10, Color.blue); //right
            Debug.DrawRay(origin.position, originFwd * 10, Color.cyan); //forward
        }
        // -------------------------------------------- //
        // check if target is in range of sight
        if ((target.transform.position - origin.position).magnitude > rangeOfSight)
        {
            return false;
        }
        bool invalidAngleSight = false;
        bool detected = false;
        // check if angle of sight is valid
        float sightAngle = Vector3.Angle(relativePos.normalized, originFwd.normalized);
        if (sightAngle > fieldOfView * 0.5f)
        {
            invalidAngleSight = true;
        }
        else
        {
            // Check if target is in sight or not
            int layers = 1 << LayerMask.NameToLayer("Terrain") | 1 << LayerMask.NameToLayer("Environment") | 1 << target.layer;
            if (Physics.Raycast(origin.position, relativePos.normalized, out RaycastHit hit, relativePos.magnitude, layers))
            {
                if (hit.collider.CompareTag("grass"))
                {
                    // vision is blocked by grass
                    float verticalAngle = Vector3.Angle(relativePos.normalized, Vector3.down);
                    if (verticalAngle < grassVerticalViewAngleMax)
                    {
                        // vertical enough to see target inside the grass
                        detected = true;
                    }
                }
                else if (hit.collider.gameObject.layer == target.layer)
                {
                    // nothing is blocking the vision
                    detected = true;
                }
            }
        }
        if (debugDraws)
        {
            Debug.DrawLine(origin.position, target.transform.position, detected ? Color.green : (invalidAngleSight ? Color.red : Color.yellow));
        }
        return detected;
    }
    void CancelDestination()
    {
        CancelInvoke(nameof(CheckIfPointReached));
        _agent.ResetPath();
        _targetPointReached = true;
    }
    bool SetDestinationPoint(Vector3 destination)
    {
        if (GetSurfacePoint(destination, out Vector3 surfacePoint))
        {
            if ((surfacePoint - transform.position).magnitude < 0.05f)
            {
                _targetPointReached = true;
                _lastDestination = destination;
                return true;
            }
            bool destinationCorrect = _agent.SetDestination(surfacePoint);
            if (destinationCorrect)
            {
                _targetPointReached = false;
                _lastDestination = destination;
                CheckIfPointReached();
            }
            else
            {
                Debug.LogWarning("The destination provided is incorrect. " + surfacePoint);
            }
            return destinationCorrect;
        }
        return false;
    }
    void CheckIfPointReached()
    {
        CancelInvoke(nameof(CheckIfPointReached));
        // check if arrived at destination
        if (Vector3.Distance(_agent.destination, _agent.transform.position) <= _agent.stoppingDistance)
        {
            if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
            {
                CancelInvoke(nameof(CheckIfPointReached));
                OnPointReached();
                return;
            }
        }
        Invoke(nameof(CheckIfPointReached), _agentPathCheckRate);
    }
    void OnPointReached()
    {
        _targetPointReached = true;
    }
    bool GetSurfacePoint(Vector3 worldPos, out Vector3 surfacePoint)
    {
        surfacePoint = worldPos;
        if (Physics.Raycast(worldPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 10, 1 << LayerMask.NameToLayer("Terrain")))
        {
            surfacePoint = hit.point;
            Vector3 raycast = hit.point - worldPos;
            if (raycast.magnitude > Utils.floorHeight)
            {
                return false;
            }
            return true;
        }
        return false;
    }
    void ChangeMaterial(Material mat)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].material = mat;
        }
    }
    bool SlerpRotation(Vector3 newDirection, float rotateSpeed)
    {
        float vectorAngle = Vector3.Angle(transform.TransformDirection(Vector3.forward), newDirection);
        float t = rotateSpeed * Time.deltaTime / vectorAngle;
        Quaternion newRotation = Quaternion.LookRotation(newDirection, transform.TransformDirection(Vector3.up));
        transform.localRotation = Quaternion.Slerp(transform.localRotation, newRotation, t);
        return t >= 1;
    }
    /*
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Enemy STARTED collided with " + other.name);
        if (other.gameObject.layer == target.gameObject.layer && other.CompareTag("HitBox"))
        {
            _isCollidingWithTarget = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.Log("Enemy STOPPED collided with " + other.name);
        if (other.gameObject.layer == target.gameObject.layer && other.CompareTag("HitBox"))
        {
            _isCollidingWithTarget = false;
        }
    }*/
}

[System.Serializable]
public enum EnemyState
{
    Idle, Patrol, Search, SearchHeard, Attack
}
