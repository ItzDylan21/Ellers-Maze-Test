using UnityEngine;
using UnityEngine.XR;

public class LookAtPlayer : MonoBehaviour
{
    public Transform playerCamera;

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        // Make the UI face the player camera
        Vector3 direction = (playerCamera.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
    }
}