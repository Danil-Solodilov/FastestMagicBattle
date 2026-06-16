using UnityEngine;

public enum EnemyType { Melee, Ranged, Exploder, Tank, Necromancer }

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public EnemyType type; // Тип врага
    public GameObject prefab;
    public float health = 10f;
    public float speed = 2f;
    public float experienceReward = 10f;
    public float damage = 10f;
    public float attackInterval = 1f;
    public int unlockAtLevel = 1;         // На каком уровне игрока этот враг начинает появляться

    public float stoppingDistance = 5f; // Как близко к игроку остановится враг (для стрелков)
    public GameObject projectilePrefab; // Префаб снаряда (для стрелков)
    public GameObject skillPrefab; // Префаб скилла (для колдунов)
    public float projectileSpeed = 10f; // Скорость пули врага
}
