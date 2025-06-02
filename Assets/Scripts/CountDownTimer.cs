using UnityEngine;
using TMPro;

public class CountDownTimer : MonoBehaviour
{[Header("Timer settings")]
    public float totalTime = 60f;            // seconds
    public bool autoRestartOnZero = false;   // set true if you want it to loop

    float timeLeft;
    bool running;
    TextMeshPro txt;

    void Awake()
    {
        txt = GetComponent<TextMeshPro>();
        StartClock();
    }

    void StartClock()
    {
        timeLeft = totalTime;
        txt.text = Mathf.CeilToInt(totalTime).ToString("000");
        txt.color = Color.white;
        running = true;
    }

    void Update()
    {
        if (!running) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0) timeLeft = 0;

        txt.text = Mathf.CeilToInt(timeLeft).ToString("000");

        // Flash red for last 10 seconds
        if (timeLeft <= 10f)
            txt.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 4f, 1));

        if (timeLeft <= 0f)
        {
            running = false;
            if (autoRestartOnZero) StartClock();
            // Here you could later trigger lose logic or sound.
        }
    }
}
