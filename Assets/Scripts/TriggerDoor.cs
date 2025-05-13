using UnityEngine;

public class TriggerDoor : MonoBehaviour
{
    [SerializeField] Animator doorAnim;
    bool opened = false;
      void Start()
    {
        if (doorAnim != null)
            doorAnim.enabled = false;     
    }

    void OnTriggerEnter(Collider other)
    {
        // Ignore collisions once we've already opened the door
        if (opened) return;

        // Only react to the HandGrab cube (tagged "Pickup")
        if (!other.CompareTag("pickup")) return;

        opened = true;

        // Enable the Animator and play the slide clip from the beginning
        doorAnim.enabled = true;
        doorAnim.Play("DoorSlide", 0, 0f);
    }
}
