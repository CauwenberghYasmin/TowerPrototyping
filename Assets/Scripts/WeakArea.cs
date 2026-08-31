using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
public class WeakArea : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private List<WeakAreaSegment> segments;

    public void OnSegmentInteracted()
    {
        foreach (var segment in segments)
        {
            if (!segment.Activated) return;
        }

        Destroy(gameObject);
    }
}
