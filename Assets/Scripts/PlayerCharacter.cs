using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour {
    public float Speed {
        get {
            return _moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? _sprintRatio : 1);
        }
    }
    public bool IsClimbing {
        get {
            return !_charaCtrl.isGrounded && _isOnClimbWall;
        }
    }
    float Radius {
        get { return _charaCtrl.radius + _charaCtrl.skinWidth + 0.05f; }
    }
    float Height {
        get { return _charaCtrl.height + _charaCtrl.skinWidth + 0.05f; }
    }
    [SerializeField] PlayerCamera _playerCam;
    [SerializeField] Transform _bodyCenter, _model, _camCenter;
    [SerializeField] float _moveSpeed = 5, _sprintRatio = 1.5f, _climbSpeed = 2, _rotationSpeed = 20, _camRotateSpeed = 100, _jumpHeight = 5;
    [SerializeField] float _inAirMoveRatio = 1, _terminalVelocity = 10, _lerpMoveTime = 0.5f; // in sec
    [SerializeField] float _maxStepHeight = 0.1f;
    [SerializeField] float _climbCheckOffset = 0.2f, _wallDistanceOffset = 0.1f;
    CharacterController _charaCtrl;
    Vector3 _movement = Vector3.zero, _wallPoint;
    bool _wasClimbing = true, _isOnClimbWall = false, _hasClimbFloor = false, _isLerpingToWall = false;
    float deltaTime;
    private void Awake() {
        _charaCtrl = GetComponent<CharacterController>();
    }
    private void Start() {
        _playerCam.Reference = _camCenter;
    }
    private void Update() {
        deltaTime = Time.deltaTime;

        _isOnClimbWall = CheckForClimb() && !CanDeclimbUp();

        Look();
        if(_isOnClimbWall) Climb();
        else Move();

        _wasClimbing = _isOnClimbWall;
    }
    void Move() {
        Vector3 inputs = GetInputs();
        Vector3 flatInputs = new Vector3(inputs.x, 0, inputs.z);
        if (_wasClimbing) {
            _movement = Vector3.zero;
        }

        //if(flatInputs.magnitude > 0) SlerpRotation(_model, transform.TransformDirection(flatInputs), _rotationSpeed);
        if(flatInputs.magnitude > 0) {
            SlerpRotation(transform, _playerCam.transform.TransformDirection(flatInputs), _rotationSpeed);
        }
        // horizontal movements
        if(_charaCtrl.isGrounded) {
            _movement = new Vector3(inputs.x * Speed, _movement.y, inputs.z * Speed);
            _movement.y = inputs.y > 0 ? inputs.y : -0.5f;
        } else {
            _movement += flatInputs * Speed * _inAirMoveRatio * Time.deltaTime;
        }
        _movement.y = Mathf.Clamp(_movement.y - 9.81f * deltaTime, -_terminalVelocity, _terminalVelocity);

        _charaCtrl.Move(_playerCam.transform.TransformDirection(_movement) * deltaTime);
        //Debug.Log(_charaCtrl.velocity);
    }
    void Climb() {
        Vector3 inputs = GetInputs();
        // check for terrain under character
        bool isGrounded = IsOnGround();
        // find closest wall point with offset
        if(!_wasClimbing) {
            _wallPoint = FindClosestWallPoint(_bodyCenter, Vector3.zero, 90, 1);
        }
        if(!_isLerpingToWall) {
            _wallPoint = FindClosestWallPoint(_bodyCenter, Vector3.zero, 90, 1);
        }
        Vector3 wallDir = _wallPoint - _bodyCenter.position;
        Vector3 dirToOffsettedPoint = wallDir - wallDir.normalized * (Radius + _wallDistanceOffset);
        if (!(isGrounded && inputs.z < 0) && dirToOffsettedPoint.magnitude > 0.01f) {
            _isLerpingToWall = true;
        }
        // lerping to adapt to the wall
        if (_isLerpingToWall) {
            if(!SlerpRotation(_model, wallDir, _rotationSpeed) && !LerpPosition(transform.position + dirToOffsettedPoint, _moveSpeed)) {
                return;
            }
            _isLerpingToWall = false;
        }

        // movement
        _movement = new Vector3(inputs.x, inputs.z, 0) * _climbSpeed;
        if(isGrounded && inputs.z < 0) {
            _movement = new Vector3(inputs.x, -_terminalVelocity, inputs.z) * Speed;
        }
        bool canMoveHori = CanClimbHorizontal(inputs.x);
        bool canMoveVert = CanClimbVertical(inputs.z);
        if(!canMoveHori) _movement.x = 0;
        if(!canMoveVert) _movement.y = 0;

        _charaCtrl.Move(_model.TransformDirection(_movement) * Time.deltaTime);
        //Debug.Log(_charaCtrl.velocity);
    }
    void Look() {
        Vector2 inputs = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        if(!_isOnClimbWall) _playerCam.RotateHorizontal(inputs.x * _camRotateSpeed);
        _playerCam.RotateVertical(inputs.y * _camRotateSpeed);
        //transform.localRotation = _playerCam.transform.localRotation;
        /*/ apply camera rotation to character
        if (!_isOnClimbWall) {
            transform.localRotation = _playerCam.transform.localRotation;
            _playerCam.transform.localRotation = Quaternion.identity;
        }*/
    }
    bool IsOnGround() {
        RaycastHit hit;
        Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir = -transform.up * Height * 0.5f;
        int layerMaskTerrain = 1 << LayerMask.NameToLayer("Terrain");
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskTerrain)) {
            // there is terrain under our feet
            return true;
        }
        return false;
    }
    bool CheckForClimb() {
        bool wallDown = false, wallMiddle = false;
        RaycastHit hit;
        Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir = transform.forward * (Radius + _wallDistanceOffset);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer("Environment");
        int layerMaskWall = 1 << LayerMask.NameToLayer("Terrain");
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "forward" direction
            if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskClimbZone)) {
                // this surface is NOT a climbable wall
                wallMiddle = true;
            }
        }

        rayOrigin = _model.position;
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskClimbZone)) {
            // this surface is a climbable wall
            wallDown = true;
        }
        Debug.Log("isOnClimbWall: " + (wallMiddle || wallDown));
        return wallMiddle || wallDown;
    }
    bool CanClimbVertical(float vInput) {
        if(vInput == 0) return false;
        RaycastHit hit;
        Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir = Mathf.Sign(vInput) * _model.up * (Height * 0.5f + _climbCheckOffset);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer("Environment");
        int layerMaskWall = 1 << LayerMask.NameToLayer("Terrain");
        if (Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.blue);
            return vInput < 0;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.blue);

        rayOrigin += rayDir;
        rayDir = _model.forward * (Radius + _wallDistanceOffset);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down -> forward" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.red);
            if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskClimbZone)) {
                // this surface is climbable wall
                return true;
            }
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.red);

        rayOrigin += rayDir;
        rayDir = Mathf.Sign(-vInput) * _model.up * (Height + _climbCheckOffset);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down -> forward -> down/up" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.green);
            return true;
        }
        // no surface blocks the "up/down -> forward -> down/up" direction
        Debug.DrawRay(rayOrigin, rayDir, Color.green);
        return false;
    }
    bool CanClimbHorizontal(float hInput) {
        if(hInput == 0) return false;
        RaycastHit hit;
        Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir = Mathf.Sign(hInput) * _model.right * (Radius + _climbCheckOffset);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer("Environment");
        int layerMaskWall = 1 << LayerMask.NameToLayer("Terrain");
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "right/left" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.blue);
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.blue);

        rayOrigin += rayDir;
        rayDir = _model.forward * (Radius + _wallDistanceOffset);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "right/left -> forward" direction
            if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskClimbZone)) {
                // this surface is climbable wall
                Debug.DrawLine(rayOrigin, hit.point, Color.red);
                return true;
            }
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.red);

        rayOrigin += rayDir;
        rayDir = Mathf.Sign(-hInput) * _model.right * (_climbCheckOffset);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "right/left -> forward -> left/right" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.green);
            return true;
        }
        // no surface blocks the "right/left -> forward -> left/right" direction
        Debug.DrawRay(rayOrigin, rayDir, Color.green);
        return false;
    }
    bool CanDeclimbUp() {
        RaycastHit hit;
        Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir = _model.up * (Height * 0.5f + _climbCheckOffset);
        int layerMaskWall = 1 << LayerMask.NameToLayer("Terrain");
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.blue);
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.blue);

        rayOrigin += rayDir;
        rayDir = _model.forward * (Radius + _wallDistanceOffset);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down -> forward" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.red);
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.red);

        rayOrigin += rayDir;
        rayDir = -_model.up * (Height + _climbCheckOffset);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down -> forward -> down/up" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.green);
            float stepHeight = Vector3.Distance(rayOrigin + rayDir, hit.point);
            return stepHeight <= _maxStepHeight;
        }
        // no surface blocks the "up/down -> forward -> down/up" direction
        Debug.DrawRay(rayOrigin, rayDir, Color.green);
        return false;
    }
    /// <summary>
    /// Return inputs as such (x: horizontal, y: jumpStrength, z: vertical).
    /// </summary>
    /// <returns></returns>
    Vector3 GetInputs() {
        Vector2 inputs = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        inputs = Utils.CartToPolar(inputs);
        return new Vector3(inputs.x, Input.GetKeyDown(KeyCode.Space) ? _jumpHeight : 0, inputs.y);
    }
    bool LerpPosition(Vector3 targetPos, float lerpSpeed) {
        float t = lerpSpeed * Time.deltaTime / Vector3.Distance(transform.position, targetPos);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        return t >= 1;
    }
    bool SlerpRotation(Transform obj, Vector3 newDirection, float rotateSpeed) {
        float vectorAngle = Vector3.Angle(obj.TransformDirection(Vector3.forward), newDirection);
        Quaternion newRotation = Quaternion.LookRotation(newDirection, obj.TransformDirection(Vector3.up));
        float t = rotateSpeed * Time.deltaTime / vectorAngle;
        obj.rotation = Quaternion.Slerp(obj.rotation, newRotation, t);
        return t >= 1;
    }
    Vector3 FindClosestWallPoint(Transform origin, Vector3 offset, float maxAngle = 90, float stepAngle = 1) {
        Vector3 rayOrigin = origin.position + origin.TransformDirection(offset);
        Vector3 centerDir = origin.forward * (Radius + _wallDistanceOffset);
        Vector3 hitPoint = rayOrigin + centerDir * 1.5f;
        int layerMask = 1 << LayerMask.NameToLayer("Terrain");
        for(float i = -maxAngle; i <= maxAngle; i += stepAngle) {
            Quaternion iRot = Quaternion.Euler(0, i, 0);
            if(Physics.Raycast(rayOrigin, iRot * centerDir.normalized, out RaycastHit hit, (hitPoint - rayOrigin).magnitude, layerMask)) {
                Debug.DrawLine(origin.position, hit.point, Color.red, 1);
                hitPoint = hit.point;
            }
        }
        return hitPoint;
    }

    Vector3 FindWallPoint(Transform origin, Vector3 offset) {
        Vector3 rayOrigin = origin.position + origin.TransformDirection(offset);
        Vector3 rayDir = origin.forward * (Radius + _wallDistanceOffset);
        int layerMask = 1 << LayerMask.NameToLayer("Terrain");
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out RaycastHit hit, rayDir.magnitude, layerMask)) {
            //Debug.DrawLine(origin.position, hit.point, Color.red, 5);
            return hit.point;
        }
        return rayOrigin + rayDir;
    }
}