using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using UnityEditor;
using UnityEngine.Rendering;


enum PlayerState
{
    Falling,
    Grounded,
    Wallrunning
};

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerScript : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerRotation playerRotation;
    public PlayerRotation PlayerRotation { get { return playerRotation; } }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";

    [Header("Movement")]
    [SerializeField] private float currMaxSpeed = 8f;
    [SerializeField] private float accel = 12f;
    [SerializeField] private float decel = 2f;
    [SerializeField] private float currTurnResponse = 4f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float normalGravity = -9.81f;
    [SerializeField] private float gravityMultiplier = 0.5f;

    [SerializeField] private float minminSpeed = 6;
    [SerializeField] private float averageSpeedSpeed = 10;
    [SerializeField] private float speedMulitplier = 14;

    [SerializeField] private float minHeightDiff = 1;
    [SerializeField] private Transform frontPointTransform;
    [SerializeField] private Transform backPointTransform;

    [SerializeField] private GameObject cinemachineCamera;
    [SerializeField] private float airControllRotation = 0.3f;

    [SerializeField] private float wallRunningGravity = -0.5f;

    private Transform playerTransform;
    private float gravity = -9.81f;

    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float wallJumpForce = 8f;
    private int currJumpCount = 0;
    private const int maxJumpAMount = 2;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundedStickForce = -2f;
    private Vector3 _groundNormal = Vector3.up;

    [Header("Wall Check")]
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private float wallCheckRadius = 0.3f;

    [Header("Wall Running")]
    //min height above ground to wallrun
    [SerializeField] private float minJumpHeight = 1.5f;
    [SerializeField] private float wallRunSpeed = 9f;
    [SerializeField] private float wallRunAccel = 15f;
    //how hard the player keeps getting pushed into the wall while wallrunning
    [SerializeField] private float wallStickForce = 3f;
    //how much the player needs to move forward (input wise) to start wallrunning
    [SerializeField] private float minForwardInputToWallRun = 0.25f;
    //cooldown for when you jump off a wall
    [SerializeField] private float wallJumpCooldown = 0.5f;
    private float wallJumpCooldownTimer = 0f;
    
    private float _groundDistance = Mathf.Infinity;

    private RaycastHit leftWallRaycast;
    private RaycastHit rightWallRaycast;
    private bool leftWallHit = false;
    private bool rightWallHit = false;

    private CharacterController _controller;
    public CharacterController Controller { get { return _controller; } }

    private InputAction _moveAction;
    public InputAction MoveAction { get { return _moveAction; } }
    private InputAction _jumpAction;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private float _verticalVelocity;
    private Vector3 _horizontalVelocity;
    private Vector3 targetDirection = Vector3.zero;

    //camera var
    private const float normalCameraControllGain = 1f;
    private CinemachineInputAxisController cinemaController;
    private const float normalTurnRespons = 4f;
    private const float slowedTurnRespons = 2f;

    private PlayerState playerState = PlayerState.Grounded;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        playerTransform = GetComponent<Transform>();
        var map = inputActions.FindActionMap(actionMapName);
        _moveAction = map.FindAction(moveActionName);
        _jumpAction = map.FindAction(jumpActionName);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        cinemaController = cinemachineCamera.GetComponent<CinemachineInputAxisController>();
    }

    private void Update()
    {
        CheckGround();
        CheckForWall();
        CalculateState();
        ChangeRotationSpeed();
        CalculateMaxSpeedAndSlope();
        SlideMovement();
        
        if (wallJumpCooldownTimer > 0f)
            wallJumpCooldownTimer -= Time.deltaTime;
        
    }

    void CalculateState()
    {
        if (_isGrounded)
        {
            if (playerState != PlayerState.Grounded)
            {
                gravity = normalGravity;
                playerState = PlayerState.Grounded;
            }
            return;
        }

        // not grounded from here on
        bool wallAvailable = (leftWallHit || rightWallHit)  && wallJumpCooldownTimer <= 0f;
        //is the player pressing forward
        bool hasForwardInput = _moveInput.y > minForwardInputToWallRun;
        //is the player high enough above the ground
        bool highEnough = _groundDistance > minJumpHeight;

        // we start wallrunning
        if (wallAvailable && hasForwardInput && highEnough)
        {
            _verticalVelocity = 0f;
            currJumpCount = 0;
            gravity = wallRunningGravity;
            playerState = PlayerState.Wallrunning;
        }
        else if (playerState != PlayerState.Falling)
        {
            gravity = normalGravity;
            playerState = PlayerState.Falling;
        }
    }

    void ChangeRotationSpeed()
    {
        if (_isGrounded)
        {
            foreach (var c in cinemaController.Controllers)
            {
                if (c.Name == "Look Orbit X") c.Input.Gain = normalCameraControllGain;
                if (c.Name == "Look Orbit Y") c.Input.Gain = normalCameraControllGain;
            }
            currTurnResponse = normalTurnRespons;
        }
        else
        {
            foreach (var c in cinemaController.Controllers)
            {
                if (c.Name == "Look Orbit X") c.Input.Gain = airControllRotation;
                if (c.Name == "Look Orbit Y") c.Input.Gain = airControllRotation;
            }
            currTurnResponse = slowedTurnRespons;
        }
    }

    private void CheckForWall()
    {
        // pull the cast origin back so the sphere never starts already overlapping the wall
        //sphere cast bad when origin inside the wall
        float castOffset = wallCheckRadius + 0.1f;

        Vector3 leftOrigin = playerTransform.position + playerTransform.right * castOffset;
        leftWallHit = Physics.SphereCast(
            leftOrigin, wallCheckRadius, -playerTransform.right,
            out leftWallRaycast, wallCheckDistance + castOffset, wallMask
        );

        Vector3 rightOrigin = playerTransform.position - playerTransform.right * castOffset;
        rightWallHit = Physics.SphereCast(
            rightOrigin, wallCheckRadius, playerTransform.right,
            out rightWallRaycast, wallCheckDistance + castOffset, wallMask
        );
    }

    void CalculateMaxSpeedAndSlope()
    {
        Vector3 origin = frontPointTransform.position + Vector3.up;
        Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out RaycastHit hitInfo, groundCheckOffset + 20, groundMask);
        float frontPointDistance = hitInfo.distance;

        origin = backPointTransform.position + Vector3.up;
        Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out RaycastHit hitInfo2, groundCheckOffset + 20, groundMask);
        float backPOintDistance = hitInfo2.distance;

        double heightDifference = frontPointDistance - backPOintDistance;

        if (heightDifference < -minHeightDiff && _isGrounded)
        {
            targetDirection = Vector3.back;
            currMaxSpeed = minminSpeed;
        }
        else if (heightDifference > minHeightDiff && _isGrounded)
        {
            targetDirection = Vector3.forward;
            currMaxSpeed += speedMulitplier * Time.deltaTime;
        }
        else
        {
            targetDirection = Vector3.zero;
            currMaxSpeed = averageSpeedSpeed;
        }
    }

    void SlideMovement()
    {
        _verticalVelocity += (gravity / gravityMultiplier) * Time.deltaTime;

        if (playerState == PlayerState.Wallrunning)
        {
            WallRunMovement();
        }
        else
        {
            Vector3 moveDir = playerRotation.GetMoveDirection(_moveInput);
            Vector3 inputDir = Vector3.ProjectOnPlane(moveDir, _groundNormal).normalized;

            if (_moveInput.sqrMagnitude > 0.0001f)
            {
                Vector3 targetVelocity = inputDir * currMaxSpeed;
                _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, targetVelocity, currTurnResponse * Time.deltaTime);
                _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, accel * Time.deltaTime);
            }
            else
            {
                _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetDirection, decel * Time.deltaTime);
            }

            _horizontalVelocity = Vector3.ProjectOnPlane(_horizontalVelocity, _groundNormal);
        }

        Vector3 velocity;
        if (_isGrounded)
        {
            velocity = _horizontalVelocity + Vector3.down * 0.1f;
            velocity.y += _verticalVelocity;
            if (_verticalVelocity <= 0f)
                currJumpCount = 0;
        }
        else
        {
            velocity = new Vector3(_horizontalVelocity.x, _verticalVelocity, _horizontalVelocity.z);
        }

        _controller.Move(velocity * Time.deltaTime);
    }

    void WallRunMovement()
    {
        Vector3 wallNormal = leftWallHit ? leftWallRaycast.normal : rightWallRaycast.normal;

        // direction along the wall's surface that matches where the player is facing
        Vector3 wallForward = Vector3.ProjectOnPlane(playerTransform.forward, wallNormal).normalized;

        // if player is looking away from the wall make it go away from the wall
        if (Vector3.Dot(wallForward, playerTransform.forward) < 0f)
            wallForward = -wallForward;

        Vector3 targetVelocity = wallForward * wallRunSpeed;
        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, wallRunAccel * Time.deltaTime);

        // keep velocity flat against the wall plane
        _horizontalVelocity = Vector3.ProjectOnPlane(_horizontalVelocity, wallNormal);

        // stick into the wall a little so CharacterController keeps registering contact
        _horizontalVelocity += -wallNormal * wallStickForce * Time.deltaTime * 10f;
    }

    void CheckGround()
    {
        Vector3 origin = transform.position + Vector3.up;

        _isGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out RaycastHit hitInfo, groundCheckOffset, groundMask);

        // separate, longer-range cast just to know how far off the ground we are (for wallrun gating)
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundDistHit, 100f, groundMask))
            _groundDistance = groundDistHit.distance - 1f; // subtract the Vector3.up offset added to origin
        else
            _groundDistance = Mathf.Infinity;

        if (_isGrounded)
        {
            _groundNormal = hitInfo.normal;
        }
        else
        {
            _groundNormal = Vector3.up;
        }

        if (_isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = groundedStickForce;
        }
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _moveAction.performed += OnMovePerformed;
        _moveAction.canceled += OnMoveCanceled;

        _jumpAction.Enable();
        _jumpAction.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        _moveAction.performed -= OnMovePerformed;
        _moveAction.canceled -= OnMoveCanceled;
        _moveAction.Disable();

        _jumpAction.performed -= OnJumpPerformed;
        _jumpAction.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => _moveInput = Vector2.zero;

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (playerState == PlayerState.Wallrunning)
        {
            Vector3 wallNormal = leftWallHit ? leftWallRaycast.normal : rightWallRaycast.normal;

            float effectiveGravity = normalGravity / gravityMultiplier;
            _verticalVelocity = Mathf.Sqrt(-2f * jumpHeight * effectiveGravity);

            _horizontalVelocity += wallNormal * wallJumpForce;

            gravity = normalGravity;
            playerState = PlayerState.Falling;
            currJumpCount = 1;
            wallJumpCooldownTimer = wallJumpCooldown; // reset :p 
            return;
        }

        if (_isGrounded || currJumpCount < maxJumpAMount)
        {
            float effectiveGravity = gravity / gravityMultiplier;
            _verticalVelocity = Mathf.Sqrt(-2f * jumpHeight * effectiveGravity);
            ++currJumpCount;
        }
    }
    

    private void OnDrawGizmos()
    {
        var prevzTest = Handles.zTest;
        Handles.zTest = CompareFunction.Always; 
        
        Vector3 pos = transform.position;
        const float offset = 2f; 
        
        // yellow shows what checkground reads!!!
        Handles.color = _isGrounded ? Color.yellow : Color.gray;
        Handles.DrawLine(pos , pos + _groundNormal * offset);

        //horizontal velocity 
        Handles.color = Color.cyan;
        Handles.DrawLine(pos ,pos + _horizontalVelocity);
        
        // Slide targetDirection
        Handles.color = Color.magenta;
        Handles.DrawLine(pos, pos + targetDirection * 2f);
        
        // wall checks (red no hit green obv hit)
        float castOffset = wallCheckRadius + 0.1f;
        Vector3 leftOrigin = pos + transform.right * castOffset;
        Vector3 rightOrigin = pos - transform.right * castOffset;

        Handles.color = leftWallHit ? Color.red : Color.green;
        Handles.DrawLine(leftOrigin, leftOrigin - transform.right * (wallCheckDistance + castOffset));

        Handles.color = rightWallHit ? Color.red : Color.green;
        Handles.DrawLine(rightOrigin, rightOrigin + transform.right * (wallCheckDistance + castOffset));
        
        Handles.zTest = prevzTest;
    }

}

