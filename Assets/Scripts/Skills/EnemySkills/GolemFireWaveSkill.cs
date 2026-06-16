using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Для HashSet
using UnityEngine.Pool; // Если будете использовать пул

public class GolemFireWaveSkill : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private float _range;
    private Vector3 _direction;

    private float _returnDelay = 1.0f; // Задержка перед возвратом в пул
    private Coroutine _returnCoroutine; // Ссылка на корутину возврата

    [SerializeField] private LayerMask playerLayer; // Слой игрока
    private HashSet<PlayerStats> _hitPlayers = new HashSet<PlayerStats>(); // Чтобы не бить одного и того же игрока несколько раз

    // Добавляем пул для управления эффектами (если будет использоваться)
    private IObjectPool<GolemFireWaveSkill> _pool;

    // Инициализация для вражеского скилла
    public void Initialize(float damage, float speed, float range, Vector3 direction, IObjectPool<GolemFireWaveSkill> pool = null, float returnDelay = 1.0f)
    {
        _damage = damage;
        _speed = speed;
        _range = range;
        _direction = direction.normalized;
        _pool = pool; // Сохраняем ссылку на пул
        _returnDelay = returnDelay; // Сохраняем задержку

        // Сбрасываем список пораженных игроков при инициализации
        _hitPlayers.Clear();

        // Запускаем движение волны
        StartCoroutine(MoveWave());
    }

    private IEnumerator MoveWave()
    {
        Vector3 startPosition = transform.position;
        float distanceCovered = 0;

        while (distanceCovered < _range)
        {
            float step = _speed * Time.deltaTime;
            transform.position += _direction * step;
            distanceCovered += step;

            yield return null;
        }

        // После достижения нужной дистанции, запускаем корутину задержки перед возвратом
        _returnCoroutine = StartCoroutine(ReturnToPoolWithDelay(_returnDelay));
    }

    private IEnumerator ReturnToPoolWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
        _returnCoroutine = null; // Обнуляем ссылку после завершения
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли коллайдер игроком
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            PlayerStats player = other.GetComponent<PlayerStats>();
            if (player != null && !_hitPlayers.Contains(player))
            {
                player.TakeDamage(_damage); // Враг наносит урон напрямую
                _hitPlayers.Add(player);
            }
        }
    }

    // Важно: Если объект будет деактивирован до завершения корутины,
    // её нужно остановить. Добавьте это в OnDisable.
    void OnDisable()
    {
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }
    }
}