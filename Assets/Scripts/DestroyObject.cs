using UnityEngine;

public class DestroyObject : MonoBehaviour
{
   [Header("Destroy Settings")]
    [Tooltip("Seconds to wait before destroying this GameObject automatically. \nSet to 0 or negative to disable auto‐destroy.")]
    public float delaySeconds = 5f;

    // Public flag to let your teammate disable the automatic timer if needed
    public bool autoDestroyEnabled = true;

    void Start()
    {
        // If autoDestroyEnabled is true and delaySeconds > 0, schedule the destruction
        if (autoDestroyEnabled && delaySeconds > 0f)
        {
            Destroy(gameObject, delaySeconds);
        }
    }

    /// <summary>
    /// Public method that immediately destroys this GameObject.
    /// GameManager can call this at any time.
    /// </summary>
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
