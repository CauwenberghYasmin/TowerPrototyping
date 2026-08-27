using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _groundDrag;
    [SerializeField] private InputActionAsset _input;
    [SerializeField] private GameObject _cameraTarget;
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _jumpCooldown;
    [SerializeField] private float _airMultiplier;

    [Header("Rotation")]
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Ground Check")]
    [SerializeField] private float _playerHeight;
    [SerializeField] private LayerMask _groundLayer;

    private bool _isGrounded;

    private bool _readyToJump;
    private Transform _transform;
    private InputAction _moveAction;
    Vector2 _movementInput;

    Vector3 _moveDirection;
    Rigidbody _rigidBody;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _transform = transform;
        _rigidBody = GetComponent<Rigidbody>();
        var map = _input.FindActionMap("Player");
        _moveAction = map.FindAction("Move");
        _moveAction.Enable();
        _readyToJump = true;
    }

    // Update is called once per frame
    void Update()
    {
        // ground check
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, _playerHeight * 0.5f + 0.3f, _groundLayer);

        ReadInput();
        SpeedControl();
        RotatePlayer();

        if(_isGrounded)
        {
            _rigidBody.linearDamping = _groundDrag;
        }
        else
        {
            _rigidBody.linearDamping = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void ReadInput()
    {
        //read what direction the player wants to go
        _movementInput = _moveAction.ReadValue<Vector2>();
    }

    private void MovePlayer()
    {
        //calculate the direction the player should move based on the input that was read
        _moveDirection = _cameraTarget.transform.forward * _movementInput.y + _cameraTarget.transform.right * _movementInput.x;

        //apply the force to the actual player
        // on ground
        if (_isGrounded)
            _rigidBody.AddForce(_moveDirection.normalized * _moveSpeed, ForceMode.Force);

        // in air
        else if (!_isGrounded)
            _rigidBody.AddForce(_moveDirection.normalized * _moveSpeed * _airMultiplier, ForceMode.Force);

    }

    //rotates player in the direction it wants to go
    private void RotatePlayer()
    {
        //do nothing if it does not need to rotate
        if (_moveDirection.sqrMagnitude < 0.0001f)
            return;

        _moveDirection.y = 0f; // make sure the player does not look up

        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection.normalized, Vector3.up);
        Quaternion smoothed = Quaternion.Slerp(_rigidBody.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);
        _rigidBody.MoveRotation(smoothed);
    }

    private void SpeedControl()
    {
        //check current speed (excluding speed going up/down
        Vector3 flatVel = new Vector3(_rigidBody.linearVelocity.x, 0f, _rigidBody.linearVelocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > _moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * _moveSpeed;
            _rigidBody.linearVelocity = new Vector3(limitedVel.x, _rigidBody.linearVelocity.y, limitedVel.z);
        }
    }

    private void TryJump()
    {
        if(_readyToJump && _isGrounded)
        {
            _readyToJump = false;
            // set y velocity to 0
            _rigidBody.linearVelocity = new Vector3(_rigidBody.linearVelocity.x, 0f, _rigidBody.linearVelocity.z);
            //jump
            _rigidBody.AddForce(transform.up * _jumpForce, ForceMode.Impulse);

            //ready to jump again after cooldown
            Invoke(nameof(ResetJump), _jumpCooldown);
        }
    }

    private void ResetJump()
    {
        _readyToJump = true;
    }
}