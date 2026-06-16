using UnityEngine;
using UnityEngine.Pool;

public class ImpactEffect : MonoBehaviour
{
    private ParticleSystem _ps;
    private IObjectPool<ImpactEffect> _pool;
    private bool _isReturned; // Флаг, чтобы не возвращать в пул дважды

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();

        // Настраиваем систему частиц, чтобы она сообщала, когда закончила работу
        var main = _ps.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    public void Setup(IObjectPool<ImpactEffect> pool)
    {
        _pool = pool;
        _isReturned = false; // Сбрасываем флаг при получении из пула
    }

    public void Play(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
        _ps.Play();
    }

    // Этот метод Unity вызовет сам, когда частицы закончат играть
    void OnParticleSystemStopped()
    {
        // 1. Проверяем, не пустой ли пул (защита от ошибки)
        if (_pool == null)
        {
            // Если пула нет (объект создан просто через Instantiate), удаляем объект
            Destroy(gameObject);
            return;
        }

        // 2. Проверяем, не вернули ли мы его уже (защита от двойного вызова)
        if (_isReturned) return;

        _isReturned = true;
        _pool.Release(this);
    }
}
