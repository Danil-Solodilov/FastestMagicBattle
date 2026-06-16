using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChainLightning : MonoBehaviour
{
    [Header("Settings")]
    public float damage = 50f;
    public float jumpRange = 10f;    // Радиус поиска следующей цели
    public int maxJumps = 5;         // Макс. кол-во прыжков за один раз
    public float jumpDelay = 0.1f;   // Задержка между прыжками (визуальная)
    public float totalDuration = 10f; // Сколько секунд работает ульта
    public float strikeInterval = 1f; // Как часто выпускается новая цепь

    [Header("Visuals")]
    public GameObject lightningPrefab; // Префаб молнии

    private GameObject playerObj;
    private Transform _playerTransform;
    private List<LineRenderer> _activeLines = new List<LineRenderer>();

    void Start()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("ChainLightning: Player not found!");
            Destroy(gameObject); // Уничтожаем ульту, если игрока нет
            return;
        }
        _playerTransform = playerObj.transform;
    }

    public void Initialize(SkillData data)
    {
        totalDuration = data.duration;
        jumpRange = data.effectRadius;
        damage = data.damage;

        if (_playerTransform == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _playerTransform = playerObj.transform;
        }

        if (_playerTransform == null)
        {
            Debug.LogWarning("ChainLightning: Player not found on Initialize, aborting ultimate.");
            Destroy(gameObject);
            return;
        }

        // Начинаем корутину ТОЛЬКО после получения данных
        StartCoroutine(UltimateRoutine());
        Destroy(gameObject, totalDuration + 1f); // Самоудаление префаба
    }

    private void Update()
    {
        // Если игрок жив и есть трансформ, двигаем объект ульты за ним
        if (_playerTransform != null)
        {
            transform.position = _playerTransform.position;
        }
        else
        {
            // Если игрока вдруг нет, можно просто удалить ульту, чтобы не висела
            Destroy(gameObject);
        }
    }

    IEnumerator UltimateRoutine()
    {
        if (_playerTransform == null) yield break;
        float elapsed = 0;
        while (elapsed < totalDuration)
        {
            // Каждую секунду ищем цель и пускаем цепь
            Enemy firstTarget = FindNearestEnemy(_playerTransform.position, null);
            if (firstTarget != null)
            {
                StartCoroutine(SpawnChain(firstTarget));
            }

            yield return new WaitForSeconds(strikeInterval);
            elapsed += strikeInterval;
        }
    }

    // Пускаем молнию
    IEnumerator SpawnChain(Enemy firstTarget)
    {
        List<Enemy> hitEnemies = new List<Enemy>();
        Vector3 lastPos = _playerTransform.position;
        Enemy currentTarget = firstTarget;

        for (int i = 0; i < maxJumps; i++)
        {
            if (currentTarget == null) break;

            // --- ЛОГИКА ВИЗУАЛА МОЛНИИ ---
            Vector3 targetPos = currentTarget.transform.position;
            // Слегка приподнимаем позиции, чтобы молния била в центр тела, а не в пол
            Vector3 startPoint = lastPos + Vector3.up * 1f;
            Vector3 endPoint = targetPos + Vector3.up * 1f;

            // 1. Создаем эффект
            GameObject vfx = Instantiate(lightningPrefab, startPoint, Quaternion.identity);

            // 2. Направляем его на цель
            Vector3 direction = endPoint - startPoint;
            vfx.transform.forward = direction;

            // 3. Масштабируем по длине (если префаб это поддерживает)
            // Обычно в таких префабах длина регулируется по оси Z
            float distance = direction.magnitude;
            //Vector3 scale = vfx.transform.localScale;
            //scale.z = distance;
            //vfx.transform.localScale = scale;

            // 4. Наносим урон
            currentTarget.TakeDamage(damage);
            hitEnemies.Add(currentTarget);

            // 5. Авто-удаление эффекта через секунду (чтобы не засорять память)
            Destroy(vfx, 1f);

            // Запоминаем позицию для следующего прыжка
            lastPos = targetPos;

            // Ждем чуть-чуть перед следующим прыжком (эффект скорости молнии)
            yield return new WaitForSeconds(jumpDelay);

            // Ищем следующую цель (которую еще не били в этой цепи)
            currentTarget = FindNearestEnemy(lastPos, hitEnemies);
        }
    }

    // Ищем следующего врага
    Enemy FindNearestEnemy(Vector3 origin, List<Enemy> excludeList)
    {
        Enemy[] allEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy closest = null;
        float minDistance = jumpRange;

        foreach (Enemy e in allEnemies)
        {
            if (excludeList != null && excludeList.Contains(e)) continue;

            float dist = Vector3.Distance(origin, e.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = e;
            }
        }
        return closest;
    }
}
