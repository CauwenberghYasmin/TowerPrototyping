using Unity.VisualScripting;
using UnityEngine;

public class RevivalPlayer : MonoBehaviour
{
    [SerializeField] private float reviveDepth = -25f;
    [SerializeField] private Transform[] spawnpoints;

    private int currIndex = 0;
    private Vector3 playerPos;
    private PlayerControllerScript playerControllerScript;

    void Start()
    {
        playerControllerScript = GetComponent<PlayerControllerScript>();
    }

    // Update is called once per frame
    void Update()
    {
        playerPos = transform.position;


        if (playerPos.y < reviveDepth)
        {
            transform.position = spawnpoints[currIndex].position;
            playerControllerScript.PlayerReset();
        }



        if (currIndex < spawnpoints.Length-1)
        {
            if (playerPos.z > spawnpoints[currIndex+1].position.z)  //this works cause rn the map is a super long line!
            {
                ++currIndex;
            }
        }
    }
}
