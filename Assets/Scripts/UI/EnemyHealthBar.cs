using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private Transform _cam;
    private Transform _target;

    public void Setup(Transform target)
    {
        _target = target;
        _cam = Camera.main.transform;
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        slider.value = currentHealth / maxHealth;
    }

    void LateUpdate()
    {
        if (_target == null) return;

        // Поворачиваем к камере (Billboard эффект)
        transform.LookAt(transform.position + _cam.forward);
    }
}
