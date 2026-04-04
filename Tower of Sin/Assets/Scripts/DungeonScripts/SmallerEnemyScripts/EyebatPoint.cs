using UnityEngine;
using System.Collections.Generic;

public class EyebatPoint : MonoBehaviour
{
    [Tooltip("Indexes of connected points in the Watcher1 child list.")]
    public List<int> neighborIndices = new List<int>();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.15f);
    }
}