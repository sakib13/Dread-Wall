using Scripts;
using UnityEngine;
using Fusion;

public class TriggerDoor : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] int puzzleStage;
    
    [SerializeField] public Animator doorAnim;
    bool opened = false;

      void Start()
    {
        //if (doorAnim != null)
        //    doorAnim.enabled = false;     
    }
    private void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }
    void OnTriggerEnter(Collider other)
    {
        // Ignore collisions once we've already opened the door
        //if (opened) return;

        // Only react to the HandGrab cube (tagged "Pickup")
        if (!other.CompareTag("pickup")) return;
        else
        {
            //opened = true;
            gameManager.RPC_OnPuzzleSolved(puzzleStage);
            Debug.Log("Trigger Success! Moving to Stage:" + puzzleStage);
            
            
            //if (doorAnim != null)
            //{
            //    // Enable the Animator and play the slide clip from the beginning
            //    doorAnim.enabled = true;
            //    doorAnim.Play("DoorSlide", 0, 0f);
            //}
            
        }    
    }
}
