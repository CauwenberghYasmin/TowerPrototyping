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
    [SerializeField] private float friction = 3f;
    [SerializeField] private float currTurnResponse = 4f;
    [SerializeField] private float normalTurnRespons = 4f;
    [SerializeField] private float slowedTurnRespons = 2f;
    [SerializeField] private float normalGravity = -9.81f;
    [SerializeField] private float gravityMultiplier = 0.5f;

    [SerializeField] private float turnRateDegPerSec = 360f;   // how fast heading rotates toward input
    [SerializeField] private float slopeAccelStrength = 20f;    // how hard slopes accelerate you
    [SerializeField] private float groundNormalSmoothSpeed = 10f;

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
    //how much the player needs to move forward (input wise) to start wallrunning (deadzone)
    private float minForwardInputToWallRun = 0.25f;
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

    private PlayerState playerState = PlayerState.Grounded;
    
    public bool IsWallrunning => playerState == PlayerState.Wallrunning;
    public bool IsOnLeftWall => leftWallHit;
    

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
        SlideMovement();

        if (wallJumpCooldownTimer > 0f)
            wallJumpCooldownTimer -= Time.deltaTime;
        
    }

    public void PlayerReset()
    {
        currJumpCount = 0;
        _horizontalVelocity = Vector3.zero;
        playerState = PlayerState.Grounded;
        _verticalVelocity = 0f;
        targetDirection = Vector3.zero;
    }



    void CalculateState()
    {
        //if we're grounded
        if (_isGrounded)
        {
            //if we're not already in the grounded state, we enter it
            if (playerState != PlayerState.Grounded)
            {
                gravity = normalGravity;
                playerState = PlayerState.Grounded;
            }
            return;
        }

        //not grounded from here on

        //are we hitting a wall
        bool wallAvailable = (leftWallHit || rightWallHit)  && wallJumpCooldownTimer <= 0f;
        //is the player pressing forward
        bool hasForwardInput = _moveInput.y > minForwardInputToWallRun;
        //is the player high enough above the ground
        bool highEnough = _groundDistance > minJumpHeight;

        //we start wallrunning if we're hitting a wall, we're trying to move forward and we're high enough above the ground
        if (wallAvailable && hasForwardInput && highEnough)
        {
            _verticalVelocity = 0f;
            currJumpCount = 0;
            gravity = wallRunningGravity;
            playerState = PlayerState.Wallrunning;
        }
        //in this case we're supposed to be in the falling state so now we just check if we need to enter it or not
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

    void SlideMovement()
    {
        //apply gravity to vertical movement
        _verticalVelocity += (gravity / gravityMultiplier) * Time.deltaTime;

        //if we're wallrunning
        if (playerState == PlayerState.Wallrunning)
        {
            WallRunMovement();
        }
        else
        {
            //check direction player is looking
            Vector3 moveDir = playerRotation.GetMoveDirection(_moveInput);
            //get the input direction the player gave
            Vector3 inputDir = Vector3.ProjectOnPlane(moveDir, _groundNormal).normalized;
            //if the player pressed an input
            if (_moveInput.sqrMagnitude > 0.0001f)
            {
                float currentSpeed = _horizontalVelocity.magnitude;
                Vector3 currentDir = currentSpeed > 0.01f ? _horizontalVelocity.normalized : inputDir;

                //rotate towards the input direction
                Vector3 newDir = Vector3.RotateTowards(
                    currentDir, inputDir,
                    turnRateDegPerSec * Mathf.Deg2Rad * Time.deltaTime, 0f);

                float newSpeed = Mathf.MoveTowards(currentSpeed, currMaxSpeed, accel * Time.deltaTime);
                //to make sure speed is not lost
                if (currentSpeed > currMaxSpeed) newSpeed = currentSpeed;
                //apply new velocity
                _horizontalVelocity = newDir * newSpeed;
            }
            else
            {
            
                _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, Vector3.zero, friction * Time.deltaTime);
            }

            //slope acceleration
            if (_isGrounded)
            {
                Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, _groundNormal);
                
                Vector3 alongSkate = Vector3.Project(downhill, transform.forward);

                _horizontalVelocity += alongSkate * slopeAccelStrength * Time.deltaTime;
            }

            //projects on plane (stays the same if already perpendicular
            //_horizontalVelocity = Vector3.ProjectOnPlane(_horizontalVelocity, _groundNormal);
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
        //get wall normal
        Vector3 wallNormal = leftWallHit ? leftWallRaycast.normal : rightWallRaycast.normal;

        // direction along the wall's surface that matches where the player is facing
        Vector3 wallForward = Vector3.ProjectOnPlane(playerTransform.forward, wallNormal).normalized;

        // if player is looking away from the wall make it go away from the wall
        if (Vector3.Dot(wallForward, playerTransform.forward) < 0f)
            wallForward = -wallForward;

        //make sure we dont lose speed on walls
        float currentSpeed = _horizontalVelocity.magnitude;
        float newSpeed = wallRunSpeed < currentSpeed ? wallRunSpeed : currentSpeed;
        //convert the velocity to one that matches the wall's forward capped at the accel speed
        Vector3 targetVelocity = wallForward * newSpeed;
        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, wallRunAccel * Time.deltaTime);

        // keep velocity flat against the wall plane
        _horizontalVelocity = Vector3.ProjectOnPlane(_horizontalVelocity, wallNormal);

        // stick into the wall a little so CharacterController keeps registering contact
        _horizontalVelocity += -wallNormal * wallStickForce * Time.deltaTime * 10f;
    }

    void CheckGround()
    {
        //player origin
        Vector3 origin = transform.position + Vector3.up;

        //this scales based off of our speed 
        float speedScale = Mathf.Max(_horizontalVelocity.magnitude, Mathf.Abs(_verticalVelocity)) * Time.deltaTime;
        //float checkDist = groundCheckOffset + speedScale + 0.1f; 
        
        //check if we're hitting the ground
        _isGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out RaycastHit hitInfo, groundCheckOffset, groundMask);
        
        // separate, longer-range cast just to know how far off the ground we are
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundDistHit, 100f, groundMask))
            _groundDistance = groundDistHit.distance - 1f; // subtract the Vector3.up offset added to origin
        else
            _groundDistance = Mathf.Infinity;

        //let ground normal to make curved slopes smoother
        if (_isGrounded)
        {
            _groundNormal = Vector3.Slerp(_groundNormal, hitInfo.normal, groundNormalSmoothSpeed * Time.deltaTime);
        }
        else
        {
            _groundNormal = Vector3.Slerp(_groundNormal, Vector3.up, groundNormalSmoothSpeed * Time.deltaTime);
        }

        //if we're grounded, apply the grounded stickforce
        if (_isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = groundedStickForce;
        }
    }

    private void OnEnable()
    {
        //enable move and jump action and subscribe to it
        _moveAction.Enable();
        _moveAction.performed += OnMovePerformed;
        _moveAction.canceled += OnMoveCanceled;

        _jumpAction.Enable();
        _jumpAction.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        //unsub from the move and jumpaction and disable them
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
    //for the jumppads
    public void AddVerticalVelocity(float value)
    {
        _verticalVelocity += value;
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

