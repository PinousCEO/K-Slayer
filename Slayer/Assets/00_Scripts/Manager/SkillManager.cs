using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : Singleton<SkillManager>
{
    [System.Serializable]
    public class SkillSlot
    {
        public SkillBase skillScript;      // ½ºÅ³ MonoBehaviour
    }

    [SerializeField]
    private List<SkillSlot> skillSlots = new(); // ½½·Ô ¿©·¯ °³

    public Action<SkillData> OnSkillEquipment;
    public Player playerTransform;

    private void Start()
    {
        if (playerTransform == null)
            playerTransform = FindFirstObjectByType<Player>();
    }

    public List<SkillSlot> GetSlots()
    {
        return skillSlots;
    }
    private void Update()
    {
        foreach (var slot in skillSlots)
        {
            if (slot.skillScript == null) continue;

            slot.skillScript.UpdateCooldown(Time.deltaTime);

            if (slot.skillScript.IsReady && GameManager.Instance.game_State == slot.skillScript.skillData.state)
            {
                slot.skillScript.TryActivate();
            }
        }
    }
    public void EquipSkill(int slotIndex, SkillData skillData)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Count)
        {
            Debug.LogWarning("Àß¸øµÈ ½½·Ô ÀÎµ¦½º");
            return;
        }

        SkillSlot slot = skillSlots[slotIndex];

        if (slot.skillScript != null)
            Destroy(slot.skillScript.gameObject);

        GameObject skillGO = new GameObject($"Skill_{skillData.skillName}");
        skillGO.transform.SetParent(this.transform);

        SkillBase skill = skillGO.AddComponent(GetSkillScriptType(skillData.name)) as SkillBase;
        skill.Initialize(skillData);
        skill.player = playerTransform;

        slot.skillScript = skill;
        OnSkillEquipment?.Invoke(skillData);
    }

    private System.Type GetSkillScriptType(string skillName)
    {
        switch (skillName)
        {
            case "Skill01": return typeof(Skill01);
            case "Skill02": return typeof(Skill02);
            case "Skill03": return typeof(Skill03);
            case "Skill04": return typeof(Skill04);
            case "Skill05": return typeof(Skill05);
            case "Skill06": return typeof(Skill06);
            case "Skill07": return typeof(Skill07);
            case "Skill08": return typeof(Skill08);
            default: return null;
        }
    }
}
