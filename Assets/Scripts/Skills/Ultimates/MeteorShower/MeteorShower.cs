using UnityEngine;
using System.Collections;

public class MeteorShower : MonoBehaviour
{
    public GameObject meteorPrefab;
    public float duration = 8f;        // Длительность дождя
    public float spawnInterval = 0.3f; // Как часто падает метеор
    public float areaRadius = 20f;     // Радиус области поражения
    public float spawnHeight = 20f;    // Высота, с которой летят метеоры

    private Transform _playerTransform;

    private SkillData _savedData; // Сохраняем данные для передачи метеорам

    void Start()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void Initialize(SkillData data)
    {
        _savedData = data;
        duration = data.duration;
        areaRadius = data.effectRadius;

        StartCoroutine(ShowerRoutine());
        Destroy(gameObject, duration + 2f);
    }

    void Update()
    {
        // Ульта следует за игроком
        if (_playerTransform != null)
            transform.position = _playerTransform.position;
    }

    IEnumerator ShowerRoutine()
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            SpawnMeteor();
            yield return new WaitForSeconds(spawnInterval);
            elapsed += spawnInterval;
        }
    }

    void SpawnMeteor()
    {
        // 1. Находим случайную точку на земле внутри круга
        Vector2 randomCircle = Random.insideUnitCircle * areaRadius;
        Vector3 targetPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // 2. Определяем точку старта в небе
        // Можно добавить небольшой наклон, чтобы они летели под углом
        Vector3 spawnPos = targetPos + new Vector3(5f, spawnHeight, 5f);

        // 3. Создаем метеор
        GameObject meteorGO = Instantiate(meteorPrefab, spawnPos, Quaternion.LookRotation(targetPos - spawnPos));

        // 4. Передаем ему точку назначения
        MeteorProjectile projectile = meteorGO.GetComponent<MeteorProjectile>();
        if (projectile != null && _savedData != null)
        {
            projectile.targetPoint = targetPos;
            projectile.Initialize(_savedData.damage, _savedData.effectRadius); // Передаем урон из сохраненных данных ульты
        }
    }

    // Для визуализации радиуса в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, areaRadius);
    }
}
