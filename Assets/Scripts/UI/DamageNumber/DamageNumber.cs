using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textElement;
    [SerializeField] private Image effectImage;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Vector3 randomOffset = new Vector3(0.5f, 0, 0.5f);

    [SerializeField] private List<Sprite> sprites;

    private float _timer;
    private Color _startColor;

    private IObjectPool<DamageNumber> _pool;

    public void Setup(float damageAmount, IObjectPool<DamageNumber> pool, bool isCritical, bool isBurning, bool isFrozen, bool IsStunning)
    {
        _pool = pool;
        textElement.text = Mathf.RoundToInt(damageAmount).ToString();
        _timer = 0;
        

        // Добавляем немного случайности в позицию, чтобы цифры не слипались
        transform.position += new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            0f,
            Random.Range(-randomOffset.z, randomOffset.z)
        );

        effectImage.gameObject.SetActive(true); // Показываем иконку

        if (isCritical)
        {
            textElement.color = Color.darkRed;
            effectImage.sprite = sprites[2];
        }
        else if (isBurning)
        {
            textElement.color = Color.darkOrange;
            effectImage.sprite = sprites[0];
        }
        else if (isFrozen)
        {
            textElement.color = Color.darkSlateBlue;
            effectImage.sprite = sprites[1];
        }
        else if (IsStunning)
        {
            textElement.color = Color.gray;
            effectImage.sprite = sprites[3];
        }
        else
        {
            textElement.color = new Color(255, 255, 0, 255);
            effectImage.gameObject.SetActive(false); // Прячем если эффектов нет
        }

        _startColor = textElement.color;
        // Поворачиваем текст к камере
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

        // Применяем прозрачность и к тексту, и к картинке
        textElement.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);
        effectImage.color = new Color(1f, 1f, 1f, alpha);

        if (_timer >= fadeDuration)
        {
            _pool.Release(this); // Возвращаем в пул вместо Destroy
        }
    }
}
