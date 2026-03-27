using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private List<EnemyData> allEnemies; // Список всех возможных типов врагов (SO)

    [Header("Settings")]
    //[SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 1.5f; // Каждые 1.5 секунды
    [SerializeField] private float spawnRadius = 45f; // Расстояние от игрока

    private float _timer;

    // Словарь, где ключ — префаб, а значение — его персональный пул
    private Dictionary<GameObject, ObjectPool<Enemy>> _enemyPools = new Dictionary<GameObject, ObjectPool<Enemy>>();

    void Update()
    {
        if (playerStats.transform.position == null) return;

        _timer += Time.deltaTime;

        if (_timer >= spawnInterval)
        {
            SpawnEnemy();
            _timer = 0;

            // Постепенно ускоряем спавн, чтобы было сложнее
            spawnInterval = Mathf.Max(0.5f, spawnInterval - 0.01f);
        }
    }
    private void SpawnEnemy()
    {
        // 1. Находим текущий уровень игрока
        int currentLevel = playerStats.currentLevel + 1;

        // 2. Фильтруем врагов: только те, чей уровень разблокировки <= уровню игрока
        var availableEnemies = allEnemies.Where(e => e.unlockAtLevel <= currentLevel).ToList();

        if (availableEnemies.Count == 0) return;

        // 3. Выбираем врага. 
        // Логика: чем выше уровень разблокировки врага относительно текущего уровня, тем выше шанс.
        EnemyData selectedEnemyData = GetWeightedRandomEnemy(availableEnemies, currentLevel);

        // Получаем или создаем пул для этого типа врага
        ObjectPool<Enemy> pool = GetPoolForPrefab(selectedEnemyData.prefab);

        Enemy enemy = pool.Get();

        // Выбираем случайную точку на окружности вокруг игрока
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPos = new Vector3(randomCircle.x, 0, randomCircle.y) + playerStats.transform.position;

        //GameObject enemyObj = Instantiate(selectedEnemyData.prefab, spawnPos, Quaternion.identity);
        //Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        //enemyScript.Setup(selectedEnemyData);
        // В будущем здесь тоже нужно использовать Object Pool для врагов!

        enemy.transform.position = spawnPos;
        enemy.Setup(selectedEnemyData, pool); // Передаем пул во врага
    }

    private ObjectPool<Enemy> GetPoolForPrefab(GameObject prefab)
    {
        if (!_enemyPools.ContainsKey(prefab))
        {
            _enemyPools[prefab] = new ObjectPool<Enemy>(
                createFunc: () => Instantiate(prefab).GetComponent<Enemy>(),
                actionOnGet: (e) => e.gameObject.SetActive(true),
                actionOnRelease: (e) => e.gameObject.SetActive(false),
                defaultCapacity: 20,
                maxSize: 100
            );
        }
        return _enemyPools[prefab];
    }

    //Расчёт шанса спавна врага
    private EnemyData GetWeightedRandomEnemy(List<EnemyData> enemies, int playerLevel)
    {
        // Весовая система:
        // У каждого врага есть "вес". Чем больше разница между уровнем игрока и уровнем разблокировки врага,
        // тем чаще спавнятся "старые" враги... НО мы сделаем наоборот:
        // Дадим больше веса "новым" врагам.

        float totalWeight = 0;
        List<float> weights = new List<float>();

        foreach (var enemy in enemies)
        {
            // Пример формулы веса:
            // Базовый вес 1. Плюс бонус за близость к текущему уровню.
            // Чем ближе unlockAtLevel к playerLevel, тем выше шанс.
            float weight = 1f + (enemy.unlockAtLevel * 2f);

            // Если игрок уровня 10, а враг разблокировался на 1 уровне, его вес будет 3.
            // Если враг разблокировался на 10 уровне, его вес будет 21. 
            // Значит новый враг будет появляться в 7 раз чаще старого.

            weights.Add(weight);
            totalWeight += weight;
        }

        // Рандомный выбор по весам
        float randomValue = Random.Range(0, totalWeight);
        float currentSum = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            currentSum += weights[i];
            if (randomValue <= currentSum)
                return enemies[i];
        }

        return enemies[enemies.Count - 1];
    }
}
