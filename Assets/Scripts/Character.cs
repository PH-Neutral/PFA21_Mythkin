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
    public float speed = 1;

    [SerializeField] PlayerCamera _playerCamera;
    [SerializeField] SphereCollider _soundRangeCol;
    [SerializeField] float _stunDuration = 2;
    float _stunRemainingTime = 0;
    int _nbGrassCollided = 0;
    bool _isAlive = true, _isRunning = false;
    bool[] _canUseAbility;
    Rigidbody _rb;

    private void Awake()
    {
        _canUseAbility = new bool[3];
        _rb = GetComponentInChildren<Rigidbody>();
    }
    private void Update()
    {
        Move();
        Look();
    }

    void Move()
    {
        Vector2 inputs = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector2 inputsNorm = inputs.normalized;
        _rb.velocity = new Vector3(inputs.x, _rb.velocity.y, inputs.y);
        if (inputsNorm.x != 0)
        {

        }

    }
    void Look()
    {
        Vector2 inputs = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        _playerCamera.RotateHorizontal(inputs.x);
        _playerCamera.RotateVertical(inputs.y);
    }
    void Interact()
    {

    }
    public void UnlockAbility(AbilityType ability)
    {
        if ((int)ability < 0 || (int)ability >= _canUseAbility.Length) return;
        _canUseAbility[(int)ability] = true;
    }
}
public enum AbilityType
{
    None = -1, Climbing = 0, GrowBomb = 1, OpenRoots = 2
}
