using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LeapSkill : MonoBehaviour
{
    private Transform _playerTransform;
    private PlayerStats _playerStats;
    private UnityEngine.AI.NavMeshAgent _agent;
    private MonoBehaviour _playerMovementScript;

    private float _damage;
    private float _range;
    private Vector3 _direction;
    private float _leapSpeed = 30f; // Скорость рывка

    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask enemyLayer;

    private List<Enemy> _hitEnemies = new List<Enemy>();
    private bool _isLeaping = false;

    public void Initialize(float damage, float range, Vector3 direction)
    {
        _damage = damage;
        _range = range;
        _direction = direction.normalized;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerStats = player.GetComponent<PlayerStats>();
            _agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
            _playerMovementScript = player.GetComponent<PlayerMovement>();

            StartCoroutine(PerformLeap());
        }
        else
        {
            Destroy(gameObject);
        }
        transform.Rotate(0, 0, 90);
    }

    private IEnumerator PerformLeap()
    {
        _isLeaping = true;

        // 1. Подготовка: выключаем управление и включаем неуязвимость
        if (_playerMovementScript != null) _playerMovementScript.enabled = false;
        if (_agent != null) _agent.enabled = false;
        if (_playerStats != null) _playerStats.isInvincible = true;

        Vector3 startPos = _playerTransform.position;

        // 2. Проверка препятствий (чтобы не влететь в стену)
        // Пускаем луч толщиной с персонажа
        if (Physics.SphereCast(startPos + Vector3.up, 0.5f, _direction, out RaycastHit hit, _range, obstacleLayer))
        {
            _range = hit.distance - 0.5f; // Останавливаемся чуть раньше стены
        }

        Vector3 targetPos = startPos + _direction * _range;
        float elapsedTime = 0;
        float duration = _range / _leapSpeed;

        // 3. Сам процесс рывка
        while (elapsedTime < duration)
        {
            // Двигаем игрока
            _playerTransform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / duration);

            // Проверка врагов вокруг игрока во время полета
            CheckForEnemies();

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _playerTransform.position = targetPos;

        // 4. Завершение: возвращаем управление
        if (_playerStats != null) _playerStats.isInvincible = false;
        if (_agent != null)
        {
            _agent.enabled = true;
            _agent.Warp(_playerTransform.position);
        }
        if (_playerMovementScript != null) _playerMovementScript.enabled = true;

        _isLeaping = false;
        Destroy(gameObject, 0.4f); // Удаляем объект логики скилла
    }

    private void CheckForEnemies()
    {
        // Ищем врагов в небольшом радиусе вокруг игрока
        Collider[] enemies = Physics.OverlapSphere(_playerTransform.position + Vector3.up, 1.5f, enemyLayer);

        foreach (var col in enemies)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null && !_hitEnemies.Contains(enemy))
            {
                // Наносим урон с учетом множителя игрока
                float finalDamage = _damage * (_playerStats != null ? _playerStats.damageMultiplier : 1f);
                enemy.TakeDamage(finalDamage, _playerStats.RollCritical());

                _hitEnemies.Add(enemy); // Чтобы не бить одного и того же дважды за рывок
            }
        }
    }
}
