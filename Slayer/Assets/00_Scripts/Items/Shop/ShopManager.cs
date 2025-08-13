using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ShopManager : MonoBehaviour
{
    [Header("아이템 풀")]
    public List<ShopItem_BaseSObj> weaponPool = new();
    public List<ShopItem_BaseSObj> accessoryPool = new();
    public List<ShopItem_BaseSObj> skillPool = new();

    [Header("UI 참조")]
    public ShopResultUI resultUI;
    public Button confirmButton;
    public Button pull1Button;
    public Button pull11Button;
    public Button pull35Button;

    [Header("Rarity Rates (기본)")]
    [Range(0, 1)] public float rateCommon = 0.6f;
    [Range(0, 1)] public float rateUncommon = 0.25f;
    [Range(0, 1)] public float rateRare = 0.1f;
    [Range(0, 1)] public float rateEpic = 0.04f;
    [Range(0, 1)] public float rateElite = 0.009f;
    [Range(0, 1)] public float rateUnique = 0.0009f;
    [Range(0, 1)] public float rateLegendary = 0.00009f;
    [Range(0, 1)] public float rateMythic = 0.00001f;

    [Header("카테고리별 확률 사용 여부")]
    public bool useCategorySpecificRates = false;
    public RarityRate weaponRates = RarityRate.Default();
    public RarityRate accessoryRates = RarityRate.Default();
    public RarityRate skillRates = RarityRate.Default();

    private ItemCategory lastCategory = ItemCategory.Weapon;

    [Serializable]
    public struct RarityRate
    {
        public float Common, Uncommon, Rare, Epic, Elite, Unique, Legendary, Mythic;
        public float Sum => Common + Uncommon + Rare + Epic + Elite + Unique + Legendary + Mythic;

        public static RarityRate Default() => new RarityRate
        {
            Common = 0.6f,
            Uncommon = 0.25f,
            Rare = 0.1f,
            Epic = 0.04f,
            Elite = 0.009f,
            Unique = 0.0009f,
            Legendary = 0.00009f,
            Mythic = 0.00001f
        };

        public float Get(Rarity r) => r switch
        {
            Rarity.Common => Common,
            Rarity.Uncommon => Uncommon,
            Rarity.Rare => Rare,
            Rarity.Epic => Epic,
            Rarity.Elite => Elite,
            Rarity.Unique => Unique,
            Rarity.Legendary => Legendary,
            Rarity.Mythic => Mythic,
            _ => 0f
        };
    }

    private void Awake()
    {
        // 버튼 초기화
        if (confirmButton != null) confirmButton.onClick.AddListener(OnClickConfirm);
        if (pull1Button != null) pull1Button.onClick.AddListener(() => OnClickPull(1));
        if (pull11Button != null) pull11Button.onClick.AddListener(() => OnClickPull(11));
        if (pull35Button != null) pull35Button.onClick.AddListener(() => OnClickPull(35));

        ToggleResultButtons(false);

        // ShopResultUI 이벤트 연결
        if (resultUI != null)
        {
            resultUI.OnAllShown += () => ToggleResultButtons(true);
            resultUI.OnClosed += () => ToggleResultButtons(false);
        }
    }

    // 버튼용 메서드
    public void PullWeaponOnce() { lastCategory = ItemCategory.Weapon; PullCategory(lastCategory, 1); }
    public void PullWeapon35() { lastCategory = ItemCategory.Weapon; PullCategory(lastCategory, 35); }
    public void PullAccessoryOnce() { lastCategory = ItemCategory.Accessory; PullCategory(lastCategory, 1); }
    public void PullAccessory35() { lastCategory = ItemCategory.Accessory; PullCategory(lastCategory, 35); }
    public void PullSkillOnce() { lastCategory = ItemCategory.Skill; PullCategory(lastCategory, 1); }
    public void PullSkill11() { lastCategory = ItemCategory.Skill; PullCategory(lastCategory, 11); }

    private void OnClickConfirm()
    {
        if (resultUI != null && resultUI.IsShowingResult())
            resultUI.ConfirmClose();
    }

    private void OnClickPull(int count)
    {
        StartCoroutine(Co_RePullAfterClose(lastCategory, count));
    }

    private IEnumerator Co_RePullAfterClose(ItemCategory category, int count)
    {
        if (resultUI != null && resultUI.IsShowingResult())
        {
            ToggleResultButtons(false);
            resultUI.SkipAnimation();
            yield return null;
            PullCategory(category, count);
            yield break;
        }
        PullCategory(category, count);
    }

    private void ToggleResultButtons(bool on)
    {
        if (confirmButton != null) confirmButton.gameObject.SetActive(on);
        if (pull1Button != null) pull1Button.gameObject.SetActive(on);

        bool show11 = on && lastCategory == ItemCategory.Skill;
        bool show35 = on && (lastCategory == ItemCategory.Weapon || lastCategory == ItemCategory.Accessory);

        if (pull11Button != null) pull11Button.gameObject.SetActive(show11);
        if (pull35Button != null) pull35Button.gameObject.SetActive(show35);
    }

    public void PullCategory(ItemCategory cat, int count)
    {
        if (count <= 0) return;

        ToggleResultButtons(false);

        var results = new List<ShopItem_BaseSObj>(count);
        for (int i = 0; i < count; i++)
        {
            var rolled = RollOne(cat);
            if (rolled != null)
            {
                results.Add(rolled);
                InventoryManager.Instance?.AddItem(rolled);
            }
        }

        if (results.Count > 0 && resultUI != null)
            resultUI.ShowResult(results);

        InventoryManager.Instance?.skillUI?.RefreshSkills();
    }

    private ShopItem_BaseSObj RollOne(ItemCategory cat)
    {
        var pool = GetPool(cat);
        if (pool == null || pool.Count == 0) return null;

        if (cat == ItemCategory.Skill)
            return pool[Random.Range(0, pool.Count)];

        var targetRarity = PickRarity(cat);
        var candidates = pool.Where(i => GetRarityOf(i) == targetRarity).ToList();

        if (candidates.Count == 0)
        {
            for (int r = (int)targetRarity - 1; r >= (int)Rarity.Common; r--)
            {
                var rr = (Rarity)r;
                candidates = pool.Where(i => GetRarityOf(i) == rr).ToList();
                if (candidates.Count > 0) break;
            }
        }
        if (candidates.Count == 0) candidates = pool;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private Rarity PickRarity(ItemCategory cat)
    {
        var rates = useCategorySpecificRates ? GetRates(cat) : GetDefaultRates();
        float sum = Mathf.Max(0.0001f, rates.Sum);
        float roll = UnityEngine.Random.value * sum;
        float cursor = 0f;
        foreach (Rarity r in Enum.GetValues(typeof(Rarity)))
        {
            cursor += rates.Get(r);
            if (roll <= cursor) return r;
        }
        return Rarity.Common;
    }

    private RarityRate GetDefaultRates() => new RarityRate
    {
        Common = rateCommon,
        Uncommon = rateUncommon,
        Rare = rateRare,
        Epic = rateEpic,
        Elite = rateElite,
        Unique = rateUnique,
        Legendary = rateLegendary,
        Mythic = rateMythic
    };
    private RarityRate GetRates(ItemCategory cat) => cat switch
    {
        ItemCategory.Weapon => weaponRates,
        ItemCategory.Accessory => accessoryRates,
        ItemCategory.Skill => skillRates,
        _ => GetDefaultRates()
    };
    private List<ShopItem_BaseSObj> GetPool(ItemCategory cat)
    {
        if (cat == ItemCategory.Weapon) return weaponPool;
        if (cat == ItemCategory.Accessory) return accessoryPool;
        if (cat == ItemCategory.Skill) return skillPool;
        return null;
    }
    private Rarity GetRarityOf(ShopItem_BaseSObj item)
    {
        if (item is Weapon_SObj w) return w.rarity;
        if (item is Accessory_SObj a) return a.rarity;
        return Rarity.Common;
    }
}
