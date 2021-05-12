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
            GameManager.Instance.ChangeImageColor(value ? Color.green : Color.red);
        }
    }
    public float moveSpeedBase = 3, runSpeedRatio = 1.5f, rotationSpeed = 1, modelRotationSpeed = 10, jumpForce = 5, inAirMoveRatio = 0.25f;

    [SerializeField] PlayerCamera _playerCamera;
    [SerializeField] SphereCollider _soundRangeCol;
    [SerializeField] float _stunDuration = 2;
    public Transform model;
    float _stunRemainingTime = 0;
    int _nbGrassCollided = 0;
    bool _isAlive = true, _isRunning = false, _isClimbing = false, _canDeClimb = false;
    bool[] _canUseAbility;
    Rigidbody _rb;
    Vector3 movement = Vector3.zero, treeNormal;

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
        //Debug.Log(_rb.velocity);
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
            float quaternionAngle = Quaternion.Angle(model.localRotation, _playerCamera.transform.localRotation);
            model.localRotation = Quaternion.Slerp(model.localRotation, _playerCamera.transform.localRotation, modelRotationSpeed / quaternionAngle);
        }
        Debug.DrawRay(transform.position, _rb.velocity, Color.green);
        //Debug.Log(_rb.velocity.y);
    }
    void Climb()
    {
        Debug.DrawRay(model.position, treeNormal * 100, Color.cyan);
        Vector2 inputs = GetInputs();
        model.LookAt(model.position - treeNormal);
        Vector3 dir = Vector3.up * inputs.y;
        if (_canDeClimb && inputs.y < 0)
        {
            dir = Vector3.forward * inputs.y;
        }
        movement = model.localRotation * dir * MoveSpeed;
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
    public void UnlockAbility(AbilityType ability)
    {
        if ((int)ability < 0 || (int)ability >= _canUseAbility.Length) return;
        _canUseAbility[(int)ability] = true;
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("tree"))
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < collision.contactCount; i++)
            {
                sum += collision.contacts[i].normal;
            }
            treeNormal = sum.normalized;
            if (Mathf.Abs(Vector3.Dot(treeNormal, Vector3.up)) > 0.2f)
            {
                IsClimbing = false;
            }
        }
        else if (IsClimbing)
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
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("tree"))
        {
            IsClimbing = true;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("tree"))
        {
            IsClimbing = false;
        }
    }
}
public enum AbilityType
{
    None = -1, Climbing = 0, GrowBomb = 1, OpenRoots = 2
}
