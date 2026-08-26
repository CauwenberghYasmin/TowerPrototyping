using Unity.VisualScripting;
using UnityEngine;

public class PlayerWallRunning : MonoBehaviour
{
    [Header("WallRunning")]
    [SerializeField] private LayerMask _wallMask;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _wallRunForce;
    [SerializeField] private float _maxWallRunTime;
    private float _wallRunTimer;

    [Header("Input")]
    private float _horizontalInput;
    private float _verticalInput;

    [Header("Detection")]
    [SerializeField] private float _wallCheckDistance;
    [SerializeField] private float _minJumpHeight;

    private RaycastHit _leftWallRaycast;
    private RaycastHit _rightWallRaycast;
    private bool _leftWallHit = false;
    private bool _rightWallHit = false;

    [Header("Reference")]
    private Transform _playerTransform;
    private PlayerControllerScript _playerControllerScript;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerTransform = GetComponent<Transform>();
        _playerControllerScript = GetComponent<PlayerControllerScript>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void CheckForWall()
    {
        //check for both left and right side wall collision
        _leftWallHit = Physics.Raycast(_playerTransform.position, -_playerTransform.right, out _leftWallRaycast, _wallCheckDistance, _wallMask);
        _rightWallHit = Physics.Raycast(_playerTransform.position, _playerTransform.right, out _rightWallRaycast, _wallCheckDistance, _wallMask);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, _minJumpHeight);
    }

    private void StateMachine()
    {
        Vector2 moveInput = _playerControllerScript.MoveAction.ReadValue<Vector2>();

        //if we're hitting a wall on either side AND we're trying to move forward (pressing W/moving joystick forward) 
        if((_rightWallHit || _leftWallHit) && moveInput.y > 0)
        {
            //start wallrunning
        }
    }

    private void StartWallRunning()
    {

    }

    private void StopWallRunning()
    {

    }
}
