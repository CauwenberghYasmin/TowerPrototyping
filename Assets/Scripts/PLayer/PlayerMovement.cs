using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed;
    [SerializeField] private InputActionAsset _input;
    [SerializeField] private GameObject _cameraTarget;

    [Header("Rotation")]
    [SerializeField] private float _rotationSpeed = 10f;

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
    }

    // Update is called once per frame
    void Update()
    {
        ReadInput();
        RotatePlayer();
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
        _rigidBody.AddForce(_moveDirection.normalized * _moveSpeed, ForceMode.Force);
        _moveDirection.y = 0f; // keep movement flat on the horizontal plane
    }

    //rotates player in the direction it wants to go
    private void RotatePlayer()
    {
        //do nothing if it does not need to rotate
        if (_moveDirection.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection.normalized, Vector3.up);
        Quaternion smoothed = Quaternion.Slerp(_rigidBody.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);
        _rigidBody.MoveRotation(smoothed);
    }
}