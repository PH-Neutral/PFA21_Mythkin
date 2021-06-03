using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Boar : Enemy
{
    public float Speed
    {
        get { return _agent.speed; }
        set
        {
            _agent.speed = value;
        }
    }

    float SprintSpeed
    {
        get { return moveSpeed * sprintRatio; }
    }
    bool IsCharging
    {
        get { return _isCharging; }
        set
        {
            _isCharging = value;
            _agent.autoBraking = !value;
            Speed = value ? SprintSpeed : moveSpeed;
        }
    }
    [SerializeField] float moveSpeed = 3, sprintRatio = 2, rotationSpeed = 50;
    [SerializeField] float searchLookDuration = 1f;
    [SerializeField] float beforeAttackTime = 1, attackReloadTime = 2.5f, chargeDuration = 2f;
    [SerializeField] Transform _attackCenter;
    [SerializeField] PatrolPath patrolPath = null;
    Vector3 _lastDestination, _searchVectorLeft, _searchVectorRight;
    int _patPathIndex = 0;
    bool _patrolJustStarted = true, _targetPointReached = true, _patrolAscending = true, _searchStart = true, _searchRight = false, _searchLeft = false;
    float _patrolWaitTimer = 0, _beforeAttackTimer = 0, _attackReloadTimer = 0, _searchLookTimer = 0;
    float _agentPathCheckRate = 1 / 30f; // in seconds
    bool _isAlerted = false, _isCollidingWithTarget = false;
    NavMeshAgent _agent;
    bool _isCharging = false;

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponentInChildren<NavMeshAgent>();
    }
    protected override void Start()
    {
        base.Start();
        if(target == null) target = FindObjectOfType<PlayerCharacter>().transform;
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

    protected override void Update()
    {
        // ---- debug ---- //
        if (_agent.angularSpeed != rotationSpeed)
        {
            _agent.angularSpeed = rotationSpeed;
        }
        // --------------- //
        base.Update();
    }
    protected override void OnStateChange()
    {
        base.OnStateChange();
        CancelDestination();
    }
    protected override void OnAggro()
    {
        base.OnAggro();
        _attackReloadTimer = attackReloadTime;
        //_beforeAttackTimer = 0;
    }
    protected override void OnSearch()
    {
        base.OnSearch();
        _searchStart = true;
    }
    protected override void OnPassive()
    {
        base.OnPassive();
        _patrolJustStarted = true;
    }
    protected override void Aggro()
    {
        if (IsCharging)
        {
            if (_targetPointReached)
            {
                IsCharging = false;
                State = EnemyState.Search;
            }
            return;
        }
        if (Look(out Vector3 targetPos))
        {
            // if target is seen
            if (_targetPointReached)
            {
                // if enemy isn't moving (start of Attack mode or destination reached)
                if (_beforeAttackTimer < beforeAttackTime)
                {
                    Speed = 0f;
                    // if during the pre-attack wait time
                    //Debug.Log("Attack: pre-attack");
                    _beforeAttackTimer += Time.deltaTime;
                    SlerpRotation(targetPos - transform.position, rotationSpeed); // turn toward target at defined speed
                    return;
                }
            }
            // if pre-attack wait time is over, sprint toward target and instantly prepare to attack
            if (_attackReloadTimer >= attackReloadTime && !IsCharging)
            {
                IsCharging = true;
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
    protected override void Search()
    {
        if (soundHeard)
        {
            // if a sound was heard while searching for the target, then move toward the source of the sound
            soundHeard = false;
            Vector3 searchPoint = transform.position + lastSoundVector * 0.75f;
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
            State = EnemyState.Passive;
        }
    }
    protected override void Passive()
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
    protected override void OnSoundHeard()
    {
    }
}

