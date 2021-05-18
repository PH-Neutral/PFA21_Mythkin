using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public float SoundRadius
    {
        get
        {
            return _soundRangeCol.radius;
        }
        set
        {
            _soundRangeCol.radius = value;
        }
    }
    public float MoveSpeed
    {
        get
        {
            return (IsClimbing ? climbSpeedRatio : (Input.GetKey(KeyCode.LeftShift) ? runSpeedRatio : 1)) * moveSpeedBase;
        }
    }
    public bool IsStuned
    {
        get
        {
            return _stunRemainingTime > 0;
        }
        set
        {
            _stunRemainingTime = value ? _stunDuration : 0;
        }
    }
    public bool IsInsideGrass
    {
        get
        {
            return _nbGrassCollided > 0;
        }
    }
    public bool IsClimbing
    {
        get
        {
            return _isClimbing;
        }
        private set
        {
            _isClimbing = value;
            _rb.useGravity = !value;
            //GameManager.Instance.ChangeImageColor(value ? Color.green : Color.red);
        }
    }
    public float moveSpeedBase = 3, runSpeedRatio = 1.5f, climbSpeedRatio = 0.5f, rotationSpeed = 1, modelRotationSpeed = 10, jumpForce = 5, inAirMoveRatio = 0.25f, fallingDistance = 1, nearestClimb = 0.1f, t;

    [SerializeField] Transform _model;
    [SerializeField] PlayerCamera _playerCamera;
    [SerializeField] SphereCollider _soundRangeCol;
    [SerializeField] float _stunDuration = 2;
    [SerializeField] float _noiseRangeWalking = 5, _noiseRangeSprinting = 15;
    float _stunRemainingTime = 0;
    int _nbGrassCollided = 0;
    bool _isAlive = true, _isRunning = false, _isClimbing = false, _canDeClimb = false, isInPosition, isLerping;
    bool[] _canUseAbility;
    Rigidbody _rb;
    Vector3 movement = Vector3.zero, treeNormal, treeColPoint, startPos, targetPos;
    Quaternion startRot, targetRot;
    Transform helper;
    public float possitionOffset, offsetFromWall;
    float _debugMakeNoiseTimer = 0;

    private void Awake()
    {
        _canUseAbility = new bool[3];
        _rb = GetComponentInChildren<Rigidbody>();
        helper = new GameObject().transform;
        helper.name = "climb helper";
    }
    private void Update()
    {
        if (IsClimbing)
        {
            if (!isInPosition)
            {
                GetInPosition();
            }

            if (!isLerping)
            {
                Vector2 inputs = GetInputs();
                float m = Mathf.Abs(inputs.x) + Mathf.Abs(inputs.y);

                Vector3 h = helper.right * inputs.x;
                Vector3 v = helper.up * inputs.y;
                Vector3 moveDir = (h + v).normalized;

                bool canMove = CanMove(moveDir);
                if (!canMove || moveDir == Vector3.zero) return;

                t = 0;
                isLerping = true;
                startPos = transform.position;
                //Vector3 tp = helper.position - transform.position;
                targetPos = helper.position;

            }
            else
            {
                t += Time.deltaTime * MoveSpeed;
                if (t > 1)
                {
                    t = 1;
                    isLerping = false;
                }

                Vector3 cp = Vector3.Lerp(startPos, targetPos, t);
                transform.position = cp;
                transform.rotation = Quaternion.Slerp(transform.rotation, helper.rotation, Time.deltaTime * rotationSpeed);
            }
        }
        else
        {
            CheckForClimb();
            Move();
        }


        //if (_isClimbing) Climb();
        //else Move();
        Look();
        /*/ ----- debug ----- //
        if (Input.GetKeyDown(KeyCode.Return)) {
            MakeNoise(_debugNoiseRange);
        }//*/
    }
    private void LateUpdate()
    {
        _canDeClimb = false;
    }
    Vector2 GetInputs()
    {
        Vector2 inputs = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        return CartToPolar(inputs);
    }
    void Move()
    {
        Vector2 inputs = GetInputs();
        if (_rb.velocity.y == 0)
        {
            movement = _playerCamera.transform.localRotation * new Vector3(inputs.x, 0, inputs.y) * MoveSpeed;
        }
        else
        {
            movement += _playerCamera.transform.localRotation * new Vector3(inputs.x, 0, inputs.y) * MoveSpeed * inAirMoveRatio * Time.deltaTime;
        }
        //Vector3 movement = _playerCamera.transform.localRotation * new Vector3(polarInputs.x, 0, polarInputs.y) * (_rb.velocity.y == 0 ? 1 : inAirMoveRatio) * moveSpeed;
        float jump = Input.GetKey(KeyCode.Space) && _rb.velocity.y == 0 ? jumpForce : _rb.velocity.y;
        _rb.velocity = new Vector3(movement.x, jump, movement.z);
        if (movement.magnitude != 0)
        {
            float quaternionAngle = Quaternion.Angle(_model.localRotation, _playerCamera.transform.localRotation);
            _model.localRotation = Quaternion.Slerp(_model.localRotation, _playerCamera.transform.localRotation, modelRotationSpeed / quaternionAngle);
        }
        Debug.DrawRay(transform.position, _rb.velocity, Color.green);

        // noises
        MakeMovingNoises();
    }
    void Climb()
    {
        Debug.DrawRay(_model.position, treeNormal * 100, Color.cyan);

        float distanceToTree = Vector3.Distance(transform.position, treeColPoint);

        Vector2 inputs = GetInputs();
        _model.LookAt(_model.position - treeNormal);
        Vector3 dir = new Vector3(inputs.x, inputs.y, 0);
        if (_canDeClimb && inputs.y < 0)
        {
            dir = Vector3.forward * inputs.y;
        }
        movement = _model.localRotation * dir * MoveSpeed;
        if (distanceToTree < fallingDistance && distanceToTree > nearestClimb)
        {
            if (!_canDeClimb) movement += _model.TransformDirection(0, 0, MoveSpeed);
            GameManager.Instance.ChangeImageColor(Color.green);
        }
        else
        {
            GameManager.Instance.ChangeImageColor(Color.red);
        }
        _rb.velocity = movement;
    }
    void CheckForClimb()
    {
        Vector3 origin = _model.position;
        origin.y += 1.4f;
        Vector3 dir = _model.forward;
        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, 5f))
        {
            helper.position = PosWithOffset(origin, hit.point);
            InitForClimb(hit);
        }
    }
    void InitForClimb(RaycastHit hit)
    {
        IsClimbing = true;
        helper.transform.rotation = Quaternion.LookRotation(-hit.normal);
        startPos = transform.position;
        targetPos = hit.point + (hit.normal * offsetFromWall);
        t = 0;
        isInPosition = false;
    }

    bool CanMove(Vector3 moveDir)
    {
        Vector3 origin = transform.position;
        float dis = possitionOffset;
        Vector3 dir = moveDir;
        Debug.DrawRay(origin, dir * dis, Color.red);
        RaycastHit hit;

        if (Physics.Raycast(origin, dir, out hit, dis))
        {
            return false;
        }

        origin += moveDir * dis;
        dir = helper.forward;
        float dis2 = 0.5f;

        Debug.DrawRay(origin, dir * dis2, Color.blue);
        if (Physics.Raycast(origin, dir, out hit, dis))
        {
            helper.position = PosWithOffset(origin, hit.point);
            helper.rotation = Quaternion.LookRotation(-hit.normal);
            return true;
        }

        origin += dir * dis2;
        dir = -Vector3.up;

        Debug.DrawRay(origin, dir, Color.yellow);
        if (Physics.Raycast(origin, dir, out hit, dis2))
        {
            float angle = Vector3.Angle(helper.up, hit.normal);
            if (angle < 40)
            {
                helper.position = PosWithOffset(origin, hit.point);
                helper.rotation = Quaternion.LookRotation(-hit.normal);
                return true;
            }
        }

        return false;
    }

    void GetInPosition()
    {
        t += Time.deltaTime;
        if (t > 1)
        {
            t = 1;
            isInPosition = true;

            //enable the ik
        }

        Vector3 tp = Vector3.Lerp(startPos, targetPos, t);
        transform.position = tp;
        _model.rotation = Quaternion.Slerp(transform.rotation, helper.rotation, t);
    }
    Vector3 PosWithOffset(Vector3 origin, Vector3 target)
    {
        Vector3 direction = origin - target;
        direction.Normalize();
        Vector3 offset = direction * offsetFromWall;
        return target += offset;
    }
    Vector2 CartToPolar(Vector2 coord)
    {
        if (coord == Vector2.zero) return Vector2.zero;
        Vector2 vMax;
        if (Mathf.Abs(coord.x) > Mathf.Abs(coord.y))
        {
            vMax.x = Mathf.Sign(coord.x);
            vMax.y = coord.y / Mathf.Abs(coord.x);
        }
        else
        {
            vMax.y = Mathf.Sign(coord.y);
            vMax.x = coord.x / Mathf.Abs(coord.y);
        }
        return coord / vMax.magnitude;
    }
    void Look()
    {
        Vector2 inputs = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        _playerCamera.RotateHorizontal(inputs.x * rotationSpeed);
        _playerCamera.RotateVertical(inputs.y * rotationSpeed);
    }
    void Interact()
    {

    }
    void MakeMovingNoises()
    {
        Vector2 inputs = GetInputs();
        if (_rb.velocity.y == 0 && inputs.magnitude > 0)
        {
            if (_debugMakeNoiseTimer <= 0)
            {
                bool isSprinting = Input.GetKey(KeyCode.LeftShift);
                _debugMakeNoiseTimer += 0.5f * (isSprinting ? 0.5f : 1);
                float noiseRange = inputs.magnitude * (isSprinting ? _noiseRangeSprinting : _noiseRangeWalking);
                MakeNoise(noiseRange);
            }
            _debugMakeNoiseTimer -= Time.deltaTime;
        }
        else
        {
            _debugMakeNoiseTimer = 0;
        }
    }
    void MakeNoise() => MakeNoise(_noiseRangeWalking);
    void MakeNoise(float maxDistance)
    {
        float levelAtSource = Utils.SoundSourceLevelFromDistance(maxDistance, 1);
        Collider[] colliders = Physics.OverlapSphere(transform.position, maxDistance, 1 << LayerMask.NameToLayer("Enemies"));
        //Debug.Log($"Noise produced at {levelAtSource.ChangePrecision(4)}dB and reached {colliders.Length} colliders with radius {maxDistance}m.");
        float soundLevel;
        Vector3 relativePos;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].TryGetComponent(out Enemy enemy))
            {
                relativePos = transform.position - enemy.transform.position;
                soundLevel = Utils.CalculateSoundLevel(levelAtSource, relativePos.magnitude);
                enemy.HearSound(relativePos, soundLevel);
            }
        }
    }
    public void UnlockAbility(AbilityType ability)
    {
        if ((int)ability < 0 || (int)ability >= _canUseAbility.Length) return;
        _canUseAbility[(int)ability] = true;
    }

    /*
    private void OnCollisionStay(Collision collision)
    {
        if (IsClimbing)
        {
            _canDeClimb = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (Vector3.Dot(collision.contacts[i].normal, Vector3.up) > 0.8f)
                {
                    _canDeClimb = true;
                    Debug.Log("sol");
                    break;
                }
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("tree"))
        {
            IsClimbing = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("tree"))
        {
            IsClimbing = false;
            GameManager.Instance.ChangeImageColor(Color.red);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        RaycastHit hit;
        treeColPoint = Vector3.forward * fallingDistance * 2;
        for (float i = -90; i < 90; i += 1f)
        {
            if (Physics.Raycast(_model.position, Quaternion.Euler(0, i, 0) * _model.TransformDirection(Vector3.forward), out hit, _model.InverseTransformPoint(treeColPoint).magnitude))
            {
                Debug.DrawRay(_model.position, Quaternion.Euler(0, i, 0) * _model.TransformDirection(Vector3.forward), Color.red, 5);
                treeColPoint = hit.point;
                treeNormal = hit.normal;
            }
        }
    }*/
}
public enum AbilityType
{
    None = -1, Climbing = 0, GrowBomb = 1, OpenRoots = 2
}
