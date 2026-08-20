using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerScript : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerRotation playerRotation;
    
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";

    
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float accel = 12f;  
    [SerializeField] private float decel = 2f;   
    [SerializeField] private float turnResponse = 4f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundedStickForce = -2f; 
    private Vector3 _groundNormal = Vector3.up;


    private CharacterController _controller;
    private InputAction _moveAction;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private float _verticalVelocity;
    private Vector3 _horizontalVelocity;
    
    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        var map = inputActions.FindActionMap(actionMapName);
        _moveAction = map.FindAction(moveActionName);

    }
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        CheckGround();
        SlideMovement();

    }



    void SlideMovement()
    {
        _verticalVelocity += gravity * Time.deltaTime;
 
        Vector3 moveDir = playerRotation.GetMoveDirection(_moveInput);
        Vector3 inputDir = Vector3.ProjectOnPlane(moveDir, _groundNormal).normalized;
 

        if (_moveInput.sqrMagnitude > 0.0001f)
        {
            Vector3 targetVelocity = inputDir * maxSpeed;
            _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, targetVelocity, turnResponse * Time.deltaTime);
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, accel * Time.deltaTime);
        }
        else
        {
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, Vector3.zero, decel * Time.deltaTime);
        }
        
        _horizontalVelocity = Vector3.ProjectOnPlane(_horizontalVelocity, _groundNormal);
 
        Vector3 velocity;
        if (_isGrounded)
        {
            velocity = _horizontalVelocity + Vector3.down * 0.1f;
            velocity.y += _verticalVelocity;
        }
        else { velocity = new Vector3(_horizontalVelocity.x, _verticalVelocity, _horizontalVelocity.z); }
 
        _controller.Move(velocity * Time.deltaTime);
    }

    
    void CheckGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        _isGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out RaycastHit hitInfo, groundCheckOffset, groundMask);
 
        _groundNormal = _isGrounded ? hitInfo.normal : Vector3.up;
 
        if (_isGrounded && _verticalVelocity < 0f) { _verticalVelocity = groundedStickForce; }
    }
    
    
   
    private void OnEnable()
    {
        _moveAction.Enable();
        _moveAction.performed += OnMovePerformed;
        _moveAction.canceled += OnMoveCanceled;

    }

    private void OnDisable()
    {
        _moveAction.performed -= OnMovePerformed;
        _moveAction.canceled -= OnMoveCanceled;
        _moveAction.Disable();

    }

    private void OnMovePerformed(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => _moveInput = Vector2.zero;


}
