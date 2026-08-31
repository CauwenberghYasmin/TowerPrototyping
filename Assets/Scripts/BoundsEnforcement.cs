using UnityEngine;

public class BoundsEnforcement : MonoBehaviour
{
    [SerializeField] GameObject respawnPoint = null;

    private void OnTriggerEnter(Collider other)
    {
        var controllerScript = other.GetComponent<PlayerControllerScript>();
        if (controllerScript == null) return;
        controllerScript.CancelVelocity();
        other.gameObject.transform.position = respawnPoint.transform.position;
    }
}
