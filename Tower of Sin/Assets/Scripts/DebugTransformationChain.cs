using UnityEngine;

public class DebugTransformChain : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== TRANSFORM CHAIN FOR " + gameObject.name + " ===");
        Debug.Log(GetHierarchyPath(transform));
        Debug.Log("Position: " + transform.position);
        Debug.Log("Rotation: " + transform.rotation.eulerAngles);
    }

    string GetHierarchyPath(Transform t)
    {
        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + " -> " + path;
        }

        return path;
    }
}