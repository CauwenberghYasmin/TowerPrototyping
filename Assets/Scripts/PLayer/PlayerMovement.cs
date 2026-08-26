using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed;
    [SerializeField] private InputActionAsset _input;

    private Transform _transform;

    private InputAction _moveAction;
    Vector2 _movementInput;

    Vector3 _moveDirection;
    Rigidbody _rigidBody;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        var map = _input.FindActionMap("Player");
        _moveAction = map.FindAction("Move");
        _moveAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void ReadInput()
    {
        //read what direction the player wants to go
        _movementInput = _moveAction.ReadValue<Vector2>();
        if (_movementInput.y > 0f)
            return;
    }

    private void MovePlayer()
    {
        //calculate the direction the player should move based on the input that was read
        _moveDirection = transform.forward * _movementInput.y + transform.right * _movementInput.x;

        //apply the force to the actual player
        _rigidBody.AddForce(_moveDirection.normalized * _moveSpeed, ForceMode.Force);
    }
}
