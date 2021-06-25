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
    public float AirSpeed {
        get {
            return _moveSpeed * (_isRunning ? AirSprintRatio : 1);
        }
    }
    public float RotationSpeed {
        get {
            return _rotationSpeed * 360;
        }
    }
    public bool canJump = true;
    float AirSprintRatio {
        get {
            return 1 + (_sprintRatio - 1) * _airSprintRatio;
        }
    }
    float Radius {
        get { return _charaCtrl.radius + _charaCtrl.skinWidth; }
    }
    float Height {
        get { return _charaCtrl.height + _charaCtrl.skinWidth * 2; }
    }
    float BodyCenterUp {
        get { return (_charaCtrl.height * 0.5f + _charaCtrl.skinWidth * 2); }
    }
    Vector3 BodyCenter {
        get { return transform.position + transform.up * BodyCenterUp; }
    }
    Vector3 FlatForward {
        get { return transform.forward.Flatten().normalized; }
    }
    Vector3 ClimbCheckVectorTrigger {
        get { return transform.forward * (Radius + _wallCheckDistance); }
    }
    float ClimbCheckVectorWallLength {
        get { return ClimbCheckVectorTrigger.magnitude * 1.5f; }
    }
    bool CanInteract {
        get { return !_isOnClimbWall && !_hasBomb; }
    }
    bool CanThrow {
        get { return _isAiming && _hasBomb; }
    }
    [HideInInspector] public bool isAlive = true;
    public Transform head;

    [SerializeField] Transform _camCenter, _throwPoint, _rightHand;
    [SerializeField] bool advancedMovement = false, debugDraws = false;
    [SerializeField] float _accelerationTime = 0.5f, _decelerationTime = 0.2f, _airDrag = 0.5f;
    [SerializeField] float _moveSpeed = 5, _sprintRatio = 1.5f, _airSprintRatio = 0.5f, _climbSpeed = 2, _rotationSpeed = 20, _jumpHeight = 5;
    [SerializeField] float _inAirMoveRatio = 1, _groundPullMagnitude = 0.5f;
    [SerializeField] float _wallCheckDistance = 0.1f;
    [Range(0, 0.99f)] [SerializeField] float _climbWallDistanceOffsetRatio = 0.5f;
    [SerializeField] float _climbMoveDistanceCheck = 0.2f;
    [Range(0, 89)][SerializeField] float _climbSideAngle = 40;
    [SerializeField] float _soundRadiusRun = 10f, _soundRadiusWalk = 5f;
    [SerializeField] float interactionRange = 3f;
    [SerializeField] MeshRenderer _fakebomb; //temporary, but too late to remove I believe
    CharacterController _charaCtrl;
    PlayerIKController _ikCtrl;
    PlayerCamera _playerCam;
    TrajectoryHandler _trajectoryHandler;
    Animator _anim;
    Renderer[] _renderers;
    bool _isModelHidden = false;
    Vector3 _movement = Vector3.zero, _inputs = Vector3.zero, _declimbPoint1, _declimbInputMask, _declimbBasePos, _declimbBaseDir;
    RaycastHit _declimbHit, _tempHit;
    bool _wasGrounded = true, _wasClimbing = false, _wasPushed = false;
    bool _isOnClimbWall = false, _isDeclimbingUp = false, _declimbPart1 = true, _declimbCancel = false;
    bool _isAiming = false, _isThrowing = false, _hasBomb = false, _isInteracting = false, _isJumping = false, _isRunning = false;
    float deltaTime;
    BombPlant lastPlant;
    Vector3 move = Vector3.zero, _climbPosOffset;
    Transform helper;
    List<ControllerColliderHit> ccHits = new List<ControllerColliderHit>();

    private void Awake() {
        _charaCtrl = GetComponent<CharacterController>();
        _ikCtrl = GetComponent<PlayerIKController>();
        _playerCam = GetComponentInChildren<PlayerCamera>();
        _trajectoryHandler = GetComponentInChildren<TrajectoryHandler>();
        if(_trajectoryHandler != null) _trajectoryHandler.playerCamera = _playerCam;
        _anim = GetComponentInChildren<Animator>();
        _anim.SetBool("isAlive", true);
        //_renderers = _model.GetComponentsInChildren<Renderer>();
    }
    private void Start() {
        _playerCam.SetReferences(this, head,_camCenter);
        helper = new GameObject("Helper_" + name).transform;
    }
    private void Update() {
        if(GameManager.Instance.GamePaused || !isAlive) return;

        deltaTime = Time.deltaTime;
        _isJumping = Input.GetKey(KeyCode.Space) && canJump;
        _isInteracting = Input.GetKeyDown(KeyCode.E);
        //if(Input.GetKeyDown(KeyCode.Alpha1)) _isAiming = _hasBomb ? !_isAiming : false;
        //_isThrowing = Input.GetKeyDown(KeyCode.Alpha2);
        _isAiming = Input.GetMouseButton(1);
        _isThrowing = Input.GetMouseButtonDown(0);
        _isRunning = Input.GetKey(KeyCode.LeftShift) && !_hasBomb;
        _inputs = GetInputs();
        bool wasGrounded = _charaCtrl.isGrounded;

        if (!canJump && Input.GetKeyUp(KeyCode.Space)) canJump = true;

        Look();
        HandleMovement();
        HandleInteractions();
        HandleThrowing();
        HandleSound();
        HandleAnimations();

        _wasClimbing = _isOnClimbWall;
        _wasGrounded = wasGrounded;
    }
    private void LateUpdate() {
        ccHits.Clear();
        _wasPushed = false;
    }
    #region GENERAL
    public void Die() {
        isAlive = false; // stop player controls
        GameManager.Instance.disablePauseToggle = true;
        _anim.SetBool("isAlive", false); // play anim Death
        _anim.SetTrigger("Die");
        AudioManager.instance.PlaySound(AudioTag.playerDeath, gameObject, 1);
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
            _climbPosOffset = Vector3.zero;
            if(!_wasPushed) {
                _movement = _isOnClimbWall ? Climb() : Move();
            }
            //if(chaosOn) transform.position -= transform.forward;
            _charaCtrl.Move(_movement * deltaTime + _climbPosOffset);
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
            if (root.isInteractable) root.ShowOutline(true);
            if(_isInteracting && root.isInteractable) root.Open();
        }
        //UIManager.Instance?.rootIndicator.SetActive(root != null);

        // Plants
        BombPlant bombPlant;
        if ((bombPlant = CheckPlantInteraction()) != null) {
            if (bombPlant.gotABomb) bombPlant.ShowOutline(true);
            if (_isInteracting && !_hasBomb) { 
                bombPlant.PickBomb();
                PlayAnimPickup();
                _hasBomb = true;
                _fakebomb.enabled = true;
                lastPlant = bombPlant;
                AudioManager.instance?.PlaySound(AudioTag.fruitBombTaken, 1);
            }
        }
        //UIManager.Instance?.bombIndicator.SetActive(bombPlant != null);
    }
    void HandleThrowing() {
        if(_trajectoryHandler == null) return;

        _trajectoryHandler.IsDisplaying = CanThrow;
        if (CanThrow){
            _trajectoryHandler.SetBombTrajectory();
            if (_isThrowing){
                _fakebomb.enabled = false;
                _trajectoryHandler.ThrowBomb();
                PlayAnimThrow();
                lastPlant.GrowBomb();
                _hasBomb = false;
                _trajectoryHandler.IsDisplaying = false;
            }
        } 
        
    }
    float walkTimer, stepPerSec = 2, climbingNoisePerSec = 0.5f;
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
        if(!_isDeclimbingUp) {
            _anim.SetBool("hasDeclimbedUp", true);
            AdjustAnimSpeed(_isOnClimbWall || _charaCtrl.isGrounded ? Speed / _moveSpeed : 1);
        }
        _anim.SetBool("isSprinting", _isRunning);
        _anim.SetFloat("MoveSpeed", motion.Flatten().magnitude / Speed);

        
        if(CheckEndFalling()) {
            _anim.SetBool("isJumping", false);
            _anim.SetBool("isFalling", false);
        } else if(CheckIsFalling()) {
            //Debug.Log($"Velocity: {_charaCtrl.velocity}");
            _anim.SetBool("isJumping", false);
            _anim.SetBool("isFalling", true);
        }

        _anim.SetBool("isClimbing", _isOnClimbWall);
        _anim.SetFloat("ClimbSpeedX", motion.x);
        _anim.SetFloat("ClimbSpeedY", motion.y);
        if(_isOnClimbWall && !_wasClimbing) {
            //PlayAnimClimb();
        }
    }
    #endregion
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
        Vector3 flatInputs = _inputs.Flatten();
        //Debug.Log(flatInputs);
        if(_wasClimbing) {
            //Debug.LogError("climbing LOST");
            _movement = Vector3.zero;
            move = Vector3.zero;
            transform.localRotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, 0);
        }
        if(flatInputs.magnitude > 0) transform.SlerpRotation(_playerCam.transform.TransformDirection(flatInputs), transform.up, RotationSpeed);
        else if(_isAiming && _charaCtrl.isGrounded) transform.SlerpRotation(_playerCam.transform.forward.Flatten(), transform.up, RotationSpeed);
        else if(!_charaCtrl.isGrounded) transform.SlerpRotation(_movement.Flatten(), transform.up, RotationSpeed);

        if (!advancedMovement) {
            //Vector3 motion;
            //Vector3 slopeVector = GetSlopeVector();
            //bool inSlopeLimit = Vector3.Angle(Vector3.up, slopeVector) - 90 < _charaCtrl.slopeLimit;
            Vector3 slopeNormal = GetSlopeNormal();
            //float slopeAngle = Vector3.Angle(Vector3.up, slopeNormal);
            bool inSlopeLimit = Vector3.Angle(Vector3.up, slopeNormal) < _charaCtrl.slopeLimit;
            //Debug.Log($"slope angle: {slopeAngle} deg");
            if(_charaCtrl.isGrounded) {
                //motion = new Vector3(_inputs.x * Speed, _movement.y, _inputs.z * Speed);
                motion = new Vector3(_inputs.x * Speed, -_groundPullMagnitude, _inputs.z * Speed);
                if(_wasGrounded) {
                    //motion.y = -_groundPullMagnitude;
                }
                if(inSlopeLimit && _isJumping) {
                    AudioManager.instance.PlaySound(AudioTag.playerJump, gameObject);
                    motion.y = _jumpHeight * (_isRunning ? AirSprintRatio : 1);
                    OnStartJumping();
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
                motion = flatInputs * _moveSpeed * _inAirMoveRatio * Time.deltaTime;
                _movement += -_charaCtrl.velocity.Flatten().normalized * Mathf.Clamp(_airDrag * deltaTime, 0, _charaCtrl.velocity.Flatten().magnitude);
                _movement += _playerCam.transform.TransformDirection(motion);
                //Debug.Log($"IN AIR: WasGrounded = {_wasGrounded} || Movement.y = {_movement.y}");
                if(_wasGrounded && _movement.y < 0) {
                    _movement.y = 0;
                }
                else _movement += Physics.gravity * deltaTime;
                //Debug.Log(">>> Movement.y = " + _movement.y);
            }
            _movement += GetFrictionVector() * deltaTime;
            return _movement;
        } else {
            // jump
            if(_charaCtrl.isGrounded && _isJumping) {
                AudioManager.instance.PlaySound(AudioTag.playerJump, gameObject);
                move.y = _jumpHeight;
            } else if(_charaCtrl.isGrounded) move.y = -_groundPullMagnitude;
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
    void OnStartJumping() {
        _anim.SetBool("isJumping", true);
        PlayAnimJump(); 
    }
    #endregion
    #region CLIMBING
    Vector3 Climb() {
        // check for terrain under character
        bool isGrounded = CheckIfGrounded();
        // check if character can declimb on a ledge
        if(CheckForDeclimb()) {
            //Debug.Log(">>> Start declimbing");
            return Vector3.zero;
        }
        if(!_wasClimbing) {
            //Debug.LogError("climbing RETRIEVED");
        }
        RaycastHit hit;
        if(!_wasClimbing ? FindClosestWallPoint(BodyCenter, ClimbCheckVectorTrigger, out hit, 90, 1) : FindWallPoint(BodyCenter, ClimbCheckVectorTrigger, out hit)) {
            //Debug.Log(">>> WallPoint found at ");
            //Debug.DrawLine(BodyCenter, hit.point, Color.red, 0.1f);
            //_wallPoint = hit.point;
            //Debug.Log("Angle w/ normal: " + Vector3.Angle(transform.forward, -hit.normal));
            //Debug.DrawRay(BodyCenter, ClimbCheckVector, Color.red);
            //Debug.DrawRay(hit.point, -hit.normal, Color.blue);
            helper.position = transform.position;
            helper.rotation = transform.rotation;
            if(Vector3.Angle(transform.forward, -hit.normal) != 0) {
                //Debug.Log($"Repositioning...");
                //transform.LookAt(transform.position - hit.normal, Vector3.up);
                //Debug.Log($"Normal: {-hit.normal} || Angle: {Vector3.Angle(transform.forward, -hit.normal)}");
                helper.position = BodyCenter;
                Quaternion rot = Quaternion.FromToRotation(transform.forward, -hit.normal);
                helper.RotateAround(hit.point, rot); 
                helper.position -= helper.up * Vector3.Distance(transform.position, BodyCenter);
                transform.rotation = helper.rotation;

                //transform.RotateAround(BodyCenter, Quaternion.FromToRotation(transform.forward, -hit.normal));
            }
            if(!isGrounded || !(_inputs.z < 0)) {
                Vector3 wallDir = hit.point - (helper.position + helper.up * BodyCenterUp);
                Vector3 dirToOffsettedPoint = wallDir - wallDir.normalized * (Radius + _wallCheckDistance * _climbWallDistanceOffsetRatio);
                if(dirToOffsettedPoint != Vector3.zero) helper.position += dirToOffsettedPoint;
                //transform.SlerpRotation(-hit.normal, Vector3.up, RotationSpeed);
            }
            _climbPosOffset = helper.position - transform.position;
            Debug.DrawRay(helper.position + helper.up * BodyCenterUp, ClimbCheckVectorTrigger, Color.green);
            Debug.DrawRay(hit.point, Vector3.Cross(helper.up, -hit.normal), Color.blue); // perpendicualr to hit point
        }

        // movement
        _movement = new Vector3(_inputs.x, _inputs.z, 0) * ClimbSpeed;
        motion = new Vector3(_inputs.x, _inputs.z, 0);
        if(isGrounded && _inputs.z < 0) {
            _movement = new Vector3(_inputs.x, -_groundPullMagnitude, _inputs.z) * Speed;
        }
        //if(!canMoveHori) _movement.x = 0;
        if(_inputs.z > 0 && !CanClimbVertical(_inputs.z)) _movement.y = 0;
        // jumping
        if(_isJumping) {
            if (_inputs.z < 0) {
                _movement = new Vector3(_inputs.x * Speed, 0, _jumpHeight * -0.5f);
                transform.rotation = Quaternion.LookRotation(-FlatForward, Vector3.up); // turn player on the opposite direction
                OnStartJumping();
            }
        }
        // result
        return transform.TransformDirection(_movement);
    }
    bool CheckForDeclimb() {
        bool declimb = false;
        Vector3 dirX = (Vector3.right * _inputs.x).normalized, dirZ = Vector3.forward;
        // check for declimb on local axis X (left or right)
        if(_inputs.x != 0 && FindDeclimbHitPoint(dirX, out _declimbHit)) {
            _declimbInputMask = dirX;
            declimb = true;
        }
        // if no declimb point was found, check for declimb on local axis Z (forward)
        else if(_inputs.z > 0 && FindDeclimbHitPoint(dirZ, out _declimbHit)) {
            _declimbInputMask = dirZ;
            declimb = true;
        }
        if(declimb) {
            _declimbBasePos = transform.position;
            _declimbBaseDir = transform.forward;
            _declimbCancel = false;
            _declimbPoint1 = FindDeclimbPoint1();
            _isDeclimbingUp = true;
            _declimbPart1 = true;
            PlayAnimDeclimbUp();
            return true;
        }
        return false;
    }
    void Declimb() {
        // check for correct input to decide if declimb yes or no
        _declimbCancel = _declimbPart1 && (_inputs.Multiply(Utils.Abs(_declimbInputMask)).normalized != _declimbInputMask);
        InvertAnimDeclimbUp(_declimbCancel);

        if(_declimbCancel) {
            // go back to previous pos and orientation
            transform.SlerpRotation(_declimbBaseDir, Vector3.up, RotationSpeed);
            if(transform.LerpPosition(_declimbBasePos, _climbSpeed)) {
                StopAnimDeclimbUp();
                ResetDeclimb();
            }
        } else {
            // proceed to declimb pos and orientation
            transform.SlerpRotation((_declimbHit.point - transform.position).Flatten(), Vector3.up, RotationSpeed);
            if(_declimbPart1) {
                // transform.pos to up
                if(transform.LerpPosition(_declimbPoint1, _climbSpeed)) {
                    _isOnClimbWall = false;
                    _declimbPart1 = false;
                    StopAnimDeclimbUp();
                }
            }
            if(!_declimbPart1) {
                if(transform.LerpPosition(_declimbHit.point, _moveSpeed)) {
                    ResetDeclimb();
                }
            }
        }
    }
    Vector3 FindDeclimbPoint1() {
        Vector3 vHit = _declimbHit.point - transform.position;
        float upMagn = vHit.magnitude * Mathf.Sin(Vector3.Angle(vHit, FlatForward) * Mathf.Deg2Rad);
        return transform.position + Vector3.up * upMagn;
    }
    void ResetDeclimb() {
        _isDeclimbingUp = false;
        //Debug.Log("<<< Stop Declimbing");
    }
    bool CheckForClimb() {
        if(_hasBomb) return false;
        //return (CheckForClimbForward(BodyCenter) || CheckForClimbForward(BodyCenter, _climbSideAngle) || CheckForClimbForward(BodyCenter, -_climbSideAngle));
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
    bool FindClosestWallPoint(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxAngle = 90, float stepAngle = 1) {
        hit = new RaycastHit();
        RaycastHit wallHit;
        bool wallFound = false;
        Vector3 rayOrigin = origin;
        Vector3 rayDir = direction.normalized * (Radius + _wallCheckDistance);
        Vector3 hitPoint = rayOrigin + rayDir * 1.1f;
        for(float i = -maxAngle; i <= maxAngle; i += stepAngle) {
            Quaternion iRot = Quaternion.Euler(0, i, 0);
            if(FindWallPoint(rayOrigin, iRot * (rayDir.normalized * (hitPoint - rayOrigin).magnitude), out wallHit)) {
                hitPoint = wallHit.point;
                hit = wallHit;
                wallFound = true;
            }
        }
        //Debug.Log("Wall found: " + wallFound);
        return wallFound;
    }
    bool FindWallPoint(Vector3 origin, Vector3 direction, out RaycastHit hit) {
        if(Physics.Raycast(origin, direction.normalized, out hit, ClimbCheckVectorWallLength, Utils.l_Terrain.ToLayerMask())) {
            return Physics.Raycast(origin, direction.normalized, out hit, direction.magnitude, Utils.l_Environment.ToLayerMask());
        }
        return false;
    }
    bool FindDeclimbHitPoint(Vector3 localDir, out RaycastHit hit) {
        Vector3 rayOrigin = BodyCenter;
        Vector3 rayDir = transform.up * (Height * 0.5f + _climbMoveDistanceCheck * 1.5f); // pu a bigger multiplier like 1.5f
        int layerMaskTerrain = Utils.l_Terrain.ToLayerMask();
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskTerrain)) {
            // a surface blocks the "forward" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.yellow);
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.yellow);

        rayOrigin += rayDir;
        rayDir = transform.TransformDirection(localDir.normalized) * (Radius + _wallCheckDistance) * 2;
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskTerrain)) {
            // a surface blocks the "forward" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.yellow);
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.yellow);
        rayOrigin += rayDir;
        rayDir = -transform.up * (Height + _climbMoveDistanceCheck);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskTerrain)) {
            // a surface blocks the "forward -> down" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.yellow);
            return true;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.yellow);
        return false;
    }
    bool CheckForClimbForward(Vector3 rayOrigin, float angleY = 0) {
        Vector3 rayDir = Quaternion.Euler(0, angleY, 0) * ClimbCheckVectorTrigger;
        int layerMaskClimbZone = Utils.l_Environment.ToLayerMask();
        int layerMaskWall = Utils.l_Terrain.ToLayerMask();
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out _tempHit, ClimbCheckVectorWallLength, layerMaskWall)) {
            // a surface blocks the "forward" direction
            if(Physics.Raycast(rayOrigin, rayDir.normalized, out _tempHit, rayDir.magnitude, layerMaskClimbZone)) {
                // this surface is NOT a climbable wall
                if (_tempHit.collider.CompareTag("tree"))
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
        Vector3 rayDir = Mathf.Sign(vInput) * transform.up * (Height * 0.5f + _climbMoveDistanceCheck);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer(Utils.l_Environment);
        int layerMaskWall = 1 << LayerMask.NameToLayer(Utils.l_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "up/down" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.blue);
            return vInput < 0;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.blue);

        rayOrigin += rayDir;
        rayDir = transform.forward * (Radius + _wallCheckDistance * 4);
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
        rayDir = Mathf.Sign(-vInput) * transform.up * (Height + _climbMoveDistanceCheck);
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
        Vector3 rayDir = Mathf.Sign(hInput) * transform.right * (Radius + _climbMoveDistanceCheck);
        int layerMaskClimbZone = 1 << LayerMask.NameToLayer(Utils.l_Environment);
        int layerMaskWall = 1 << LayerMask.NameToLayer(Utils.l_Terrain);
        if(Physics.Raycast(rayOrigin, rayDir.normalized, out hit, rayDir.magnitude, layerMaskWall)) {
            // a surface blocks the "right/left" direction
            Debug.DrawLine(rayOrigin, hit.point, Color.blue);
            return false;
        }
        Debug.DrawRay(rayOrigin, rayDir, Color.blue);

        rayOrigin += rayDir;
        rayDir = transform.forward * (Radius + _wallCheckDistance * 2);
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
        rayDir = Mathf.Sign(-hInput) * transform.right * (_climbMoveDistanceCheck);
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
        if(!CanInteract) return null;
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
        if(!CanInteract) return null;
        RaycastHit hit;
        int layer = Utils.l_Terrain.ToLayerMask() | Utils.l_Interactibles.ToLayerMask();
        if (Physics.Raycast(_playerCam.transform.position, _playerCam.CamForward, out hit, interactionRange, layer))
        {
            if (hit.collider.TryGetComponent(out BombPlant bp)){
                if (bp.gotABomb){
                    return bp;
                }
            }
        }
        return null;
    }
    public void PushOut(Vector3 direction, float strength) {
        AudioManager.instance.PlaySound(AudioTag.playerGetsPushed, gameObject);
        _movement = direction.normalized * strength;
        _wasPushed = true;
    }
    #endregion
    #region ANIMATIONS
    const string animNameClimb = "Climb Tree", animNameJump = "JumpMiddle", animNamePickup = "PickUpFruit", animaNameThrow = "ThrowFruit", animNameDeclimb = "ClimbToLedge";
    const float minFallVelocityY = -2.1f;
    void PlayAnimClimb() {
        _anim.Play(animNameClimb, 0, 0);
    }
    void PlayAnimDeclimbUp() {
        _anim.SetBool("hasDeclimbedUp", false);
        _anim.Play(animNameDeclimb, 0, 0);
    }
    void StopAnimDeclimbUp() {
        _anim.SetBool("hasDeclimbedUp", true);
    }
    void InvertAnimDeclimbUp(bool invert) {
        _anim.SetFloat("DeclimbUpSpeedRatio", invert ? -1 : 1);
    }
    void PlayAnimJump() {
        _anim.Play(animNameJump, 0, 0);
    }
    void PlayAnimPickup() {
        _anim.Play(animNamePickup, 1, 0);
        OpenRightHand(false);
    }
    void PlayAnimThrow() {
        _anim.Play(animaNameThrow, 1, 0);
        OpenRightHand(true);
    }
    bool CheckIsFalling() {
        return _charaCtrl.velocity.y < minFallVelocityY && !_charaCtrl.isGrounded && !_isOnClimbWall && !_wasClimbing;
    }
    bool CheckEndFalling() {
        return _charaCtrl.isGrounded || _isOnClimbWall;
    }
    void OpenRightHand(bool open) {
        _anim.SetLayerWeight(2, open ? 0 : 1);
    }
    void AdjustAnimSpeed(float speed) {
        _anim.speed = speed;
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
    /*
    private void OnTriggerStay(Collider other) {
        if (other.TryGetComponent(out TunnelEntrance tEntrance)) {
            _playerCam.maxZoomRatio = tEntrance.GetCamLerpRatio(transform.position, true);
        }
    }
    private void OnTriggerExit(Collider other) {
        if(other.TryGetComponent(out TunnelEntrance tEntrance)) {
            _playerCam.maxZoomRatio = tEntrance.GetCamLerpRatio(transform.position, true);
        }
    }*/
#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        if(!debugDraws) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _soundRadiusWalk);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _soundRadiusRun);
    }
#endif
}