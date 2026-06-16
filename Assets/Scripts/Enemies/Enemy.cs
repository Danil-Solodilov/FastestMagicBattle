using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class Enemy : MonoBehaviour
{
    private float _health = 3f; //Здоровье врага
    private float _moveSpeed = 2f; // Скорость движения врага к игроку
    private float _experienceReward = 10f; // Сколько опыта дает враг
    private float _damage = 10f; //Урон врага
    private float _attackInterval = 1f; //Скорость атаки врага
    private float _nextAttackTime;
    private bool _isDead; //Проверка на смерть врага
    [SerializeField] private float hitRange = 2.5f;
    public bool IsDead => _isDead; // Публичный геттер для проверки извне


    [Header("Enemy Type Specific")]
    [SerializeField] private EnemyType enemyType; // Тип врага (задается в инспекторе префаба)
    [SerializeField] private float stoppingDistance;
    [SerializeField] private GameObject projectilePrefab; // Префаб снаряда
    [SerializeField] private GameObject skillPrefab; // Префаб скилла
    [SerializeField] private GameObject projectileSpawnPos; // Точка спавна снаряда
    [SerializeField] private float projectileSpeed; // Скорость полёта снаряда
    [SerializeField] private float projectileDamage; // Чтобы пуля врага могла наносить урон
    [SerializeField] private float skillDamage; // Урон скила
    [SerializeField] private float skillDuration; // Длительность действия скилла
    [SerializeField] private float skillRadius; // Область действия скилла
    [SerializeField] private float skillSpeed; // Скорость скилла
    private Vector3 _playerLastKnownPos; // Для стрельбы, если игрок уйдет из поля зрения

    [Header("Enemy Animation")]
    [SerializeField] private Animator _animator;

    // Переменные для эффектов от скиллов игрока
    public bool isFrozen = false;
    public bool isBurning = false;
    public bool isStunning = false;
    private float _baseSpeed; // Чтобы запомнить скорость до заморозки

    //Создание пула
    private IObjectPool<Enemy> _myPool;
    private float _currentHealth; // Используем для здоровья в рантайме

    //Визуализация
    [Header("VFX & UI")]
    [SerializeField] private EnemyHealthBar healthBar; // Ссылка на компонент слайдера
    [SerializeField] private Camera cam; // Камера
    [SerializeField] private ParticleSystem particle; // Система частиц
    [SerializeField] private GameObject damageNumberPos; // Для правильной позиции спавна цифр урона

    private Renderer _renderer;
    private Color _originalColor;
    private Coroutine _currentFreezeRoutine;
    private Coroutine _currentBurningRoutine;
    private Coroutine _currentStunningRoutine;

    //Необходимые ссылки
    private Transform _playerTransform; // Ссылка на игрока
    private PlayerStats _playerStats; // Ссылка на PlayerStats
    private NavMeshAgent _agent; // Ссылка на агент

    void Awake()
    {
        // Ищем Renderer в самом объекте или его детях
        _renderer = GetComponentInChildren<Renderer>();
        _agent = GetComponent<NavMeshAgent>(); // Получаем агент

        if (_animator == null) _animator = GetComponent<Animator>();

        // Запоминаем исходный цвет, чтобы к нему вернуться
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
        // Находим игрока при старте
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            _playerStats = _playerTransform.GetComponent<PlayerStats>();
        }

        _baseSpeed = _moveSpeed; //Запоминаем скорость врага
    }

    public void Setup(EnemyData data, IObjectPool<Enemy> pool)
    {
        _myPool = pool;

        // Настройка основных параметров
        _currentHealth = data.health; // Сбрасываем здоровье при выходе из пула!
        _health = data.health;
        _moveSpeed = data.speed;
        _experienceReward = data.experienceReward;
        _damage = data.damage;
        _attackInterval = data.attackInterval;
        _baseSpeed = _moveSpeed; // Обновляем базовую скорость врага

        // Выключаем агент, если он был включен
        if (_agent != null) _agent.enabled = false;

        // --- СБРОС ВСЕХ ЭФФЕКТОВ ПЕРЕД ИНИЦИАЛИЗАЦИЕЙ ---
        ResetAllEffects();

        // Настройки для конкретного типа врага
        enemyType = data.type;
        stoppingDistance = data.stoppingDistance;
        projectilePrefab = data.projectilePrefab;
        projectileSpeed = data.projectileSpeed;
        projectileDamage = data.damage;
        skillPrefab = data.skillPrefab;
        skillDamage = data.damage;

        _isDead = false; // Сброс флага при получении из пула

        // Настройка агента
        if (_agent != null)
        {
            _agent.enabled = true;
            _agent.speed = _moveSpeed;

            _agent.isStopped = false; // Гарантируем, что не остановлен при старте

            // Если враг дальнего боя или некромант — берем дистанцию из данных, иначе 0.5
            if (enemyType == EnemyType.Ranged || enemyType == EnemyType.Necromancer)
                _agent.stoppingDistance = stoppingDistance;
            else
                _agent.stoppingDistance = 0.5f;
        }

        // --- ИНИЦИАЛИЗАЦИЯ ХЕЛСБАРА ---
        if (healthBar != null)
        {
            healthBar.Setup(transform);
            healthBar.UpdateHealth(_currentHealth, _health);
            healthBar.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (_playerTransform == null || _isDead || isFrozen)
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                _agent.isStopped = true;
            return;
        }

        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        // 1. Управляем движением через NavMesh

        _agent.SetDestination(_playerTransform.position);

        if (distanceToPlayer <= _agent.stoppingDistance)
        {
            _agent.isStopped = true;
        }
        else
        {
            _agent.isStopped = false;
        }

        // Проверяем, готов ли враг атаковать прямо сейчас
        bool canAttackNow = (Time.time >= _nextAttackTime && distanceToPlayer <= _agent.stoppingDistance && CanSeePlayer());

        // Устанавливаем Idle только если мы НЕ двигаемся И НЕ собираемся атаковать
        bool isMoving = _agent.velocity.magnitude > 0.1f && !_agent.isStopped;

        if (HasParameter("Idle", _animator))
        {
            if (isMoving)
            {
                _animator.SetBool("Idle", false);
            }
            else if (canAttackNow)
            {
                // Если пора атаковать, выключаем Idle заранее, чтобы дать дорогу триггеру Attack
                _animator.SetBool("Idle", false);
            }
            else
            {
                _animator.SetBool("Idle", true);
            }
        }

        // 2. Логика для стрелков и колдунов
        if (enemyType == EnemyType.Ranged || enemyType == EnemyType.Necromancer)
        {
            if (distanceToPlayer <= _agent.stoppingDistance)
            {
                FaceTarget(_playerTransform.position);

                if (CanSeePlayer())
                {
                    if (enemyType == EnemyType.Ranged) HandleRangedAttack();
                    else StartAnimCastSkill();
                }
            }
        }
    }

    // Проверка виддимости игрока
    private bool CanSeePlayer()
    {
        Vector3 start = transform.position + Vector3.up * 1.5f;
        Vector3 end = _playerTransform.position + Vector3.up * 1.5f;
        Vector3 direction = (end - start).normalized;

        // Пускаем луч. Если он попал в препятствие раньше, чем в игрока - стрелять нельзя.
        if (Physics.Raycast(start, direction, out RaycastHit hit, stoppingDistance + 5f))
        {
            if (hit.collider.CompareTag("Player")) return true;
        }
        return false;
    }

    // Поворот к игроку
    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    // Этот метод будет вызываться пулей
    public void TakeDamage(float amount, bool isCritical = false)
    {
        if (_isDead) return; // Если враг уже мертв, игнорируем урон

        _currentHealth -= amount;

        //Пуск анимации получения урона
        if (enemyType != EnemyType.Exploder)
        {
            _animator.Play("GetHit", 0, 0f);
        }
        else
        {
            _animator.Play("GetHit", 1, 0f);
        }

        // --- ОБНОВЛЕНИЕ ХЕЛСБАРА ---
        if (healthBar != null)
        {
            healthBar.UpdateHealth(_currentHealth, _health);
        }

        // Используем глобальный менеджер цифр урона
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.SpawnDamageNumber(damageNumberPos.transform.position + Vector3.up, amount, isCritical, isBurning, isFrozen, isStunning);
        }

        //Отбрасывание врага
        if (enemyType == EnemyType.Melee && enemyType == EnemyType.Ranged)
        {
            _agent.velocity = (transform.position - _playerTransform.position).normalized * 2f;
        }
        else if (enemyType == EnemyType.Tank)
        {
            _agent.velocity = (transform.position - _playerTransform.position).normalized * 1.1f;
        }
        else
        {
            _agent.velocity = (transform.position - _playerTransform.position).normalized * 1.5f;
        }
        
        // Смерть врага
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    // Смерть
    private void Die()
    {
        if (_isDead)
        {
            return; // Дополнительная проверка, чтобы быть на 100% уверенным
        }

        // Скрываем полоску при смерти
        if (healthBar != null) healthBar.gameObject.SetActive(false);

        _isDead = true;      // Устанавливаем флаг, что враг мертв
        // Отключаем агент, чтобы он не держал позицию в пуле
        if (_agent != null)
        {
            // Проверяем, активен ли агент и находится ли он на NavMesh
            // прежде чем пытаться его остановить
            if (_agent.enabled && _agent.gameObject.activeInHierarchy && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
            _agent.enabled = false;
        }
        _renderer.material.color = _originalColor;
        Debug.Log("Враг уничтожен!");

        // Эффекты смерти, начисление опыта и т.д.
        if (_playerStats != null)
        {
            _playerStats.AddExperience(_experienceReward); // Добавляем опыт игроку
        }

        if (UISystem.Instance != null)
        {
            UISystem.Instance.UpdateKillCount();
        }

        // Вместо уничтожения — возвращаем в пул
        if (_myPool != null) _myPool.Release(this);
        else gameObject.SetActive(false); // На всякий случай
    }

    // ------ЛОГИКА АТАКИ ДЛЯ СТРЕЛКОВ-----

    // Пуск анимации стрельбы
    private void HandleRangedAttack()
    {
        if (projectilePrefab == null) return; // Нет пули - нет стрельбы

        if (Time.time >= _nextAttackTime)
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }
            _nextAttackTime = Time.time + _attackInterval;
        }
    }

    // Выстрел в нужный момент анимации
    public void ShootProjectile()
    {
        if (projectilePrefab == null || _isDead) return;

        // Определяем точку спавна (грудь врага)
        Vector3 heightOffset = Vector3.up * 1.5f; // Используем то же смещение
        Vector3 spawnPosition = projectileSpawnPos.transform.position;

        // Определяем точку цели (грудь игрока)
        // Убедимся, что игрок существует
        if (_playerTransform == null) return;
        Vector3 targetPosition = _playerTransform.position + heightOffset;

        // Вычисляем направление от груди до груди
        Vector3 shootDirection = (targetPosition - spawnPosition).normalized;

        // Создаем пулю
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(shootDirection));

        // Если у пули есть скрипт, передаем ей данные
        var enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            enemyProjectile.Setup(shootDirection, projectileSpeed, projectileDamage);
        }

        Debug.Log("Враг выстрелил!");
    }

    // Частицы для стрелков
    public void ShootingParticle()
    {
        if (particle != null)
        {
            particle.Play();
        }
    }

    // ------ЛОГИКА АТАКИ ДЛЯ ТАНКОВ И МИЛИШНИКОВ-----

    //Атаковать при приближении к игроку
    private void OnTriggerStay(Collider other)
    {
        // Если коснулись игрока
        if (other.CompareTag("Player"))
        {
            if (Time.time >= _nextAttackTime)
            {
                if (_playerStats != null)
                {
                    if (HasParameter("RandAttack", _animator))
                    {
                        _animator.SetInteger("RandAttack", Random.Range(0, 2));
                    }
                    else
                    {
                        _animator.SetTrigger("Attack");
                    }
                    _nextAttackTime = Time.time + _attackInterval;
                }
            }
        }
    }

    // Удар в нужный момент анимации
    public void punch()
    {
        if (_playerTransform == null || _isDead) return;

        if (particle != null)
        {
            particle.Play();
        }

        // Проверка дистанции до игрока
        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
        if (distanceToPlayer <= hitRange)
        {
            // Игрок получает урон если он не дальше нужной дистанции
            _playerStats.TakeDamage(_damage);
        }

        cam = Camera.main;
        // Тряска камеры если враг большой
        if (enemyType == EnemyType.Tank && cam != null)
        {
            cam.GetComponent<CameraShake>().StartShake(0.5f, 0.3f);
        }
    }

    // ------ЛОГИКА АТАКИ ДЛЯ ПОДРЫВНИКОВ-----
    // Взрыв в нужный момент анимации
    public void EnemyExplode()
    {
        VFXManager.Instance.SpawnEnemyExplosion(healthBar.transform.position);

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
        if (distanceToPlayer <= hitRange)
        {
            _playerStats.TakeDamage(_damage);
        }

        // Тряска камеры при взрыве
        cam = Camera.main;
        cam.GetComponent<CameraShake>().StartShake(0.5f, 0.3f);

        // Наносит урон и самому себе
        //TakeDamage(_damage);
        Die();
    }


    // ------ЛОГИКА АТАКИ ДЛЯ МАГОВ-----
    // Пуск анимации каста скила
    private void StartAnimCastSkill()
    {
        if (skillPrefab == null) return; // Нет скилла - нет каста

        if (Time.time >= _nextAttackTime)
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }
            _nextAttackTime = Time.time + _attackInterval;
        }
    }

    // Каст скила в нужный момент анимации
    public void CastSkill()
    {
        if (_playerTransform == null) return;
        // Определяем положение игрока ДЛЯ СПАВНА ЭФФЕКТА ПОД ИГРОКОМ
        Vector3 targetPosition = _playerTransform.position + Vector3.up * 0.3f;
        // Определяем направление от врага к игроку ДЛЯ СПАВНА НАПРАВЛЕННОГО ЭФФЕКТА ОТ ПРОТИВНИКА К ИГРОКУ
        Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0; // Для горизонтальной волны
        Quaternion rotation = Quaternion.LookRotation(directionToPlayer);

        // Если это огненная волна
        if (enemyType == EnemyType.Necromancer && skillPrefab != null && skillPrefab.GetComponent<GolemFireWaveSkill>() != null)
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnFireWave(
                    transform.position,
                    rotation,
                    skillDamage, // Урон из SkillData врага
                    skillSpeed, // Скорость волны
                    stoppingDistance,
                    1f// Дальность волны
                );
                Debug.Log("Некромант кастанул Огненную Волну!");
            }
        }
        else
        {
            // Создаем эффект скилла
            GameObject skill = Instantiate(skillPrefab, targetPosition, Quaternion.identity);

            // Если у скилла есть скрипт, передаем ему данные
            var lichSkill = skill.GetComponent<LichIceSkill>();
            if (lichSkill != null)
            {
                lichSkill.Setup(skillDamage, skillDuration, skillRadius);
            }
        }
    }

    // Проверка наличия нужного параметра в аниматоре
    private bool HasParameter(string paramName, Animator animator)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    // Проверка для врагов с несколькими типами анимаций атаки
    private void OnTriggerExit(Collider other)
    {
        // Если отошли от игрока
        if (other.CompareTag("Player") && HasParameter("RandAttack", _animator))
        {
            _animator.SetInteger("RandAttack", 4);
        }
    }

    // ЛОГИКА ЭФФЕКТОВ ВРАГА
    // Эффект заморозки от скилла игрока
    public void Freeze(float duration)
    {
        if (isFrozen || !gameObject.activeInHierarchy) return; // Уже заморожен
        if (_currentFreezeRoutine != null) StopCoroutine(_currentFreezeRoutine); // Остановить старую, если почему-то активна
        _currentFreezeRoutine = StartCoroutine(FreezeRoutine(duration)); // Сохранить ссылку
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;

        // ЛОГИКА ОСТАНОВКИ:
        _agent.isStopped = true; // Останавливаем NavMesh
        Debug.Log("Враг заморожен!");
        // Можно изменить цвет врага на синий
        _renderer.material.color = Color.blue;

        yield return new WaitForSeconds(duration);

        // ЛОГИКА РАЗМОРОЗКИ:
        _agent.isStopped = false;
        isFrozen = false;
        _renderer.material.color = _originalColor;
        _currentFreezeRoutine = null; // Обнулить ссылку после завершения
        Debug.Log("Враг разморожен.");
    }

    // Эффект воспламенения от скилла игрока
    public void Burning(float duration)
    {
        if (isBurning || !gameObject.activeInHierarchy) return;
        if (_currentBurningRoutine != null) StopCoroutine(_currentBurningRoutine);
        _currentBurningRoutine = StartCoroutine(BurningRoutine(duration));
    }

    private IEnumerator BurningRoutine(float duration)
    {
        isBurning = true;

        // ЛОГИКА ЗАМЕДЛЕНИЯ:
        _agent.speed = _baseSpeed * 0.5f; // Замедляем NavMesh
        // Можно изменить цвет врага на красный
        _renderer.material.color = Color.red;

        yield return new WaitForSeconds(duration);

        //Горение остановилось
        _agent.speed = _baseSpeed;
        isBurning = false;
        _renderer.material.color = _originalColor;
        _currentBurningRoutine = null;
    }

    // Эффект стана от скилла игрока
    public void Stunning(float duration)
    {
        if (isStunning || !gameObject.activeInHierarchy) return;
        if (_currentStunningRoutine != null) StopCoroutine(_currentStunningRoutine);
        _currentStunningRoutine = StartCoroutine(StunningRoutine(duration));
    }

    private IEnumerator StunningRoutine(float duration)
    {
        isStunning = true;

        // ЛОГИКА ОСТАНОВКИ:
        _agent.speed = 0f; // Останавливаем NavMesh
        Debug.Log("Враг Застанен!");

        // ДОБАВИТЬ АНИМАЦИЮ IDLE
        
        yield return new WaitForSeconds(duration);

        //Вышел из стана
        _agent.speed = _baseSpeed;
        isStunning = false;
        Debug.Log("Враг вышел из стана.");
        _currentStunningRoutine = null;
    }


    // Сброс всех корутин и флагов
    private void ResetAllEffects()
    {
        // 1. Останавливаем все активные корутины эффектов
        if (_currentFreezeRoutine != null) { StopCoroutine(_currentFreezeRoutine); _currentFreezeRoutine = null; }
        if (_currentBurningRoutine != null) { StopCoroutine(_currentBurningRoutine); _currentBurningRoutine = null; }
        if (_currentStunningRoutine != null) { StopCoroutine(_currentStunningRoutine); _currentStunningRoutine = null; }
        
        // 2. Сбрасываем все флаги состояния
        isFrozen = false;
        isBurning = false;
        isStunning = false;

        // 3. Сбрасываем визуальные изменения цвета (если были)
        if (_renderer != null)
        {
            _renderer.material.color = _originalColor;
        }

        // 4. Сбрасываем скорость NavMeshAgent
        if (_agent != null)
        {
            _agent.speed = _baseSpeed; // Важно сбросить до базовой скорости

            // Только если агент включен и на NavMesh, можно менять isStopped
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false; // Убедиться, что он не остановлен
            }
        }
    }
}
