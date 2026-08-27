using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

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
    
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";

    
    [Header("Movement")]
    [SerializeField] private float currMaxSpeed = 8f;
    [SerializeField] private float accel = 12f;  
    [SerializeField] private float decel = 2f;   
    [SerializeField] private float turnResponse = 4f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float gravityMultiplier = 0.5f;

    [SerializeField] private float minminSpeed = 6;
    [SerializeField] private float averageSpeedSpeed = 10;
    [SerializeField] private float speedMulitplier = 14;

    [SerializeField] private float minHeightDiff = 1;
    [SerializeField] private Transform frontPointTransform;
    [SerializeField] private Transform backPointTransform;


    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 5f;
    private int currJumpCount = 0;
    private const int maxJumpAMount = 2;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundedStickForce = -2f; 
    private Vector3 _groundNormal = Vector3.up;


    private CharacterController _controller;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private float _verticalVelocity;
    private Vector3 _horizontalVelocity;
    private Vector3 targetDirection = Vector3.zero;

    private PlayerState _playerState;
    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        var map = inputActions.FindActionMap(actionMapName);
        _moveAction = map.FindAction(moveActionName);
        _jumpAction = map.FindAction(jumpActionName);

    }
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        CheckGround();
        CalculateMaxSpeedAndSlope();
        SlideMovement();
    }


    void CalculateMaxSpeedAndSlope()
    {
        Vector3 origin = frontPointTransform.position + (Vector3.up);
        Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out RaycastHit hitInfo, groundCheckOffset+20, groundMask);


        float frontPointDistance = hitInfo.distance;

        origin = backPointTransform.position + (Vector3.up);
        Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out RaycastHit hitInfo2, groundCheckOffset+20, groundMask);

        float backPOintDistance = hitInfo2.distance;
        double heightDifference = frontPointDistance - backPOintDistance;

        if (heightDifference < -minHeightDiff && _isGrounded) //negative so slope upwards (char tryin to go up?)
        {
            targetDirection = Vector3.back;
            currMaxSpeed = minminSpeed; //trying to skate upwards should be slower!

            Debug.Log("slope!");
        }
        else if (heightDifference > minHeightDiff && _isGrounded) //pos so slope downwards
        {
            targetDirection = Vector3.forward;
            currMaxSpeed += speedMulitplier * Time.deltaTime;
            Debug.Log("slope2!");
        }
        else
        {
            targetDirection = Vector3.zero;
            currMaxSpeed = averageSpeedSpeed;
            Debug.Log("no slope!");
        }
    }





    void SlideMovement()
    {
        _verticalVelocity += (gravity / gravityMultiplier) * Time.deltaTime;
      
        Vector3 moveDir = playerRotation.GetMoveDirection(_moveInput);
        Vector3 inputDir = Vector3.ProjectOnPlane(moveDir, _groundNormal).normalized;
 

        if (_moveInput.sqrMagnitude > 0.0001f)
        {
            Vector3 targetVelocity = inputDir * currMaxSpeed;
            _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, targetVelocity, turnResponse * Time.deltaTime);
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, accel * Time.deltaTime);
        }
        else
        {
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetDirection, decel * Time.deltaTime); //if floor not flat, targetdirection should be slope!
        }
        
        _horizontalVelocity = Vector3.ProjectOnPlane(_horizontalVelocity, _groundNormal);

 

        Vector3 velocity;
        if (_isGrounded)
        {
            velocity = _horizontalVelocity + Vector3.down * 0.1f;
            velocity.y += _verticalVelocity;
            Debug.Log("is grounded!");

            currJumpCount = 0;
        }
        else 
        { 
            velocity = new Vector3(_horizontalVelocity.x, _verticalVelocity, _horizontalVelocity.z);
            Debug.Log("is NOT grounded!");
        }
 
        _controller.Move(velocity * Time.deltaTime);
    }

    
    void CheckGround()
    {
        Vector3 origin = transform.position + (Vector3.up);

        _isGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out RaycastHit hitInfo, groundCheckOffset, groundMask);


        if (_isGrounded)
        {
            _groundNormal = hitInfo.normal;
            Debug.Log("On floor!");
        }
        else
        {
            _groundNormal = Vector3.up;
        }


        if (_isGrounded && _verticalVelocity < 0f) 
        { 
            _verticalVelocity = groundedStickForce;
            Debug.Log("Calling reset!");
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
        if (_isGrounded || currJumpCount < (maxJumpAMount -1))
        {
            float effectiveGravity = gravity / gravityMultiplier;
            _verticalVelocity = Mathf.Sqrt( -2f * jumpHeight* effectiveGravity); //physics fomrula 

            ++currJumpCount;
        }
    }
}
