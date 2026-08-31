using UnityEngine;

public class SlowingPad : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    
    private void OnTriggerEnter(Collider other)
    {
        PlayerControllerScript player = other.GetComponent<PlayerControllerScript>();
        if (!player) return;


        player.SlowPlayerDown();
    }
}
