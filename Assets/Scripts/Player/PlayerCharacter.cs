using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AshkynCore.Audio;

public class PlayerCharacter : MonoBehaviour {
    public float Speed {
        get {
            return _moveSpeed * (_isRunning ? _sprintRatio : 1);
        }
    }
    public float Acceleration {
        get {
            return _moveSpeed / _accelerationTime;
        }
    }
    public float Deceleration {
        get {
            return _moveSpeed * _sprintRatio / _decelerationTime;
        }
    }
    public float ClimbSpeed {
        get {
            return _climbSpeed * (_isRunning ? _sprintRatio : 1);
        }
    }
    public float RotationSpeed {
        get {
            return _rotationSpeed * 360;
        }
    }
    float Radius {
        get { return _charaCtrl.radius + _charaCtrl.skinWidth; }
    }
    float Height {
        get { return _charaCtrl.height + _charaCtrl.skinWidth * 2; }
    }
    Vector3 BodyCenter {
        get { return transform.position + transform.up * (_charaCtrl.height * 0.5f + _charaCtrl.skinWidth * 2); }
    }
    Vector3 FeetPos {
        get { return transform.position + transform.up * _charaCtrl.skinWidth; }
    }
    public Transform head;

    [SerializeField] Transform _camCenter, _throwPoint;
    [SerializeField] bool advancedMovement = false, debugDraws = false;
    [SerializeField] float _accelerationTime = 0.5f, _decelerationTime = 0.2f, _airDrag = 0.5f;
    [SerializeField] float _moveSpeed = 5, _sprintRatio = 1.5f, _climbSpeed = 2, _rotationSpeed = 20, _jumpHeight = 5;
    [SerializeField] float _inAirMoveRatio = 1, _groundPullMagnitude = 5;
    [SerializeField] float _climbCheckDistanceOffset = 0.2f, _wallDistanceOffset = 0.1f;
    [Range(0, 89)][SerializeField] float _climbSideAngle = 40;
    [SerializeField] float _soundRadiusRun = 10f, _soundRadiusWalk = 5f;
    [SerializeField] float interactionRange = 3f;
    [SerializeField] MeshRenderer _fakebomb; //temporary. Will need to do it by animation
    CharacterController _charaCtrl;
    PlayerCamera _playerCam;
    TrajectoryHandler _trajectoryHandler;
    Animator _anim;
    Renderer[] _renderers;
    bool _isModelHidden = false;
    Vector3 _movement = Vector3.zero, _wallPoint, _inputs = Vector3.zero, _declimbPoint1;
    RaycastHit _declimbHit;
    bool _wasGrounded = false, _wasClimbing = true, _wasPushed = false;
    bool _isOnClimbWall = false, _isDeclimbingUp = false, _declimbPart1 = true;
    bool _isAiming = false, _isThrowing = false, _hasBomb = false, _isInteracting = false, _isJumping = false, _isRunning = false;
    bool _isInJump = false, _startJumping = false, _stopJumping = false, _isFalling = false;
    float deltaTime;
    BombPlant lastPlant;
    Vector3 move = Vector3.zero, _jumpBasePos;
    List<ControllerColliderHit> ccHits = new List<ControllerColliderHit>();
    private void Awake() {
        _charaCtrl = GetComponent<CharacterController>();
        _playerCam = GetComponentInChildren<PlayerCamera>();
        _trajectoryHandler = GetComponentInChildren<TrajectoryHandler>();
        if(_trajectoryHandler != null) _trajectoryHandler.playerCamera = _playerCam;
        _anim = GetComponentInChildren<Animator>();
        //_renderers = _model.GetComponentsInChildren<Renderer>();
    }
    private void Start() {
        _playerCam.SetReferences(this, head,_camCenter);
    }
    private void Update() {
        if(GameManager.Instance.GamePaused) return;

        deltaTime = Time.deltaTime;
        _isJumping = Input.GetKey(KeyCode.Space);
        _isInteracting = Input.GetKeyDown(KeyCode.E);
        if(Input.GetKeyDown(KeyCode.Alpha1)) _isAiming = _hasBomb ? !_isAiming : false;
        _isThrowing = Input.GetKeyDown(KeyCode.Alpha2);
        _isRunning = Input.GetKey(KeyCode.LeftShift);
        _inputs = GetInputs();


        Look();
        HandleMovement();
        HandleInteractions();
        HandleThrowing();
        HandleSound();
        HandleAnimations();

        _wasClimbing = _isOnClimbWall;
        _wasGrounded = _charaCtrl.isGrounded;
    }
    private void LateUpdate() {
        ccHits.Clear();
        _wasPushed = false;
    }
    void Look() {
        Vector2 inputs = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        _playerCam.RotateHorizontal(inputs.x);
        _playerCam.RotateVertical(inputs.y);
    }
    void HandleMovement() {
        if(!_isDeclimbingUp) {
            _isOnClimbWall = CheckForClimb();
            //Debug.Log("Climbing = " + _isOnClimbWall);

            if(!_wasPushed) {
                _movement = _isOnClimbWall ? Climb() : Move();
            }

            _charaCtrl.Move(_movement * deltaTime);
        }
        if(_isDeclimbingUp) {
            if(_wasPushed) ResetDeclimb();
            Declimb();
        }
    }
    void HandleInteractions() {
        // Roots
        Root root;
        if ((root = CheckRootsInteraction()) != null) {
            root.ShowOutline(true);
            if(_isInteracting) root.Open();
        }
        UIManager.Instance?.rootIndicator.SetActive(root != null);

        // Plants
        BombPlant bombPlant;
        if ((bombPlant = CheckPlantInteraction()) != null) {
            bombPlant.ShowOutline(true);
            if (_isInteracting && !_hasBomb) { 
                bombPlant.PickBomb();
                _hasBomb = true;
                _fakebomb.enabled = true;
                lastPlant = bombPlant;
                AudioManager.instance?.PlaySound(AudioTag.fruitBombTaken, 1);
            }
        }
        UIManager.Instance?.bombIndicator.SetActive(bombPlant != null);
    }
    void HandleThrowing() {
        if(_trajectoryHandler == null) return;
        if(!_hasBomb) return;

        _trajectoryHandler.IsDisplaying = _isAiming;
        if (_isAiming){
            _trajectoryHandler.SetBombTrajectory();
            if (_isThrowing){
                _fakebomb.enabled = false;
                _trajectoryHandler.ThrowBomb();
                lastPlant.GrowBomb();
                _hasBomb = false;
                _trajectoryHandler.IsDisplaying = false;
            }
        } 
        
    }
    float walkTimer, stepPerSec = 2, climbingNoisePerSec = 0.75f;
    void HandleSound() {
        if(_inputs != Vector3.zero) {
            if(_charaCtrl.isGrounded) {
                if(!_wasGrounded) {
                    Utils.EmitSound(_soundRadiusRun, transform.position + Vector3.up * 0.1f, true);
                    AudioManager.instance.PlaySound(AudioTag.playerFall, gameObject, 1);
                }
                Utils.EmitSound(_isRunning ? _soundRadiusRun : _soundRadiusWalk, transform.position + Vector3.up * 0.1f, true);
                float speedRatio = Speed / _moveSpeed;
                float stepDelay = 1 / (stepPerSec * speedRatio);
                if(walkTimer >= stepDelay) {
                    walkTimer -= stepDelay;
                    AudioManager.instance.PlaySound(AudioTag.playerWalkGrass, gameObject, speedRatio);
                }
                walkTimer += deltaTime;
            }
            else if (_isOnClimbWall)
            {
                float speedRatio = Speed / _moveSpeed;
                float stepDelay = 1 / (climbingNoisePerSec * speedRatio);
                if (walkTimer >= stepDelay)
                {
                    walkTimer -= stepDelay;
                    AudioManager.instance.PlaySound(AudioTag.playerClimb, gameObject, speedRatio);
                }
                walkTimer += deltaTime;
            }
        }
        else
        {
            AudioManager.instance.StopAudio(AudioTag.playerWalkGrass, gameObject);
        }
        if (!_charaCtrl.isGrounded && _wasGrounded)
        {
            AudioManager.instance.StopAudio(AudioTag.playerWalkGrass, gameObject);
        }
    }
    void HandleAnimations() {
        if(_startJumping) {
            _jumpBasePos = transform.position;
        }
        if(_isInJump) {
            if(transform.position.y < _jumpBasePos.y - 1) {
                _isInJump = false;
                _isFalling = true;
            }
        }

        _anim.SetBool("isAlive", true);
        //float speed = _movement.Multiply(new Vector3(1, 0, 1)).magnitude / Speed;
        //Debug.Log($"{_movement.Multiply(new Vector3(1, 0, 1)).magnitude}/{Speed} = {speed}");
        _anim.SetFloat("SpeedX", motion.x);
        _anim.SetFloat("SpeedY", motion.y);
        _anim.SetFloat("SpeedXZ", motion.Flatten().magnitude);
        _anim.SetBool("isSprinting", _isRunning);
        //_anim.SetFloat("ClimbSpeed", _movement.magnitude / ClimbSpeed);
        _anim.SetBool("startJumping", _startJumping);
        _anim.SetBool("isJumping", _isInJump);
        _anim.SetBool("stopJumping", _stopJumping);
        _anim.SetBool("isFalling", _isFalling);
        _anim.SetBool("isClimbing", _isOnClimbWall);

        // debug
        if(_startJumping) _startJumping = false;
        if(_stopJumping) {
            _stopJumping = false;
            _isInJump = false;
        } 
    }

    #region MOVEMENT
    /// <summary>
    /// Return inputs as such (x: horizontal, y: jumpStrength, z: vertical).
    /// </summary>
    /// <returns></returns>
    Vector3 GetInputs() {
        Vector2 inputs = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        //inputs = Utils.CartToPolar(inputs);
        return new Vector3(inputs.x, 0, inputs.y).normalized;
    }
    Vector3 motion;
    Vector3 Move() {
        Vector3 flatInputs = new Vector3(_inputs.x, 0, _inputs.z);
        //Debug.Log(flatInputs);
        if(_wasClimbing) {
            _movement = Vector3.zero;
            move = Vector3.zero;
            transform.localRotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, 0);
        }
        if(_isAiming) transform.SlerpRotation(new Vector3(_playerCam.transform.forward.x, 0, _playerCam.transform.forward.z), transform.up, RotationSpeed);
        else if(flatInputs.magnitude > 0) transform.SlerpRotation(_playerCam.transform.TransformDirection(flatInputs), transform.up, RotationSpeed);

        if (advancedMovement) {
            // jump
            if (_charaCtrl.isGrounded && _isJumping)
            {
                AudioManager.instance.PlaySound(AudioTag.playerJump, gameObject);
                move.y = _jumpHeight;
            }
            else if (_charaCtrl.isGrounded) move.y = -_groundPullMagnitude;
            else move.y += Physics.gravity.y * deltaTime;
            // move
            if(flatInputs.x > 0) {
                move.x += Mathf.Sign(flatInputs.x) * (move.x < 0 ? Deceleration : Acceleration) * deltaTime;
            } else if(flatInputs.x < 0) {
                move.x += Mathf.Sign(flatInputs.x) * (move.x > 0 ? Deceleration : Acceleration) * deltaTime;
            } else {
                move.x += -Mathf.Sign(move.x) * Mathf.Clamp((_charaCtrl.isGrounded ? 1 : 0.1f) * Deceleration * deltaTime, 0, Mathf.Abs(move.x));
            }
            if(flatInputs.z > 0) {
                move.z += Mathf.Sign(flatInputs.z) * (move.z < 0 ? Deceleration : Acceleration) * deltaTime;
            } else if(flatInputs.z < 0) {
                move.z += Mathf.Sign(flatInputs.z) * (move.z > 0 ? Deceleration : Acceleration) * deltaTime;
            } else if(_charaCtrl.isGrounded) {
                move.z += -Mathf.Sign(move.z) * Mathf.Clamp((_charaCtrl.isGrounded ? 1 : 0.1f) * Deceleration * deltaTime, 0, Mathf.Abs(move.z));
            }
            Vector3 moveTemp = move;
            moveTemp.y = 0;
            moveTemp = Vector3.ClampMagnitude(moveTemp, Speed);
            move.x = moveTemp.x;
            move.z = moveTemp.z;
            // full motion
            return _playerCam.transform.TransformDirection(move);
        } else {
            //Vector3 motion;
            //Vector3 slopeVector = GetSlopeVector();
            //bool inSlopeLimit = Vector3.Angle(Vector3.up, slopeVector) - 90 < _charaCtrl.slopeLimit;
            Vector3 slopeNormal = GetSlopeNormal();
            //float slopeAngle = Vector3.Angle(Vector3.up, slopeNormal);
            bool inSlopeLimit = Vector3.Angle(Vector3.up, slopeNormal) < _charaCtrl.slopeLimit;
            //Debug.Log($"slope angle: {slopeAngle} deg");
            if(_charaCtrl.isGrounded) {
                motion = new Vector3(_inputs.x * Speed, _movement.y, _inputs.z * Speed);
                _isFalling = false;
                if(_isInJump) {
                    _stopJumping = true;
                }
                if(inSlopeLimit && _isJumping)
                {
                    AudioManager.instance.PlaySound(AudioTag.playerJump, gameObject);
                    motion.y = _jumpHeight;
                    _isInJump = true;
                    _startJumping = true;
                }
                _movement = _playerCam.transform.TransformDirection(motion);

                if(!inSlopeLimit) {
                    //Debug.Log($"Slope too steep! ({Vector3.Angle(Vector3.up, slopeNormal)}deg)");
                    //_movement += slopeVector * Vector3.Dot(slopeVector, Physics.gravity) * deltaTime;

                    //Vector3 slideVector = new Vector3(slopeNormal.x, 0, slopeNormal.z);
                    //_movement += slideVector * _slideAcceleration * deltaTime;
                }
            } else {
                //if(_wasGrounded && _movement.y < -0.5f) _movement.y = -0.5f;
                motion = flatInputs * Speed * _inAirMoveRatio * Time.deltaTime;
                _movement += -_charaCtrl.velocity.Flatten().normalized * Mathf.Clamp(_airDrag * deltaTime, 0, _charaCtrl.velocity.Flatten().magnitude);
                _movement += _playerCam.transform.TransformDirection(motion);
                if(_wasGrounded && _movement.y < 0) _movement.y = 0;
                else _movement += Physics.gravity * deltaTime;
            }
            _movement += GetFrictionVector() * deltaTime;
            return _movement;

        }
    }
    Vector3 GetSlopeVector() {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        Vector3 rayDir = -transform.up * _charaCtrl.skinWidth * 2;
        int layerMaskTerrain = 1 << LayerMask.NameToLayer(Utils.l_Terrain);
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
        int layerMaskTerrain = Utils.l_Terrain.ToLayerMask();
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskTerrain)) {
            // calculate downward slope vector
            //Debug.DrawLine(rayOrigin, hit.point, Color.green);
            return hit.normal;
        }
        //Debug.DrawRay(rayOrigin, rayDir, Color.red);
        return Vector3.up;
    }
    Vector3 GetFrictionVector() {
        Vector3 friction = Vector3.zero;
        for(int i = 0; i < ccHits.Count; i++) {
            friction += Vector3.Project(ccHits[i].normal * _charaCtrl.velocity.magnitude, _charaCtrl.velocity);
        }
        return friction;
    }
    #endregion
    #region CLIMBING
    Vector3 Climb() {
        _isInJump = false;
        _isFalling = false;
        // check for terrain under character
        bool isGrounded = CheckIfGrounded();
        // check if character can declimb on a ledge
        Vector3 declimbDir = _inputs.x != 0 ? (Vector3.right * _inputs.x).normalized : Vector3.forward;
        if((_inputs.z > 0 || _inputs.x != 0) && FindDeclimbHitPoint(declimbDir, out _declimbHit)) {
            _declimbPoint1 = FindDeclimbPoint1();
            _isOnClimbWall = false;
            _isDeclimbingUp = true;
            _declimbPart1 = true;
            return Vector3.zero;
        }
        _wallPoint = FindClosestWallPoint(BodyCenter, Vector3.zero, 90, 1);
        Vector3 wallDir = _wallPoint - BodyCenter;
        Vector3 dirToOffsettedPoint = wallDir - wallDir.normalized * (Radius + _wallDistanceOffset);
        //Vector3 normalY = new Vector3(0, (transform.TransformDirection(-hit.normal) * wallDir.magnitude).y, 0);
        if(!(isGrounded && _inputs.z < 0)) {
            if(Vector3.Angle(transform.forward, wallDir) != 0) transform.LookAt(transform.position + wallDir, Vector3.up);
            if(dirToOffsettedPoint != Vector3.zero) transform.LerpPosition(transform.position + dirToOffsettedPoint, _moveSpeed);
        }

        // movement
        _movement = new Vector3(_inputs.x, _inputs.z, 0) * ClimbSpeed;
        motion = new Vector3(_inputs.x, _inputs.z, 0);
        if(isGrounded && _inputs.z < 0) {
            _movement = new Vector3(_inputs.x, -_groundPullMagnitude, _inputs.z) * Speed;
        }
        bool canMoveHori = true;// CanClimbHorizontal(_inputs.x);
        bool canMoveVert = CanClimbVertical(_inputs.z);
        if(!canMoveHori) _movement.x = 0;
        if(!canMoveVert) _movement.y = 0;
        // jumping
        if(_isJumping) {
            if (_inputs.z < 0) {
                _movement = new Vector3(_inputs.x * Speed, _jumpHeight * 0.5f, _inputs.z * Speed * 0.5f);
            }
        }
        // result
        return transform.TransformDirection(_movement);
    }
    void Declimb() {
        transform.SlerpRotation((_declimbHit.point - transform.position).Flatten(), Vector3.up, RotationSpeed);
        if(_declimbPart1) {
            // transform.pos to up
            if(transform.LerpPosition(_declimbPoint1, _climbSpeed)) {
                _declimbPart1 = false;
            }
        }
        if(!_declimbPart1) {
            if(transform.LerpPosition(_declimbHit.point, _moveSpeed)) {
                ResetDeclimb();
            }
        }
    }
    Vector3 FindDeclimbPoint1() {
        Vector3 vHit = _declimbHit.point - transform.position;
        float upMagn = vHit.magnitude * Mathf.Sin(Vector3.Angle(vHit, transform.forward) * Mathf.Deg2Rad);
        return transform.position + transform.up * upMagn;
    }
    void ResetDeclimb() {
        _isDeclimbingUp = false;
    }
    bool CheckForClimb() {
        if(!_wasClimbing) {
            return (CheckForClimbForward(BodyCenter) || CheckForClimbForward(BodyCenter, _climbSideAngle) || CheckForClimbForward(BodyCenter, -_climbSideAngle));
            //&& CanClimbHorizontal(-1) && CanClimbHorizontal(1); 
        } else {
            return CheckForClimbForward(BodyCenter);// || CheckForClimbForward(FeetPos); // && !CanDeclimbUp();
        }
    }
    bool CheckIfGrounded() {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + transform.up * _charaCtrl.skinWidth * 2;
        Vector3 rayDir = -transform.up * _charaCtrl.skinWidth * 4;
        int layerMaskTerrain = Utils.l_Terrain.ToLayerMask();
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskTerrain)) {
            // there is terrain under our feet
            Debug.DrawLine(rayOrigin, hit.point, Color.red);
            return true;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.green);
        return false;
    }
    Vector3 FindClosestWallPoint(Vector3 origin, Vector3 offset, float maxAngle = 90, float stepAngle = 1) {
        RaycastHit hit;
        Vector3 rayOrigin = origin + transform.TransformDirection(offset);
        Vector3 centerDir = transform.forward * (Radius + _wallDistanceOffset);
        Vector3 hitPoint = rayOrigin + centerDir * 1.5f;
        int layerMask = Utils.l_Terrain.ToLayerMask();
        for(float i = -maxAngle; i <= maxAngle; i += stepAngle) {
            Quaternion iRot = Quaternion.Euler(0, i, 0);
            if(Physics.Raycast(rayOrigin, iRot * centerDir.normalized, out hit, (hitPoint - rayOrigin).magnitude, layerMask)) {
                Debug.DrawLine(origin, hit.point, Color.red, 1);
                hitPoint = hit.point;
            }
        }
        return hitPoint;
    }
    bool FindDeclimbHitPoint(Vector3 localDir, out RaycastHit hitPoint) {
        Vector3 rayOrigin = BodyCenter;
        Vector3 rayDir = transform.up * (Height * 0.5f + _climbCheckDistanceOffset * 1.1f);
        int layerMaskWall = Utils.l_Terrain.ToLayerMask();
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hitPoint, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "forward" direction
            Debug.DrawLine(rayOrigin, hitPoint.point, Color.yellow);
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.yellow);

        rayOrigin += rayDir;
        rayDir = transform.TransformDirection(localDir.normalized) * (Radius + _wallDistanceOffset) * 2;
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hitPoint, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "forward" direction
            Debug.DrawLine(rayOrigin, hitPoint.point, Color.yellow);
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.yellow);
        rayOrigin += rayDir;
        rayDir = -transform.up * (Height * 0.5f + _climbCheckDistanceOffset * 2);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hitPoint, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "forward -> down" direction
            Debug.DrawLine(rayOrigin, hitPoint.point, Color.yellow);
            return true;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.yellow);
        return false;
    }
    bool CheckForClimbForward(Vector3 rayOrigin, float angleY = 0) {
        RaycastHit hit;
        //Vector3 rayOrigin = _bodyCenter.position;
        Vector3 rayDir = Quaternion.Euler(0, angleY, 0) * transform.forward * (Radius + _wallDistanceOffset);
        int layerMaskClimbZone = Utils.l_Environment.ToLayerMask();
        int layerMaskWall = Utils.l_Terrain.ToLayerMask();
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "forward" direction
            if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskClimbZone)) {
                // this surface is NOT a climbable wall
                if (hit.collider.CompareTag("tree"))
                {
                    return true;
                }
            }
        }
        return false;
    }
    bool CanClimbVertical(float vInput) {
        if(vInput == 0) return false;
        RaycastHit hit;
        Vector3 rayOrigin = BodyCenter;
        Vector3 rayDir = Mathf.Sign(vInput) * transform.up * (Height * 0.5f + _climbCheckDistanceOffset);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer(Utils.l_Environment);
        int layerMaskWall = 1 << LayerMask.NameToLayer(Utils.l_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.blue);
            return vInput < 0;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.blue);

        rayOrigin += rayDir;
        rayDir = transform.forward * (Radius + _wallDistanceOffset * 4);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down -> forward" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.red);
            if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskClimbZone)) {
                // this surface is climbable wall
                if (hit.collider.CompareTag("tree"))
                {
                    return true;
                }
            }
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.red);

        rayOrigin += rayDir;
        rayDir = Mathf.Sign(-vInput) * transform.up * (Height + _climbCheckDistanceOffset);
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
        Vector3 rayOrigin = BodyCenter;
        Vector3 rayDir = Mathf.Sign(hInput) * transform.right * (Radius + _climbCheckDistanceOffset);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer(Utils.l_Environment);
        int layerMaskWall = 1 << LayerMask.NameToLayer(Utils.l_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "right/left" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.blue);
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.blue);

        rayOrigin += rayDir;
        rayDir = transform.forward * (Radius + _wallDistanceOffset * 2);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "right/left -> forward" direction
            if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskClimbZone)) {
                // this surface is climbable wall
                Debug.DrawLine(rayOrigin, hit.point, Color.red);
                if (hit.collider.CompareTag("tree"))
                {
                    return true;
                }
            }
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.red);

        rayOrigin += rayDir;
        rayDir = Mathf.Sign(-hInput) * transform.right * (_climbCheckDistanceOffset);
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
        int layer = Utils.l_Terrain.ToLayerMask() | Utils.l_Interactibles.ToLayerMask();
        if (Physics.Raycast(_playerCam.transform.position, _playerCam.CamForward, out hit, interactionRange, layer))
        {
            if (hit.collider.TryGetComponent(out Root root))
            {
                return root;
            }
        }
        return null;
    }
    BombPlant CheckPlantInteraction()
    {
        RaycastHit hit;
        int layer = Utils.l_Terrain.ToLayerMask() | Utils.l_Interactibles.ToLayerMask();
        if (Physics.Raycast(_playerCam.transform.position, _playerCam.CamForward, out hit, interactionRange, layer))
        {
            if (hit.collider.TryGetComponent(out BombPlant bp)){
                if (bp._gotABomb){
                    return bp;
                }
            }
        }
        return null;
    }
    void GetBomb()
    {
        _fakebomb.enabled = true;
        _hasBomb = true;
    }
    public void PushOut(Vector3 direction, float strength) {
        AudioManager.instance.PlaySound(AudioTag.playerGetsPushed, gameObject);
        _movement = direction.normalized * strength;
        _wasPushed = true;
    }
    #endregion

    public void HideMeshes(bool hide) {
        if(_isModelHidden == hide) return;
        _isModelHidden = hide;
        for(int i = 0; i < _renderers.Length; i++) {
            _renderers[i].enabled = !hide;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        ccHits.Add(hit);
    }

    private void OnTriggerStay(Collider other) {
        if (other.TryGetComponent(out TunnelEntrance tEntrance)) {
            _playerCam.maxZoomRatio = tEntrance.GetCamLerpRatio(transform.position, true);
        }
    }
    private void OnTriggerExit(Collider other) {
        if(other.TryGetComponent(out TunnelEntrance tEntrance)) {
            _playerCam.maxZoomRatio = tEntrance.GetCamLerpRatio(transform.position, true);
        }
    }

    private void OnDrawGizmosSelected() {
        if(!debugDraws) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _soundRadiusWalk);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _soundRadiusRun);
    }
}