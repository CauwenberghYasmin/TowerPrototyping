using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    private float _rotationVelocity;

    //camera third person!!
    public Vector3 GetMoveDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f) return Vector3.zero;

        float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
        float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);

        return Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
    }
}
