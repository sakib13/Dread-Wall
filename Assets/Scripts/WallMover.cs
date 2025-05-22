using UnityEngine;

public class WallMover : MonoBehaviour
{
    [System.Serializable]
    public class WallInfo
    {
        [Header("Drag the wall mesh here")]
        public Transform wall;            // wall to move
        [Header("How far, in metres, to move inward")]
        public float moveDistance = 0.5f; // slide amount
        [Header("Direction pointing INTO the room (local world space)")]
        public Vector3 inwardDir = Vector3.forward;

        // cached at runtime
        [HideInInspector] public Vector3 startPos;
        [HideInInspector] public Vector3 targetPos;
    }

    [Header("Walls to move")]
    public WallInfo[] walls;

    [Header("Timing")]
    public float delayBeforeStart = 10f; // seconds to wait
    public float moveDuration     = 90f; // seconds to finish slide

    float t;            // 0-1 lerp factor
    bool moving = false;

    void Start()
    {
        // cache start & target positions
        foreach (var w in walls)
        {
            if (w.wall == null) continue;
            w.startPos  = w.wall.localPosition;
            w.targetPos = w.startPos + w.inwardDir.normalized * w.moveDistance * -1f;
            // multiply by -1 because inwardDir should point INTO the room
        }
        Invoke(nameof(BeginMove), delayBeforeStart);
    }

    void BeginMove() => moving = true;

    void Update()
    {
        if (!moving) return;

        t += Time.deltaTime / moveDuration;
        float k = Mathf.SmoothStep(0f, 1f, t);           // easing curve

        foreach (var w in walls)
            if (w.wall) w.wall.localPosition = Vector3.Lerp(w.startPos, w.targetPos, k);

        if (t >= 1f) moving = false; // finished
    }

    // Call this from another script (e.g., your timer) if you want to start early
    public void TriggerEarly() { if (!moving) BeginMove(); }
}
