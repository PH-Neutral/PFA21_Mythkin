using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AshkynCore.Audio;

public class Snake : Enemy
{
    [SerializeField] float inGroundTime = 5f;
    bool isInGround;

    protected override void Awake()
    {
        base.Awake();
    }
    protected override void Update()
    {
        if (!isInGround)
        {
            base.Update();
        }
    }
    protected override void OnAggro()
    {
        base.OnAggro();
        AudioManager.instance.PlaySound(AudioTag.snakeTalk);
    }
    protected override void OnUpdate() {
        // decide the state you in
    }
    protected override void Passive()
    {

    }
    protected override void Search()
    {
        
    }
    protected override void Aggro()
    {
        if (soundHeard)
        {
            CancelInvoke(nameof(StopSearching));
            soundHeard = false;
            // Debug.Log(lastSoundVector);
            Debug.DrawLine(transform.position, transform.TransformPoint(lastSoundVector), Color.green);

            Vector3 lookPos = new Vector3(lastSoundVector.x, 0, lastSoundVector.z);
            head.LookAt(transform.TransformPoint(lookPos));
        }
        else
        {
            Invoke(nameof(StopSearching), searchDuration);
        }
    }
    protected override void OnSoundHeard()
    {
        if (lastSoundIsPlayer)
        {
            State = EnemyState.Aggro;
        }
        else
        {
            GoInHole();
        }
    }
    void StopSearching() {
        State = EnemyState.Passive;
    }
    void GoInHole()
    {
        head.gameObject.SetActive(false);

        AudioManager.instance.PlaySound(AudioTag.snakeGoesInHole, gameObject);
        //play anim goInHole
        isInGround = true;
        Invoke(nameof(LeaveHole), inGroundTime);
    }
    void LeaveHole()
    {
        head.gameObject.SetActive(true);


        //play anime leaveHole
        isInGround = false;
        if (Vector3.Distance(transform.position, target.position) <= GetComponent<SphereCollider>().radius)
        {
            Debug.Log("you died");
            // play anim bite
            // other.GetComponent<PlayerCharacter>().Die();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(isInGround) return;
        if (LayerMask.LayerToName(other.gameObject.layer) == Utils.l_Player)
        {
            if(TryGetComponent(out PlayerCharacter player)) Debug.Log("Player component found! Player is dead.");
            else Debug.Log("Player died, but where is component ?");
            // play anim bite
            // other.GetComponent<PlayerCharacter>().Die();
        }
    }
}
