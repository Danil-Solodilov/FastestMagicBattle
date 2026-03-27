using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public GameObject prefab;
    public float health = 10f;
    public float speed = 2f;
    public float experienceReward = 10f;
    public float damage = 10f;
    public float attackInterval = 1f;
    //public Color bodyColor = Color.white; // Чтобы визуально отличать их, если префаб один
    public int unlockAtLevel = 1;         // На каком уровне игрока этот враг начинает появляться
}
