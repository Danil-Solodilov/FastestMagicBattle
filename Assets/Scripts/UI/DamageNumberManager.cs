using UnityEngine;
using UnityEngine.Pool;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance; // Синглтон для легкого доступа

    [SerializeField] private DamageNumber damageNumberPrefab;
    private ObjectPool<DamageNumber> _pool;

    private void Awake()
    {
        if (Instance != null && Instance != this) // Проверка на дубликаты синглтона
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _pool = new ObjectPool<DamageNumber>(
            createFunc: () => {
                DamageNumber dn = Instantiate(damageNumberPrefab, transform);
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

    public void SpawnDamageNumber(Vector3 position, float amount)
    {
        DamageNumber dn = _pool.Get();
        dn.transform.position = position;
        dn.Setup(amount, _pool); // Передаем пул, чтобы цифра сама могла вернуться в него
    }
}
