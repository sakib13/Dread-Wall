using UnityEngine;

public class WallMover : MonoBehaviour
{
    [System.Serializable]
    public class WallInfo
    {
        public Transform wall;            // drag wall here
        public float moveDistance = 0.5f; // metres toward centre
        public Vector3 inwardDir = Vector3.forward; // into room
        [HideInInspector] public Vector3 startPos;
        [HideInInspector] public Vector3 targetPos;
    }

    public WallInfo[] walls;
    public float delayBeforeStart = 0f;   // set to 0 while testing
    public float moveDuration     = 5f;   // short so you can see it

    float t;
    bool moving;

    void Start()
    {
        foreach (var w in walls)
        {
            if (w.wall == null) continue;
            w.startPos  = w.wall.localPosition;
            w.targetPos = w.startPos + w.inwardDir.normalized * w.moveDistance;
        }
        Invoke(nameof(BeginMove), delayBeforeStart);
    }

    void BeginMove() => moving = true;

    void Update()
    {
        if (!moving) return;

        t += Time.deltaTime / moveDuration;
        float k = Mathf.SmoothStep(0f, 1f, t);

        foreach (var w in walls)
            if (w.wall) w.wall.localPosition = Vector3.Lerp(w.startPos, w.targetPos, k);

        if (t >= 1f) moving = false;
    }
}
