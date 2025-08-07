using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Objects/SkillData")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public Sprite skillIcon;
    public float cooldown;
    public string description;

    public GameObject effectPrefab;

    [Header("Damage Settings")]
    [Tooltip("기본 공격력의 몇 %인지 (예: 1.5 = 150%)")]
    public float damagePercentage = 1.0f;

    public Game_State state;
}
