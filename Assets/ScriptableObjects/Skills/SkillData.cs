using UnityEngine;

public enum SkillType { Self, TargetDirection, GroundTarget}

[CreateAssetMenu(fileName = "New Skill", menuName = "Game/Skills/Skill Data")]

public class SkillData : ScriptableObject
{
    public string skillName; //Название скила
    public Sprite icon; //Иконка скила
    public SkillType type; //Тип скила
    public float cooldown; //Кулдаун скила
    public float range; // Дальность (для стрелки или круга)
    public float effectRadius; // Радиус воздействия (для АоЕ)
    public float damage; // Урон скилла
    public float duration; // Длительность скилла
    public GameObject effectPrefab; // Префаб самого заклинания (щит, шторм и т.д.)
    public bool isUlt;

    [TextArea] public string description;
}
