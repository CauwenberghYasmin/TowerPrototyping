using Unity.VisualScripting;
using UnityEngine;

public class RevivalPlayer : MonoBehaviour
{
    [SerializeField] private float reviveDepth = -25f;
    [SerializeField] private Transform[] spawnpoints;

    private int currIndex = 0;
    private Vector3 playerPos;
    private PlayerControllerScript playerControllerScript;
    private CharacterController cr;

    void Start()
    {
        playerControllerScript = GetComponent<PlayerControllerScript>();
        cr = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        playerPos = transform.position;


        if (playerPos.y < reviveDepth)
        {
            cr.enabled = false;
            transform.position = spawnpoints[currIndex].position;
            playerControllerScript.PlayerReset();
            cr.enabled = true;
        }
    }
}
