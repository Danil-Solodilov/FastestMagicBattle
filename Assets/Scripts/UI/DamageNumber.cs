using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textElement;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Vector3 randomOffset = new Vector3(0.5f, 0, 0.5f);

    private float _timer;
    private Color _startColor;

    private IObjectPool<DamageNumber> _pool;

    public void Setup(float damageAmount, IObjectPool<DamageNumber> pool)
    {
        _pool = pool;
        textElement.text = Mathf.RoundToInt(damageAmount).ToString();
        _startColor = textElement.color;
        _timer = 0;

        // Добавляем немного случайности в позицию, чтобы цифры не слипались
        transform.position += new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(1f, 2f), // чуть выше врага
            Random.Range(-randomOffset.z, randomOffset.z)
        );

        // Поворачиваем текст к камере (опционально)
        if (Camera.main != null)
            transform.LookAt(transform.position + Camera.main.transform.forward);
    }

    void Update()
    {
        // Двигаем вверх
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Плавное исчезновение
        _timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1, 0, _timer / fadeDuration);
        textElement.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);

        if (_timer >= fadeDuration)
        {
            _pool.Release(this); // Возвращаем в пул вместо Destroy
        }
    }
}
