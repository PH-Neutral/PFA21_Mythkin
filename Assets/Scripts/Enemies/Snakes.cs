using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Snakes : Enemy
{
    [SerializeField] float inGroundTime = 5f, searchDuration = 3f;
    [SerializeField] Transform _model;
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
    protected override void Passive()
    {

    }
    protected override void Search()
    {
        if (soundHeard)
        {
            CancelInvoke(nameof(StopSearching));
            soundHeard = false;
            // Debug.Log(lastSoundVector);
            Debug.DrawLine(transform.position, transform.TransformPoint(lastSoundVector), Color.green);

            Vector3 lookPos = new Vector3(lastSoundVector.x, 0, lastSoundVector.z);
            _model.LookAt(transform.TransformPoint(lookPos));
        }
        else
        {
            Invoke(nameof(StopSearching), searchDuration);
        }
    }
    void StopSearching()
    {
        State = EnemyState.Passive;
    }
    protected override void Aggro()
    {
        
    }
    protected override void OnSoundHeard()
    {
        if (lastSoundIsPlayer)
        {
            State = EnemyState.Search;
        }
        else
        {
            GoInHole();
        }
    }
    void GoInHole()
    {
        _model.gameObject.SetActive(false);


        //play anim goInHole
        isInGround = true;
        Invoke(nameof(LeaveHole), inGroundTime);
    }
    void LeaveHole()
    {
        _model.gameObject.SetActive(true);


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
        if (other.CompareTag("HitBox") && !isInGround)
        {
            Debug.Log("you died");
            // play anim bite
            // other.GetComponent<PlayerCharacter>().Die();
        }
    }
}
