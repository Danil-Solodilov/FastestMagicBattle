using UnityEngine;
using UnityEngine.Pool;

public class HealNumberManager : MonoBehaviour
{
    public static HealNumberManager Instance; // Синглтон для легкого доступа

    [SerializeField] private HealNumber healNumberPrefab;
    private ObjectPool<HealNumber> _pool;

    private void Awake()
    {
        if (Instance != null && Instance != this) // Проверка на дубликаты синглтона
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _pool = new ObjectPool<HealNumber>(
            createFunc: () => {
                HealNumber dn = Instantiate(healNumberPrefab, transform);
                dn.gameObject.SetActive(false); // Активируем при Get
                return dn;
            },
            actionOnGet: (obj) => obj.gameObject.SetActive(true),
            actionOnRelease: (obj) => obj.gameObject.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj.gameObject),
            defaultCapacity: 20,
            maxSize: 100
        );
    }

    public void SpawnHealNumber(Vector3 position, float amount)
    {
        HealNumber dn = _pool.Get();
        dn.transform.position = position;
        dn.Setup(amount, _pool); // Передаем пул, чтобы цифра сама могла вернуться в него
    }
}
