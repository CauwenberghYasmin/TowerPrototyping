using UnityEngine;

public class WeakAreaSegment : MonoBehaviour
{
    public bool Activated = false;

    private void OnTriggerEnter(Collider other)
    {
        Activated = true;
        GetComponentInParent<WeakArea>().OnSegmentInteracted();
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        if (box == null)
            return;

        Gizmos.color = Activated ? Color.green : Color.red;

        //Draw the collider using its local center/size, accounting for rotation and scale.
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(box.center, box.size);

        Gizmos.matrix = oldMatrix;
    }
}
