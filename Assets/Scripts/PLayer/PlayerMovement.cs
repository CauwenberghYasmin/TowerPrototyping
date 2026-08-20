using UnityEngine;
using UnityEngine.InputSystem;


//I dropped this as it just is abit harder to make our own movements without clashing with the physics of unity :/
[RequireComponent((typeof(Rigidbody)))]
public class PlayerMovement : MonoBehaviour
{

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";
    
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckOffset = 0.1f; 
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;
    
    private Vector3 GroundCheckPosition => transform.position + Vector3.down * groundCheckOffset;

    
    private Rigidbody _rb;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private Vector2 _moveInput;
    private bool _jumpQueued;
    private bool _isGrounded;

    
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        var map = inputActions.FindActionMap(actionMapName);
        _moveAction = map.FindAction(moveActionName);
        _jumpAction = map.FindAction(jumpActionName);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }


    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        _isGrounded = Physics.CheckSphere(GroundCheckPosition, groundCheckRadius, groundMask);
        
        
        Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y);
        Vector3 targetVelocity = moveDir * moveSpeed;

        Vector3 velocity = _rb.linearVelocity;
        velocity.x = targetVelocity.x;
        velocity.z = targetVelocity.z;
        _rb.linearVelocity = velocity;

        if (_jumpQueued)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _jumpQueued = false;
        }
        
        
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();

        _moveAction.performed += OnMovePerformed;
        _moveAction.canceled += OnMoveCanceled;
        _jumpAction.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        _moveAction.performed -= OnMovePerformed;
        _moveAction.canceled -= OnMoveCanceled;
        _jumpAction.performed -= OnJumpPerformed;

        _moveAction.Disable();
        _jumpAction.Disable();
    }
    
    
    private void OnMovePerformed(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => _moveInput = Vector2.zero;
    private void OnJumpPerformed(InputAction.CallbackContext ctx) { if (_isGrounded) _jumpQueued = true; }
}
