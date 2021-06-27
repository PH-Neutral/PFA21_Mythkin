using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AshkynCore.Audio;

public class Corvid : Enemy {

    public enum AnimState
    {
        Attack, Fly
    }

    [SerializeField] float _afterAttackDelay = 3, _afterAttackUpAngle = 30, _knockbackStrength = 25;
    Vector3 _suspiciousPos, _attackPos, _afterAttackPos;
    bool _trajectoryPrepared, _attackDone;
    bool chargeSoundPlayed = false;
    float _searchTimer, _afterAttackTimer;

    protected override void Update() {
        base.Update();
    }

    public override void HitPlayer(Vector3 direction) {
        Vector3 pushDir = Utils.GetDirectionUpped(direction, 10);
        GameManager.Instance.player.PushOut(pushDir, _knockbackStrength);
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
        anim.speed = Speed * 0.5f;
        anim.SetInteger("State", (int)AnimState.Fly);
    }
    protected override void OnSearch() {
        base.OnSearch();
        Speed = moveSpeed;
        chargeSoundPlayed = false;
        _searchTimer = 0;
        anim.speed = Speed * 0.5f;
        anim.SetInteger("State", (int)AnimState.Fly);
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
        if(!destinationReached) {
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
            if (!chargeSoundPlayed) {
                anim.speed = 1;
                anim.SetInteger("State", (int)AnimState.Attack);
                AudioManager.instance.PlaySound(AudioTag.corvidCharge, gameObject);
                chargeSoundPlayed = true;
            }
            // after facing and having seen player for at least one frame, charge player then continue moving a bit
            if(!_attackDone) {
                Vector3 lastPos = transform.position;
                bool finishedMoving = Move(_attackPos, Speed);
                bool touchedPlayer = CheckForTouch(lastPos, transform.position);
                if(finishedMoving || touchedPlayer) {
                    if(touchedPlayer) {
                        HitPlayer(transform.position - lastPos);
                    }
                    _attackDone = true;
                    _afterAttackPos = transform.position + Utils.GetDirectionUpped(transform.position - lastPos, _afterAttackUpAngle) * 10;
                }
            } else {
                anim.speed = Speed * 0.5f;
                anim.SetInteger("State", (int)AnimState.Fly);
                _afterAttackTimer -= Time.deltaTime;
                Turn((_afterAttackPos - transform.position).Flatten(), Vector3.up, rotationSpeed);
                if(Move(_afterAttackPos, SprintSpeed) || _afterAttackTimer <= 0) {
                    _trajectoryPrepared = false;
                }
            }
        }
    }
    bool CheckForTouch(Vector3 start, Vector3 end) {
        Ray ray = new Ray(start, end - start);
        return Physics.SphereCast(ray, 0.5f, Vector3.Distance(start, end), Utils.l_Player.ToLayerMask());
    }
    float walkTimer, stepPerSec = 1.5f;
    bool Move(Vector3 targetPos, float speed) {
        float speedRatio = Speed / moveSpeed;
        float stepDelay = 1 / (stepPerSec * speedRatio);
        if (walkTimer >= stepDelay)
        {
            walkTimer -= stepDelay;
            AudioManager.instance.PlaySound(AudioTag.corvidFly, gameObject, speedRatio);
        }
        walkTimer += Time.deltaTime;
        if (transform.LerpPosition(targetPos, speed)) return true;
        return false;
    }
    bool Turn(Vector3 newDir, Vector3 upAxis, float rotationSpeed) {
        if(transform.SlerpRotation(newDir, upAxis, rotationSpeed)) return true;
        return false;
    }
    protected override void OnDestinationReached()
    {
        base.OnDestinationReached();
        AudioManager.instance.PlaySound(AudioTag.corvidTalk, gameObject);
    }
}