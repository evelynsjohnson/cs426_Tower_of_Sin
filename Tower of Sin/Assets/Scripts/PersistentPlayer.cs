using UnityEngine;

public class PersistentPlayer : MonoBehaviour
{
    public static PersistentPlayer instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this; 

            transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}