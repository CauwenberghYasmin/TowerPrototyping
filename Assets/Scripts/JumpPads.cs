using UnityEngine;


[RequireComponent(typeof(Collider))]
public class JumpPads : MonoBehaviour
{
 
    private void OnTriggerEnter(Collider other)
    {        
        PlayerControllerScript player = other.GetComponent<PlayerControllerScript>();
        if (!player) return;
 
        player.AddVerticalVelocity(-128f * player.VerticalVelocity);

    }
 
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.position, Vector3.one);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 1.1f);
    }


}
