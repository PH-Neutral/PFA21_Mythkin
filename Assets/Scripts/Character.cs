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
            return (Input.GetKey(KeyCode.LeftShift) ? runSpeedRatio : 1) * moveSpeedBase;
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
    public float moveSpeedBase = 3, runSpeedRatio = 1.5f, rotationSpeed = 1, modelRotationSpeed = 10, jumpForce = 5, inAirMoveRatio = 0.25f, fallingDistance = 1, nearestClimb = 0.1f;

    [SerializeField] Transform _model;
    [SerializeField] PlayerCamera _playerCamera;
    [SerializeField] SphereCollider _soundRangeCol;
    [SerializeField] float _stunDuration = 2;
    [SerializeField] float _noiseRangeWalking = 100;
    float _stunRemainingTime = 0;
    int _nbGrassCollided = 0;
    bool _isAlive = true, _isRunning = false, _isClimbing = false, _canDeClimb = false;
    bool[] _canUseAbility;
    Rigidbody _rb;
    Vector3 movement = Vector3.zero, treeNormal, treeColPoint;
    float _debugMakeNoiseTimer = 0;

    private void Awake()
    {
        _canUseAbility = new bool[3];
        _rb = GetComponentInChildren<Rigidbody>();
    }
    private void Update()
    {
        if (_isClimbing) Climb();
        else Move();
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
            movement += transform.TransformDirection(0, 0, MoveSpeed);
            GameManager.Instance.ChangeImageColor(Color.green);
        }
        else
        {
            GameManager.Instance.ChangeImageColor(Color.red);
        }
        _rb.velocity = movement;
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
    void MakeMovingNoises() {
        Vector2 inputs = GetInputs();
        if(_rb.velocity.y == 0 && inputs.magnitude > 0) {
            if(_debugMakeNoiseTimer <= 0) {
                bool isSprinting = Input.GetKey(KeyCode.LeftShift);
                _debugMakeNoiseTimer += 0.5f * (isSprinting ? 0.5f : 1);
                float noiseRange = _noiseRangeWalking * inputs.magnitude * (isSprinting ? 2 : 1);
                MakeNoise(noiseRange);
            }
            _debugMakeNoiseTimer -= Time.deltaTime;
        } else {
            _debugMakeNoiseTimer = 0;
        }
    }
    void MakeNoise() => MakeNoise(_noiseRangeWalking);
    void MakeNoise(float maxDistance) {
        float levelAtSource = Utils.SoundSourceLevelFromDistance(maxDistance, 1);
        Collider[] colliders = Physics.OverlapSphere(transform.position, maxDistance, 1<<LayerMask.NameToLayer("Enemies"));
        //Debug.Log($"Noise produced at {levelAtSource.ChangePrecision(4)}dB and reached {colliders.Length} colliders with radius {maxDistance}m.");
        float soundLevel;
        Vector3 relativePos;
        for(int i = 0; i < colliders.Length; i++) {
            if (colliders[i].TryGetComponent(out Enemy enemy)) {
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
    private void OnCollisionStay(Collision collision)
    {
        /*if (collision.gameObject.CompareTag("tree"))
        {
            treeColPoint = Vector3.zero;
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < collision.contactCount; i++)
            {
                sum += collision.contacts[i].normal;
                treeColPoint += collision.contacts[i].point;
            }
            treeNormal = sum.normalized;
            if (Mathf.Abs(Vector3.Dot(treeNormal, Vector3.up)) > 0.2f)
            {
                IsClimbing = false;
            }
        }
        else*/
        if (IsClimbing)
        {
            _canDeClimb = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (Vector3.Dot(collision.contacts[i].normal, Vector3.up) > 0.8f)
                {
                    _canDeClimb = true;
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
        for (float i = -90; i < 90; i += 0.1f)
        {
            if (Physics.Raycast(transform.position, Quaternion.Euler(0, i, 0) * _model.TransformDirection(Vector3.forward), out hit, treeColPoint.magnitude))
            {
                treeColPoint = hit.point;
                treeNormal = hit.normal;
            }
        }
    }
}
public enum AbilityType
{
    None = -1, Climbing = 0, GrowBomb = 1, OpenRoots = 2
}
