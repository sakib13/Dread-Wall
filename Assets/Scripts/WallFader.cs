using UnityEngine;
using System.Collections;
public class WallFader : MonoBehaviour
{
    [Header("Wall Renderers (Assign in Inspector)")]
    public MeshRenderer[] walls;       // Drag each wall's MeshRenderer here (size = number of walls)

    [Header("Fade Settings")]
    public float fadeDuration = 1f;    // How many seconds to fade out
    public AudioSource audioSource;    // (Optional) AudioSource component for SFX
    public AudioClip fadeClip;         // (Optional) One-shot clip to play when fade starts

    // Internal flag, so we don’t re-trigger the fade while it’s already running
    private bool isFading = false;

    void Update()
    {
        // Listen for “F” key press to start fade (testing only)
        if (!isFading && Input.GetKeyDown(KeyCode.U))

        {
            Debug.Log("U key pressed → calling StartFade()");
            StartFade();
        }
    }

    /// <summary>
    /// Public method to begin the fade. 
    /// GameManager can call this later instead of using the “F” key.
    /// </summary>
    public void StartFade()
    {
        if (isFading) return;   // Already fading? Ignore.
        isFading = true;

        // Play SFX if assigned
        if (audioSource != null && fadeClip != null)
        {
            audioSource.PlayOneShot(fadeClip);
        }

        // Kick off the coroutine to fade materials to zero alpha
        StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        // Cache each wall’s material instance so we can adjust its alpha
        Material[] mats = new Material[walls.Length];
        for (int i = 0; i < walls.Length; i++)
        {
            mats[i] = walls[i].material; 
            // Ensure starting alpha = 1
            Color c = mats[i].GetColor("_BaseColor");
            c.a = 1f;
            mats[i].SetColor("_BaseColor", c);
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float newAlpha = Mathf.Lerp(1f, 0f, t);

            // Update alpha on every material
            for (int i = 0; i < mats.Length; i++)
            {
                Color c = mats[i].GetColor("_BaseColor");
                c.a = newAlpha;
                mats[i].SetColor("_BaseColor", c);
            }

            yield return null;
        }

        // After the fade completes, disable each wall GameObject (optional)
        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].gameObject.SetActive(false);
        }
    }
}
