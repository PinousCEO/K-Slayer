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
    [Tooltip("�⺻ ���ݷ��� �� %���� (��: 1.5 = 150%)")]
    public float damagePercentage = 1.0f;

    public Game_State state;
}
