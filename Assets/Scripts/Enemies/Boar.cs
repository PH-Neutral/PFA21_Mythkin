using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MythkinCore.Audio;

public class Boar : Enemy
{
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
    [SerializeField] float beforeAttackTime = 1, attackReloadTime = 2.5f, chargeDuration = 2f;
    [SerializeField] Transform _attackCenter;
    Vector3 _lastDestination, _searchVectorLeft, _searchVectorRight;
    bool _searchStart = true, _searchRight = false, _searchLeft = false;
    float _beforeAttackTimer = 0, _attackReloadTimer = 0, _searchLookTimer = 0;
    float _agentPathCheckRate = 1 / 30f; // in seconds
    bool _isAlerted = false, _isCollidingWithTarget = false;
    NavMeshAgent _agent;
    bool _isCharging = false;

    Vector3 _suspiciousPos, _attackPos;
    bool _stateStart = false;

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponentInChildren<NavMeshAgent>();
        _agent.enabled = false;
    }
    protected override void Start()
    {
        base.Start();
        _agent.enabled = true;
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

        HandleSound();
    }
    protected override void OnUpdate() {
        if(Look(out Vector3 targetPos)) {
            State = EnemyState.Aggro;
        } else if(State == EnemyState.Passive && soundHeard) {
            State = EnemyState.Search;
        }
    }
    protected override void OnStateChange()
    {
        base.OnStateChange();
        CancelDestination();
        _stateStart = true;
    }
    protected override void OnAggro()
    {
        base.OnAggro();
        AudioManager.instance.PlaySound(AudioTag.boarScream, 1);
    }
    protected override void OnSearch()
    {
        base.OnSearch();
        AudioManager.instance.PlaySound(AudioTag.boarTalk, 1);
    }
    protected override void Aggro()
    {
        bool targetSeen;
        if(targetSeen = Look(out Vector3 targetPos)) {
            _attackPos = targetPos;
        }
        if(!_targetPointReached || _stateStart) {
            if(_stateStart) {
                _stateStart = false;
                Speed = moveSpeed;
            }
            SetDestinationPoint(_attackPos);
        }
        if(_targetPointReached && !targetSeen) {
            _suspiciousPos = _attackPos;
            State = EnemyState.Search;
            return;
        }
    }
    protected override void Search()
    {
        if(_stateStart || soundHeard) {
            _stateStart = false;
            _searchLeft = true;
            _searchRight = true;
            if(soundHeard) {
                soundHeard = false;
                _suspiciousPos = transform.position + lastSoundVector;
            }
            Speed = moveSpeed;
            SetDestinationPoint(_suspiciousPos);
        }
        if(_targetPointReached) {
            if(_searchLeft && _searchRight) {
                _searchLeft = false;
                _searchRight = false;
                _searchVectorLeft = transform.TransformDirection(Quaternion.Euler(0, -60, 0) * Vector3.forward);
                _searchVectorRight = transform.TransformDirection(Quaternion.Euler(0, 60, 0) * Vector3.forward);
            }
            if(!_searchLeft) {
                if(transform.SlerpRotation(_searchVectorLeft, Vector3.up, rotationSpeed)) {
                    _searchLeft = true;
                }
            } else if(!_searchRight) {
                if(transform.SlerpRotation(_searchVectorRight, Vector3.up, rotationSpeed)) {
                    _searchRight = true;
                }
            } else {
                State = EnemyState.Passive;
            }
        }
    }
    protected override void Passive()
    {
        FollowPatrol();
    }

    float walkTimer;
    void HandleSound() {
        if(_agent.velocity != Vector3.zero) {
            float speedRatio = Speed / moveSpeed;
            float stepDelay = 6;
            if(walkTimer >= stepDelay) {
                walkTimer -= stepDelay;
                AudioManager.instance.PlaySound(AudioTag.boarWalk, gameObject, speedRatio);
            }
            walkTimer += Time.deltaTime;
        }
    }
    void CancelDestination()
    {
        CancelInvoke(nameof(CheckIfPointReached));
        if(_agent.enabled) _agent.ResetPath();
        _targetPointReached = true;
    }
    protected override bool SetDestinationPoint(Vector3 destination, bool raycastGround = true)
    {
        if(!raycastGround) base.SetDestinationPoint(destination, raycastGround);

        if(Utils.GetSurfacePoint(destination, out Vector3 surfacePoint))
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
                OnDestinationReached();
                return;
            }
        }
        Invoke(nameof(CheckIfPointReached), _agentPathCheckRate);
    }
    protected override void OnSoundHeard()
    {
    }
    protected override float GetSpeed() {
        return _agent.speed;
    }
    protected override void SetSpeed(float speed) {
        _agent.speed = speed;
    }
}

#region OLD
/*
 * 
    protected override void OnUpdate() {
        if(Look(out Vector3 targetPos)) {
            State = EnemyState.Aggro;
        } else if(soundHeard) {
            if(State == EnemyState.Passive) {
                State = EnemyState.Search;
            }
        }
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
                    transform.SlerpRotation(targetPos - transform.position, transform.up, rotationSpeed, Space.Self); // turn toward target at defined speed
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
            if (transform.SlerpRotation(_searchVectorLeft, transform.up, rotationSpeed * 2, Space.Self))
            {
                // if finished turning, wait a bit before turning the other way
                if (_searchLookTimer < searchDuration)
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
            if (transform.SlerpRotation(_searchVectorRight, transform.up, rotationSpeed * 2, Space.Self))
            {
                // if finished turning, wait a bit before going back to patrolling
                if (_searchLookTimer < searchDuration)
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
        FollowPatrol();
    }
*/
#endregion

