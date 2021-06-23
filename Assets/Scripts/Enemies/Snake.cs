using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AshkynCore.Audio;

public class Snake : Enemy
{
    Transform tHead {
        get { return debugHead; }
    }
    [SerializeField] float attackDelay = 1f, headRadius = 0.5f;
    [SerializeField] Transform debugHead;
    Quaternion baseRotation;
    Vector3 attackPos, headStartPos;
    bool isInGround, targetInRange, startAttack, reloadAttack;
    float searchTimer, attackTimer;
    Coroutine searchTurnHead;

    protected override void Awake()
    {
        base.Awake();
        baseRotation = transform.rotation;
        debugHead.GetComponentInChildren<Collider>().enabled = false;
        headStartPos = tHead.position;
    }
    protected override void Update()
    {
        if (!isInGround)
        {
            base.Update();
        }
    }

    protected override void OnUpdate() {
        // decide the state you in
        if(soundHeard && !lastSoundIsPlayer) {
            GoInHole();
        } else if(targetInRange) {
            State = EnemyState.Aggro;
        }
    }
    protected override void OnAggro()
    {
        base.OnAggro();
        StopTurningHead();
        attackTimer = 0;
        startAttack = true;
        reloadAttack = false;
        AudioManager.instance.PlaySound(AudioTag.snakeTalk, gameObject, 1.5f);
        if(debugLogs) Debug.Log($"{name} => AGGRO !");
    }
    protected override void OnSearch() {
        base.OnSearch();
        searchTimer = 0;
        AudioManager.instance.PlaySound(AudioTag.snakeTalk, gameObject);
        if(debugLogs) Debug.Log($"{name} => SEARCH !");
    }
    protected override void OnPassive() {
        base.OnPassive();
        StopTurningHead();
        if(debugLogs) Debug.Log($"{name} => PASSIVE !");
    }
    protected override void Passive()
    {
        // when player is not seen
        transform.SlerpRotation(baseRotation, rotationSpeed); 

        if(soundHeard && lastSoundIsPlayer) {
            soundHeard = false;
            // if player was heard
            State = EnemyState.Search;
            return;
        }
    }
    protected override void Search()
    {
        if(soundHeard && lastSoundIsPlayer) {
            soundHeard = false;
            // when player is heard, move head towards sound source
            StartTurningHead(lastSoundVector.Flatten());
            searchTimer = 0;
        } else if(searchTimer < searchDuration) {
            searchTimer += Time.deltaTime;
        } else {
            // back to passive after a little delay
            State = EnemyState.Passive;
        }
    }
    protected override void Aggro()
    {
        // prepare to attack
        if(attackTimer < attackDelay) {
            attackTimer += Time.deltaTime;
            return;
        }
        // start attacking
        if(startAttack) {
            startAttack = false;
            attackPos = target.position;
        }
        if(!reloadAttack) {
            // move head to target over time
            Vector3 lastPos = tHead.position;
            bool lerpFinished = MoveHead(attackPos);
            //transform.LookAt(attackPos.Flatten(transform.position.y), Vector3.up);
            bool playerHit = CheckHeadCollision(lastPos, tHead.position - lastPos);
            if(playerHit) {
                // attack when head collide with target
                HitPlayer(Vector3.zero);
            } else if(!lerpFinished) {
                return;
            }
            reloadAttack = true;
        }
        if(reloadAttack) {
            // head go back at start pos
            if(MoveHead(headStartPos) && transform.SlerpRotation(baseRotation, rotationSpeed)) {
                if(targetInRange) {
                    OnAggro();
                } else {
                    State = EnemyState.Search;
                }
            }
        }
    }
    protected override void OnSoundHeard()
    {
        if(!lastSoundIsPlayer) {
            GoInHole();
        }
    }
    void StartTurningHead(Vector3 targetPos) {
        StopTurningHead();
        searchTurnHead = StartCoroutine(TurnTowardTarget(targetPos));
    }
    void StopTurningHead() {
        if(searchTurnHead == null) return;
        StopCoroutine(searchTurnHead);
    }
    IEnumerator TurnTowardTarget(Vector3 targetPos) {
        while(!transform.SlerpRotation(targetPos, Vector3.up, rotationSpeed)) {
            yield return null;
        }
        yield break;
    }
    bool MoveHead(Vector3 targetPos) {
        return tHead.LerpPosition(targetPos, SprintSpeed);
    }
    bool CheckHeadCollision(Vector3 startPos, Vector3 dirVector) {
        Ray ray = new Ray(startPos, dirVector);
        int layerMask = Utils.l_Player.ToLayerMask();
        if(Physics.SphereCast(ray, headRadius, dirVector.magnitude, layerMask)) {
            return true;
        }
        return false;
    }
    void GoInHole()
    {
        head.gameObject.SetActive(false);
        debugHead.gameObject.SetActive(false);

        AudioManager.instance.PlaySound(AudioTag.snakeGoesInHole, gameObject);
        //play anim goInHole
        isInGround = true;
        // Invoke(nameof(LeaveHole), inGroundTime);
    }
    void LeaveHole()
    {
        head.gameObject.SetActive(true);


        //play anime leaveHole
        isInGround = false;/*
        if (Vector3.Distance(transform.position, target.position) <= GetComponent<SphereCollider>().radius)
        {
            Debug.Log("you died");
            // play anim bite
            // other.GetComponent<PlayerCharacter>().Die();
        }*/
    }
    private void OnTriggerEnter(Collider other)
    {
        if(isInGround) return;
        if (other.gameObject.layer == LayerMask.NameToLayer(Utils.l_Player)) {
            targetInRange = true;
        }
    }
    private void OnTriggerExit(Collider other) {
        if(isInGround) return;
        if(other.gameObject.layer == LayerMask.NameToLayer(Utils.l_Player)) {
            targetInRange = false;
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        if(tHead == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(tHead.position, headRadius);
    }
#endif
}
