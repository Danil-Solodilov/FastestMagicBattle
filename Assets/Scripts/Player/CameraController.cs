using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target; // ѕерсонаж, за которым камера будет следовать
    [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -10f); // —мещение камеры относительно персонажа
    [SerializeField] private float smoothSpeed = 0.45f; // —корость плавного следовани€

    void LateUpdate()
    {
        if (target == null) return;

        // ¬ычисл€ем желаемую позицию (позици€ персонажа + смещение)
        Vector3 desiredPosition = target.position + offset;

        // ѕлавно перемещаем камеру к желаемой позиции
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        //  амера всегда смотрит на персонажа
        transform.LookAt(target.position);
    }
}
