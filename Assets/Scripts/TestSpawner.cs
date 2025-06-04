using UnityEngine;

public class TestSpawner : MonoBehaviour
{
    public GameObject testCube;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Instantiate(testCube, new Vector3(0, 1, 2), Quaternion.identity);
            Debug.Log("Spawned test cube manually.");
        }
    }
}
