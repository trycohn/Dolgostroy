using UnityEngine;

public class IsoCameraFollow : MonoBehaviour
{
    [Header("За кем следить (Персонаж)")]
    public Transform target;

    [Header("Плавность камеры")]
    public float smoothTime = 0.15f;

    [Header("Зум камеры (меньше = ближе к игроку)")]
    public float gameplayZoomSize = 15f;

    [Header("Дистанция камеры от земли (чтобы не врезалась в дома)")]
    public float cameraDistance = 1000f;

    private Vector3 currentVelocity;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            // Гарантируем, что камера никогда не обрежет карту вблизи или вдали
            cam.nearClipPlane = -1000f;
            cam.farClipPlane = 5000f;

            if (gameplayZoomSize > 0)
                cam.orthographicSize = gameplayZoomSize;
        }

        if (target != null)
        {
            transform.position = target.position - transform.forward * cameraDistance;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Позволяет менять зум ползунком прямо во время игры!
        if (cam != null && gameplayZoomSize > 0)
            cam.orthographicSize = gameplayZoomSize;

        // Держим камеру всегда на высоте 1000 метров позади игрока по лучу взгляда
        Vector3 targetPosition = target.position - transform.forward * cameraDistance;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}