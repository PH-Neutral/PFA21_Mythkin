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
    [SerializeField] protected EnemyState debugState;
    [Range(0, 100)]
    [SerializeField] protected float hearingLevelMin = 0.5f;
    [SerializeField] protected float rangeOfSight = 50, fieldOfView = 120, grassVerticalViewAngleMax = 50;
    public Transform sightCenter;
    [SerializeField] protected Transform target;
    [SerializeField] protected bool debugLogs, debugDraws;
    protected MeshRenderer[] _renderers;

    protected Vector3 lastSoundVector;
    protected bool soundHeard, lastSoundIsPlayer;
    protected float lastSoundLevel;

    protected EnemyState _state;

    protected virtual void Awake()
    {
        _renderers = GetComponentsInChildren<MeshRenderer>();
    }
    protected virtual void Start()
    {
        ChangeMaterial(GameManager.Instance.matEnemyPatrol); //if starts in passive state
        State = debugState;
    }
    protected virtual void Update()
    {
        // ------ debug ------
        if (debugState != State)
        {
            State = debugState;
        }
        // -------------------
        if (Look(out Vector3 targetPos))
        {
            State = EnemyState.Aggro;
        }
        else if (soundHeard)
        {
            if (State == EnemyState.Passive)
            {
                State = EnemyState.Search;
            }
        }

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
            soundHeard = true;
            lastSoundIsPlayer = isPlayer;
            if (debugLogs)
            {
                Debug.Log($"The enemy \"{name}\" heard a sound of intensity {soundLevel.ChangePrecision(0)} coming from {soundVector.magnitude.ChangePrecision(2)}m away.");
            }
            OnSoundHeard();
        }
    }
    protected abstract void Passive();
    protected abstract void Search();
    protected abstract void Aggro();
    protected abstract void OnSoundHeard();
    protected virtual void OnStateChange()
    {

    }
    protected virtual void OnPassive()
    {

    }
    protected virtual void OnSearch()
    {

    }
    protected virtual void OnAggro()
    {

    }
    protected void ChangeMaterial(Material mat)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].material = mat;
        }
    }
    protected bool Look(out Vector3 targetPos)
    {
        targetPos = target.position;

        //bool detectionLeft = DetectTarget(eyeLeft, target.gameObject);
        //bool detectionRight = DetectTarget(eyeRight, target.gameObject);
        //return (detectionLeft || detectionRight);
        return DetectTarget(sightCenter, target.gameObject);
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
}

[System.Serializable]
public enum EnemyState
{
    Passive, Search, Aggro
}