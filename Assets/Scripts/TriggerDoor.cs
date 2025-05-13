using UnityEngine;

public class TriggerDoor : MonoBehaviour
{
    [SerializeField] Animator doorAnim;
    bool opened;
  void OnTriggerEnter(Collider other)
    {
        if (opened) return;               // ignore repeats
        if (other.CompareTag("pickup"))   // our HandGrab cube prefab
        {
            opened = true;
            doorAnim.Play("DoorSlide", 0, 0f);   // play from the start
        }
    }
}
