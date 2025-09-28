using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;   // Player ที่จะตาม
    [SerializeField] float smoothSpeed = 5f;
    [SerializeField] Vector3 offset = new Vector3(0, 0, -10);

    void LateUpdate()
    {
        if (target == null) return;

        // จุดที่อยากให้กล้องไปอยู่
        Vector3 desiredPosition = target.position + offset;

        // ขยับแบบนุ่มนวล
        Vector3 smoothed = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // บังคับ z = -10 เสมอ (2D camera)
        smoothed.z = -10f;

        transform.position = smoothed;
    }
}
