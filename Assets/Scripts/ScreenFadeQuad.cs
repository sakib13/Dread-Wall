using UnityEngine;
using System.Collections;

public class ScreenFadeQuad : MonoBehaviour
{ public float delay = 0f;      // seconds before fade starts
    public float fadeTime = 2f;   // seconds to go from black→clear

    Material mat;
    void Awake() { mat = GetComponent<MeshRenderer>().material; }

    void Start() { StartCoroutine(FadeOut()); }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(delay);
        Color c = mat.color;
        for (float t = 0; t < 1f; t += Time.deltaTime / fadeTime)
        {
            c.a = 1f - t;
            mat.color = c;
            yield return null;
        }
        gameObject.SetActive(false);    // disable when done
    }
}
