using System;
using Unity.Cinemachine;
using UnityEngine;

public class SpeedyCamera : MonoBehaviour
{
    [SerializeField] private float minFov = 60f;
    [SerializeField] private float minFovSpeed = 0f;
    [SerializeField] private float maxFov = 120f;
    [SerializeField] private float maxFovSpeed = 50f;
    [SerializeField] private float lerpSpeed = 4f;
    private float currentFov;
    private PlayerControllerScript playerController = null;
    private CinemachineCamera playerCamera = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = FindFirstObjectByType<PlayerControllerScript>();
        playerCamera = GetComponent<CinemachineCamera>();
        currentFov = minFov;
    }

    // Update is called once per frame
    void Update()
    {
        float currentSpeed = playerController.GetSpeed();
        float mappedSpeed = Mathf.InverseLerp(minFovSpeed, maxFovSpeed, currentSpeed);

        float targetFov = Mathf.Lerp(minFov, maxFov, mappedSpeed);
        currentFov = Mathf.Lerp(currentFov, targetFov, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));
        var lens = playerCamera.Lens;
        lens.FieldOfView = currentFov;
        playerCamera.Lens = lens;
    }
}
