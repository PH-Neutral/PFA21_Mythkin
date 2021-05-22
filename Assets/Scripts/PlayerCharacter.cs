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
        get { return _charaCtrl.radius + _charaCtrl.skinWidth; }
    }
    float Height {
        get { return _charaCtrl.height + _charaCtrl.skinWidth * 2; }
    }
    public float interactionRange = 3f;
    public PlayerCamera playerCam;

    [SerializeField] Transform _bodyCenter, _model, _camCenter, throwPoint, _head;
    [SerializeField] float _moveSpeed = 5, _sprintRatio = 1.5f, _climbSpeed = 2, _rotationSpeed = 20, _jumpHeight = 5;
    [SerializeField] float _inAirMoveRatio = 1, _groundPullMagnitude = 5, _slideAcceleration = 2;
    [SerializeField] float _climbCheckDistanceOffset = 0.2f, _wallDistanceOffset = 0.1f;
    CharacterController _charaCtrl;
    TrajectoryHandler _trajectoryHandler;
    Vector3 _movement = Vector3.zero, _wallPoint;
    RaycastHit _declimbHit;
    bool _wasGrounded = false, _wasClimbing = true;
    bool _isOnClimbWall = false, _isLerpingToWall = false, _isDeclimbingUp = false, _declimbPart1 = true;
    bool _isAiming = false, _isThrowing = false, _isInteracting = false, _isJumping = false;
    float deltaTime;
    private void Awake() {
        _charaCtrl = GetComponent<CharacterController>();
        _trajectoryHandler = GetComponentInChildren<TrajectoryHandler>();
        if(_trajectoryHandler != null) _trajectoryHandler.playerCamera = playerCam;
    }
    private void Start() {
        playerCam.SetReferences(_head,_camCenter);
    }
    private void Update() {
        deltaTime = Time.deltaTime;
        _isJumping = Input.GetKey(KeyCode.Space);
        _isInteracting = Input.GetKeyDown(KeyCode.E);
        _isAiming = Input.GetMouseButton(1);
        _isThrowing = Input.GetMouseButtonDown(0);

        Look();
        HandleMovement();
        HandleInteractions();
        HandleThrowing();

        _wasClimbing = _isOnClimbWall;
        _wasGrounded = _charaCtrl.isGrounded;
    }
    void HandleMovement() {
        if(!_isDeclimbingUp) {
            if(!_wasClimbing) _isOnClimbWall = CheckForClimbMiddle() && CanClimbHorizontal(-1) && CanClimbHorizontal(1);
            _isOnClimbWall = CheckForClimbMiddle() || CheckForClimbDown(); // && !CanDeclimbUp();

            _movement = _isOnClimbWall ? Climb() : Move();

            _charaCtrl.Move(_movement * deltaTime);
        }
        if(_isDeclimbingUp) {
            Declimb();
        }
    }
    void HandleInteractions() {
        // Roots
        Root root;
        if ((root = CheckRootsInteraction()) != null) {
            if(_isInteracting) root.Open();
        }
        if(UIManager.Instance != null) UIManager.Instance.rootIndicator.SetActive(root != null);
    }
    void HandleThrowing() {
        if(_trajectoryHandler == null) return;
        _trajectoryHandler.IsDisplaying = _isAiming;
        if(_isAiming) {
            _trajectoryHandler.SetBombTrajectory();
            if(_isThrowing) {
                _trajectoryHandler.ThrowBomb();
            }
        }
    }
    void Look() {
        Vector2 inputs = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        playerCam.RotateHorizontal(inputs.x);
        playerCam.RotateVertical(inputs.y);
        //transform.localRotation = _playerCam.transform.localRotation;
        /*/ apply camera rotation to character
        if (!_isOnClimbWall) {
            transform.localRotation = _playerCam.transform.localRotation;
            _playerCam.transform.localRotation = Quaternion.identity;
        }*/
    }
    #region MOVEMENT
    /// <summary>
    /// Return inputs as such (x: horizontal, y: jumpStrength, z: vertical).
    /// </summary>
    /// <returns></returns>
    Vector3 GetInputs() {
        Vector2 inputs = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        inputs = Utils.CartToPolar(inputs);
        return new Vector3(inputs.x, _isJumping ? _jumpHeight : 0, inputs.y);
    }
    Vector3 Move() {
        Vector3 inputs = GetInputs();
        Vector3 flatInputs = new Vector3(inputs.x, 0, inputs.z);

        if(_wasClimbing) _movement = Vector3.zero;
        if(_isAiming) SlerpRotation(transform, new Vector3(playerCam.transform.forward.x, 0, playerCam.transform.forward.z), _rotationSpeed);
        else if(flatInputs.magnitude > 0) SlerpRotation(transform, playerCam.transform.TransformDirection(flatInputs), _rotationSpeed);

        Vector3 motion;
        //Vector3 slopeVector = GetSlopeVector();
        //bool inSlopeLimit = Vector3.Angle(Vector3.up, slopeVector) - 90 < _charaCtrl.slopeLimit;
        Vector3 slopeNormal = GetSlopeNormal();
        //float slopeAngle = Vector3.Angle(Vector3.up, slopeNormal);
        bool inSlopeLimit = Vector3.Angle(Vector3.up, slopeNormal) < _charaCtrl.slopeLimit;
        //Debug.Log($"slope angle: {slopeAngle} deg");
        if(_charaCtrl.isGrounded) {
            motion = new Vector3(inputs.x * Speed, _movement.y, inputs.z * Speed);
            if(inSlopeLimit && inputs.y > 0) motion.y = inputs.y;
            _movement = playerCam.transform.TransformDirection(motion);

            if(!inSlopeLimit) {
                //Debug.Log($"Slope too steep! ({Vector3.Angle(Vector3.up, slopeNormal)}deg)");
                //_movement += slopeVector * Vector3.Dot(slopeVector, Physics.gravity) * deltaTime;
                Vector3 slideVector = new Vector3(slopeNormal.x, 0, slopeNormal.z);
                _movement += slideVector * _slideAcceleration * deltaTime;
            }
        } else {
            //if(_wasGrounded && _movement.y < -0.5f) _movement.y = -0.5f;
            motion = flatInputs * Speed * _inAirMoveRatio * Time.deltaTime;
            _movement.x *= (1 - 0.8f * deltaTime); // damping in x
            _movement.z *= (1 - 0.8f * deltaTime); // damping in z
            _movement += playerCam.transform.TransformDirection(motion);
            if(_wasGrounded && _movement.y < 0) _movement.y = 0;
            else _movement += Physics.gravity * deltaTime;
        }
        return _movement;
    }
    Vector3 GetSlopeVector() {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        Vector3 rayDir = -transform.up * _charaCtrl.skinWidth * 2;
        int layerMaskTerrain = 1 << LayerMask.NameToLayer(Utils.layer_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskTerrain)) {
            // calculate downward slope vector
            return Vector3.Cross(Vector3.Cross(hit.normal, Vector3.down), hit.normal).normalized;
        }
        return Vector3.down;
    }
    Vector3 GetSlopeNormal() {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + transform.up * _charaCtrl.radius;
        Vector3 rayDir = -transform.up * Radius / Mathf.Sin(Mathf.Deg2Rad);
        int layerMaskTerrain = Utils.layer_Terrain.ToLayerMask();
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskTerrain)) {
            // calculate downward slope vector
            //Debug.DrawLine(rayOrigin, hit.point, Color.green);
            return hit.normal;
        }
        //Debug.DrawRay(rayOrigin, rayDir, Color.red);
        return Vector3.up;
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
    #endregion
    #region CLIMBING
    Vector3 Climb() {
        Vector3 inputs = GetInputs();
        // check for terrain under character
        bool isGrounded = CheckIfGrounded();
        if(CheckForClimbDown() && !CheckForClimbMiddle() && inputs.z > 0 && FindDeclimbHitPoint(out _declimbHit)) {
            _isDeclimbingUp = true;
            _declimbPart1 = true;
            return Vector3.zero;
        }
        // find closest wall point with offset
        if(!_wasClimbing) {
            _wallPoint = FindClosestWallPoint(_bodyCenter, Vector3.zero, 90, 1);
        }
        if(!_isLerpingToWall) {
            _wallPoint = FindClosestWallPoint(_bodyCenter, Vector3.zero, 90, 1);
        }
        Vector3 wallDir = _wallPoint - _bodyCenter.position;
        Vector3 dirToOffsettedPoint = wallDir - wallDir.normalized * (Radius + _wallDistanceOffset);
        if(!(isGrounded && inputs.z < 0) && dirToOffsettedPoint.magnitude > 0.01f) {
            _isLerpingToWall = true;
        }
        // lerping to adapt to the wall
        if(_isLerpingToWall) {
            if(!SlerpRotation(_model, wallDir, _rotationSpeed) && !LerpPosition(transform.position + dirToOffsettedPoint, _moveSpeed)) {
                return Vector3.zero;
            }
            _isLerpingToWall = false;
        }

        // movement
        _movement = new Vector3(inputs.x, inputs.z, 0) * _climbSpeed;
        if(isGrounded && inputs.z < 0) {
            _movement = new Vector3(inputs.x, -_groundPullMagnitude, inputs.z) * Speed;
        }
        bool canMoveHori = CanClimbHorizontal(inputs.x);
        bool canMoveVert = CanClimbVertical(inputs.z);
        if(!canMoveHori) _movement.x = 0;
        if(!canMoveVert) _movement.y = 0;
        return _model.TransformDirection(_movement);
    }
    void Declimb() {
        if(_declimbPart1) {
            // transform.pos to up
            Vector3 vHit = _declimbHit.point - transform.position;
            float upMagn = vHit.magnitude * Mathf.Sin(Vector3.Angle(vHit, _bodyCenter.forward) * Mathf.Deg2Rad);
            Vector3 lerpPoint1 = transform.position + _bodyCenter.up * upMagn;
            if(LerpPosition(lerpPoint1, _climbSpeed)) {
                _declimbPart1 = false;
            }
        }
        if(!_declimbPart1) {
            if(LerpPosition(_declimbHit.point, _moveSpeed)) {
                _isDeclimbingUp = false;
            }
        }
    }
    bool CheckIfGrounded() {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        Vector3 rayDir = -transform.up * _charaCtrl.skinWidth * 2;
        int layerMaskTerrain = 1 << LayerMask.NameToLayer(Utils.layer_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskTerrain)) {
            // there is terrain under our feet
            return true;
        }
        return false;
    }
    Vector3 FindClosestWallPoint(Transform origin, Vector3 offset, float maxAngle = 90, float stepAngle = 1) {
        Vector3 rayOrigin = origin.position + origin.TransformDirection(offset);
        Vector3 centerDir = origin.forward * (Radius + _wallDistanceOffset);
        Vector3 hitPoint = rayOrigin + centerDir * 1.5f;
        int layerMask = 1 << LayerMask.NameToLayer(Utils.layer_Terrain);
        for(float i = -maxAngle; i <= maxAngle; i += stepAngle) {
            Quaternion iRot = Quaternion.Euler(0, i, 0);
            if(Physics.Raycast(rayOrigin, iRot * centerDir.normalized, out RaycastHit hit, (hitPoint - rayOrigin).magnitude, layerMask)) {
                Debug.DrawLine(origin.position, hit.point, Color.red, 1);
                hitPoint = hit.point;
            }
        }
        return hitPoint;
    }
    bool FindDeclimbHitPoint(out RaycastHit hitPoint) {
        Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir = _bodyCenter.forward * (Radius * 2 + _wallDistanceOffset);
        int layerMaskWall = 1 << LayerMask.NameToLayer(Utils.layer_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hitPoint, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "forward" direction
            return false;
        }
        rayOrigin += rayDir;
        rayDir = -_bodyCenter.up * Height * 0.5f;
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hitPoint, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "forward -> down" direction
            return true;
        }
        return false;
    }
    bool CheckForClimbMiddle() {
        RaycastHit hit;
        Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir = _bodyCenter.forward * (Radius + _wallDistanceOffset);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer(Utils.layer_Environment);
        int layerMaskWall = 1 << LayerMask.NameToLayer(Utils.layer_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "forward" direction
            if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskClimbZone)) {
                // this surface is NOT a climbable wall
                return true;
            }
        }
        return false;
    }
    bool CheckForClimbDown() {
        RaycastHit hit;
        Vector3 rayOrigin = _model.position;
        Vector3 rayDir = _bodyCenter.forward * (Radius + _wallDistanceOffset);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer(Utils.layer_Environment);
        int layerMaskWall = 1 << LayerMask.NameToLayer(Utils.layer_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "forward" direction
            if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskClimbZone)) {
                // this surface is a climbable wall
                return true;
            }
        }
        return false;
    }
    bool CanClimbVertical(float vInput) {
        if(vInput == 0) return false;
        RaycastHit hit;
        Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir = Mathf.Sign(vInput) * transform.up * (Height * 0.5f + _climbCheckDistanceOffset);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer(Utils.layer_Environment);
        int layerMaskWall = 1 << LayerMask.NameToLayer(Utils.layer_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.blue);
            return vInput < 0;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.blue);

        rayOrigin += rayDir;
        rayDir = _model.forward * (Radius + _wallDistanceOffset * 2);
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
        rayDir = Mathf.Sign(-vInput) * _model.up * (Height + _climbCheckDistanceOffset);
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
        Vector3 rayDir = Mathf.Sign(hInput) * _model.right * (Radius + _climbCheckDistanceOffset);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer(Utils.layer_Environment);
        int layerMaskWall = 1 << LayerMask.NameToLayer(Utils.layer_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "right/left" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.blue);
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.blue);

        rayOrigin += rayDir;
        rayDir = _model.forward * (Radius + _wallDistanceOffset * 2);
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
        rayDir = Mathf.Sign(-hInput) * _model.right * (_climbCheckDistanceOffset);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "right/left -> forward -> left/right" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.green);
            return true;
        }
        // no surface blocks the "right/left -> forward -> left/right" direction
        Debug.DrawRay(rayOrigin, rayDir, Color.green);
        return false;
    }
    #endregion
    #region INTERACTIONS
    Root CheckRootsInteraction()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCam.cam.transform.position, playerCam.cam.transform.forward, out hit, 20, Utils.layer_Interactibles.ToLayerMask()))
        {
            if (Vector3.Distance(transform.position, hit.point) > interactionRange)
            {
                return null;
            }

            if (hit.collider.CompareTag("Roots"))
            {
                return hit.collider.GetComponent<Root>();
            }
        }
        return null;
    }
    #endregion
    bool CanDeclimbUp() {
        RaycastHit hit;
        Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir = _model.up * (Height * 0.5f + _climbCheckDistanceOffset);
        int layerMaskWall = Utils.layer_Terrain.ToLayerMask();
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
        rayDir = -_model.up * (Height * 0.5f + _climbCheckDistanceOffset);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down -> forward -> down/up" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.green);
            float stepHeight = Vector3.Distance(rayOrigin + rayDir, hit.point);
            return false;//stepHeight <= _maxStepHeight;
        }
        // no surface blocks the "up/down -> forward -> down/up" direction
        Debug.DrawRay(rayOrigin, rayDir, Color.green);
        return true;
    }

    private void OnTriggerStay(Collider other) {
        if (other.TryGetComponent(out TunnelEntrance tEntrance)) {
            playerCam.maxZoomRatio = tEntrance.GetCamLerpRatio(transform.position, true);
        }
    }
    private void OnTriggerExit(Collider other) {
        if(other.TryGetComponent(out TunnelEntrance tEntrance)) {
            playerCam.maxZoomRatio = tEntrance.GetCamLerpRatio(transform.position, true);
        }
    }
}