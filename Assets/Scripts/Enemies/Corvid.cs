using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Corvid : Enemy {
    [SerializeField] float _afterAttackDelay = 3, _afterAttackUpAngle = 30, _knockbackStrength = 25;
    Vector3 _suspiciousPos, _attackPos, _afterAttackPos;
    bool _trajectoryPrepared, _attackDone;
    float _searchTimer, _afterAttackTimer;

    protected override void Update() {
        base.Update();
    }
    protected override void OnUpdate() {
        if(Look(out Vector3 targetPos)) {
            // if player seen
            State = EnemyState.Aggro;
        }
    }
    protected override void OnStateChange() {
        base.OnStateChange();
    }
    protected override void OnPassive() {
        base.OnPassive();
        Speed = moveSpeed;
    }
    protected override void OnSearch() {
        base.OnSearch();
        _searchTimer = 0;
    }
    protected override void OnAggro() {
        base.OnAggro();
        Speed = SprintSpeed;
        _trajectoryPrepared = false;
    }
    protected override void OnSoundHeard() {
        //throw new System.NotImplementedException();
    }
    protected override void Passive() {
        FollowPatrol();
        // move to destination
        if(!_targetPointReached) {
            if(Move(destinationPoint, Speed)) {
                OnDestinationReached();
            } else {
                Vector3 xzPos = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 xzTarget = new Vector3(destinationPoint.x, 0, destinationPoint.z);
                if(Vector3.Distance(xzPos, xzTarget) > 0.0001f) Turn(xzTarget - xzPos, Vector3.up, rotationSpeed);
            }
            //Debug.Log(Vector3.Distance(transform.position, destinationPoint));
        }
    }
    protected override void Search() {
        // lookat sus pos for some time
        Turn((_suspiciousPos - transform.position).Flatten(), Vector3.up, rotationSpeed);
        if(_searchTimer >= searchDuration) State = EnemyState.Passive;
        else _searchTimer += Time.deltaTime;
    }
    protected override void Aggro() {
        bool targetSeen;
        if(targetSeen = Look(out Vector3 targetPos)) {
            _attackPos = targetPos;
        }
        // prepare trajectory ?
        if(Turn((_attackPos - transform.position).Flatten(), Vector3.up, rotationSpeed)) {
            if(!_trajectoryPrepared) {
                // when facing player before charging, reset values
                _trajectoryPrepared = true;
                _attackDone = false;
                _afterAttackTimer = _afterAttackDelay;
                if(!targetSeen) {
                    // if target is not seen, switch to search
                    _suspiciousPos = _attackPos;
                    State = EnemyState.Search;
                    return;
                }
            }
        }
        if(_trajectoryPrepared) {
            // after facing and having seen player for at least one frame, charge player then continue moving a bit
            if(!_attackDone) {
                Vector3 lastPos = transform.position;
                bool finishedMoving = Move(_attackPos, Speed);
                bool touchedPlayer = CheckForTouch(lastPos, transform.position);
                if(finishedMoving || touchedPlayer) {
                    if(touchedPlayer) {
                        PushPlayer(transform.position - lastPos);
                    }
                    _attackDone = true;
                    _afterAttackPos = transform.position + GetDirectionUpped(transform.position - lastPos, _afterAttackUpAngle) * 10;
                }
            } else {
                _afterAttackTimer -= Time.deltaTime;
                Turn((_afterAttackPos - transform.position).Flatten(), Vector3.up, rotationSpeed);
                if(Move(_afterAttackPos, SprintSpeed) || _afterAttackTimer <= 0) {
                    _trajectoryPrepared = false;
                }
            }
        }
    }
    void PushPlayer(Vector3 direction) {
        Debug.LogWarning("Player was pushed by a big bird!");
        // push player away
        Vector3 pushDir = GetDirectionUpped(direction, 10);
        GameManager.Instance.player.PushOut(pushDir, _knockbackStrength);
    }
    Vector3 GetDirectionUpped(Vector3 direction, float upAngle) {
        Vector3 newDir = direction.Flatten();
        newDir.y = newDir.magnitude * Mathf.Tan(upAngle * Mathf.Deg2Rad);
        return newDir.normalized;
    }
    bool CheckForTouch(Vector3 start, Vector3 end) {
        Ray ray = new Ray(start, end - start);
        return Physics.SphereCast(ray, 0.5f, Vector3.Distance(start, end), Utils.layer_Player.ToLayerMask());
    }
    bool Move(Vector3 targetPos, float speed) {
        if(transform.LerpPosition(targetPos, speed)) return true;
        return false;
    }
    bool Turn(Vector3 newDir, Vector3 upAxis, float rotationSpeed) {
        if(transform.SlerpRotation(newDir, upAxis, rotationSpeed)) return true;
        return false;
    }
}