using UnityEngine;

public class PlayerVisualRotate : MonoBehaviour
{
    
        
    [SerializeField] private PlayerControllerScript player;
    [SerializeField] private Transform[] meshTransforms;
    [SerializeField] private float wallRunTiltAngle = 30f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ApplyWallRunTilt();
    }
    
    void ApplyWallRunTilt()
    {
        Quaternion tilt = Quaternion.identity;

        if (player.IsWallrunning)
        {
            float sign = player.IsOnLeftWall ? -1f : 1f;
            tilt = Quaternion.Euler(0f, 0f, sign * wallRunTiltAngle);
        }

        foreach (Transform t in meshTransforms)
        {
            if (!t) continue;
            t.localRotation = tilt;
        }
    }
}
