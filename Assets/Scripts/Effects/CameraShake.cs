using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.05f; // Меньше, чтобы было плавнее

    private Vector3 _initialPosition;
    private float _elapsedTime = 0f;
    private bool _isShaking = false;

    // Для Perlin Noise
    private float _offsetX;
    private float _offsetY;
    
    private CameraController _controller;

    public static CameraShake Instance;

    void Awake()
    {
        // Генерируем случайные начальные смещения для Perlin Noise
        _offsetX = Random.Range(0f, 1000f);
        _offsetY = Random.Range(0f, 1000f);

        _controller = GetComponent<CameraController>();
    }

    void LateUpdate() // Используем LateUpdate, чтобы применить тряску ПОСЛЕ того, как камера переместилась за игроком
    {
        if (_isShaking)
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= shakeDuration)
            {
                StopShake();
                return;
            }

            // Получаем значения Perlin Noise, которые плавно меняются со временем
            float timeFactor = _elapsedTime / shakeDuration; // От 0 до 1
            float noiseX = Mathf.PerlinNoise(_offsetX + timeFactor * 2, _offsetY); // Умножаем на 2, чтобы шум был более динамичным
            float noiseY = Mathf.PerlinNoise(_offsetX, _offsetY + timeFactor * 2);

            // Масштабируем шум до нужной амплитуды
            float xOffset = (noiseX - 0.5f) * 2f * shakeMagnitude; // Масштабируем от -1 до 1
            float yOffset = (noiseY - 0.5f) * 2f * shakeMagnitude;

            transform.position = _controller.currentPos + new Vector3(xOffset, yOffset, 0);
        }
    }

    public void StartShake(float magnitude, float duration)
    {
        shakeMagnitude = magnitude;
        shakeDuration = duration;
        // Если у нас есть скрипт следования, берем его текущую позицию как базу
        if (_controller != null)
        {
            _initialPosition = _controller.currentPos;
        }
        else
        {
            _initialPosition = transform.position; // Если нет скрипта следования
        }

        _elapsedTime = 0f;
        _isShaking = true;
    }

    private void StopShake()
    {
        _isShaking = false;
        transform.position = _controller.currentPos;
    }
}
