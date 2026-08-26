using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraRotation : MonoBehaviour
{
    [SerializeField] private InputActionAsset _input;
    [SerializeField] private float _sensitivity = 0.1f;
    [SerializeField] private float _minPitch = -30f;
    [SerializeField] private float _maxPitch = 70f;

    private InputAction _lookAction;
    private float _yaw;
    private float _pitch;

    void Awake()
    {
        _lookAction = _input.FindActionMap("Player").FindAction("Look");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        _lookAction.Enable();
    }

    void OnDisable()
    {
        _lookAction.Disable();
    }

    void Update()
    {
        Vector2 lookInput = _lookAction.ReadValue<Vector2>();

        _yaw += lookInput.x * _sensitivity;
        _pitch -= lookInput.y * _sensitivity; // subtract so moving mouse up looks up
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}
