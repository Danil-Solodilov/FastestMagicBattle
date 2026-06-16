using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Available Effects")]
    [SerializeField] private ImpactEffect impactPrefab; // Префаб для эффекта попадания пули куда-либо
    [SerializeField] private ImpactEffect explosionPrefab; // Префаб для взрыва от взрывных пуль
    [SerializeField] private ImpactEffect enemyExplosionPrefab; // Префаб взрыва для врагов-подрывателей
    [SerializeField] private GolemFireWaveSkill fireWaveProjectilePrefab; // Префаб огненной волны

    private ObjectPool<ImpactEffect> _impactPool;
    private ObjectPool<ImpactEffect> _explosionPool;
    private ObjectPool<ImpactEffect> _enemyexplosionPool;
    private ObjectPool<GolemFireWaveSkill> _fireWaveProjectilePool;

    void Awake()
    {
        Instance = this;

        if (impactPrefab != null)
        {
            _impactPool = new ObjectPool<ImpactEffect>(
                createFunc: () => {
                    ImpactEffect effect = Instantiate(impactPrefab);
                    effect.Setup(_impactPool);
                    return effect;
                },
                actionOnGet: (effect) => effect.gameObject.SetActive(true),
                actionOnRelease: (effect) => effect.gameObject.SetActive(false),
                actionOnDestroy: (effect) => Destroy(effect.gameObject),
                defaultCapacity: 20,
                maxSize: 100
            );
        }

        if (explosionPrefab != null)
        {
            _explosionPool = new ObjectPool<ImpactEffect>(
                createFunc: () => {
                    ImpactEffect effect = Instantiate(explosionPrefab);
                    effect.Setup(_explosionPool);
                    return effect;
                },
                actionOnGet: (effect) => effect.gameObject.SetActive(true),
                actionOnRelease: (effect) => effect.gameObject.SetActive(false),
                actionOnDestroy: (effect) => Destroy(effect.gameObject),
                defaultCapacity: 20,
                maxSize: 100
            );
        }
        if (enemyExplosionPrefab != null)
        {
            _enemyexplosionPool = new ObjectPool<ImpactEffect>(
                createFunc: () => {
                    ImpactEffect effect = Instantiate(enemyExplosionPrefab);
                    effect.Setup(_enemyexplosionPool);
                    return effect;
                },
                actionOnGet: (effect) => effect.gameObject.SetActive(true),
                actionOnRelease: (effect) => effect.gameObject.SetActive(false),
                actionOnDestroy: (effect) => Destroy(effect.gameObject),
                defaultCapacity: 20,
                maxSize: 100
            );
        }
        if (fireWaveProjectilePrefab != null)
        {
            _fireWaveProjectilePool = new ObjectPool<GolemFireWaveSkill>(
                createFunc: () => {
                    GolemFireWaveSkill wave = Instantiate(fireWaveProjectilePrefab);
                    wave.gameObject.SetActive(false); // Сначала неактивен
                    return wave;
                },
                actionOnGet: (wave) => {
                    wave.gameObject.SetActive(true);
                },
                actionOnRelease: (wave) => wave.gameObject.SetActive(false),
                actionOnDestroy: (wave) => Destroy(wave.gameObject),
                defaultCapacity: 5,
                maxSize: 10
            );
        }
    }

    public void SpawnImpact(Vector3 position)
    {
        if (impactPrefab == null) return;
        var effect = _impactPool.Get();
        effect.Play(position);
    }

    public void SpawnExplosion(Vector3 position)
    {
        if (explosionPrefab == null || _explosionPool == null)
        {
            // fallback на обычный эффект попадания если нет префаба/пула для взрыва
            SpawnImpact(position);
            return;
        }
        var effect = _explosionPool.Get();
        effect.Play(position);
    }

    public void SpawnEnemyExplosion(Vector3 position)
    {
        if (enemyExplosionPrefab == null || _enemyexplosionPool == null)
        {
            // fallback на обычный эффект попадания если нет префаба/пула для взрыва
            SpawnImpact(position);
            return;
        }
        var effect = _enemyexplosionPool.Get();
        effect.Play(position);
    }

    // Метод для запуска огненной волны врагом
    public GolemFireWaveSkill SpawnFireWave(Vector3 position, Quaternion rotation, float damage, float speed, float range, float returnDelay = 1.0f)
    {
        GolemFireWaveSkill effect = _fireWaveProjectilePool.Get();
        effect.transform.position = position;
        effect.transform.rotation = rotation;
        // Здесь мы передаем реальные данные для этого конкретного каста
        effect.Initialize(damage, speed, range, rotation * Vector3.forward, _fireWaveProjectilePool, returnDelay);
        return effect;
    }
}
