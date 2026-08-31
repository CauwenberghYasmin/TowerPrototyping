using System;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{


    [Header("Movement Setup")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float speed = 2f;
 


    [Header("Behaviour")]
    [SerializeField] private bool MoveBack = true;
    [SerializeField] private float Pause = 0f;


    [Header("Gizmos")]
    [SerializeField] private Color gizmoColor = Color.darkRed;

    

    private Vector3 targetPos;
    private float pauseTimer;
    private bool movingToEnd;


    private void OnValidate()
    {
        //found abt this online, each time you change a vlaue on a serialized field it goes through here
        // even in the editor :p
        if (Application.isPlaying) return;
        if (!startPoint) return;

        transform.position = startPoint.position;
    }
    


    private void Awake()
    {

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (startPoint == null || endPoint == null)
        {
            Debug.LogWarning($"{name}: MovingPlatform is missing startPoint or endPoint.", this);
            enabled = false;
            return;
        }
 
        movingToEnd = true;
        targetPos = endPoint.position;

    }

    // Update is called once per frame
    void Update()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            return;
        }
 
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
        transform.position = newPos;
 
        if ((newPos - targetPos).sqrMagnitude < 0.0001f)
        {
            if (!MoveBack)
                return; // end point 
 
            movingToEnd = !movingToEnd;
            targetPos = movingToEnd ? endPoint.position : startPoint.position;
            pauseTimer = Pause;
        }

    }
    
    private void OnDrawGizmos()
    {
        if (!startPoint || !endPoint) return;
 
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(startPoint.position, endPoint.position);
        Gizmos.DrawSphere(startPoint.position, 0.2f);
        Gizmos.DrawWireSphere(endPoint.position, 0.2f);
    }

    
}
