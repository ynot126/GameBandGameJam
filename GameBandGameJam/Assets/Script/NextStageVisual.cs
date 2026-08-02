using UnityEngine;

public class NextStageVisual : MonoBehaviour
{
    public void Initialize(Vector3 damagePosition)
    {
        var cam = Camera.main;
        if (!cam)
        {
            Debug.LogWarning("No camera found");
            return;
        }

        transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
            cam.transform.rotation * Vector3.up);

        transform.position = damagePosition;
    }
}
