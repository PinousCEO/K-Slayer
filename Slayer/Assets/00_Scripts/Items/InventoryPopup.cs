using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryPopup : MonoBehaviour
{
    public static InventoryPopup Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text requirementText;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button closeBtn;

    private InventoryManager.InventoryEntry currentEntry;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        root.SetActive(false);
        levelUpButton.onClick.AddListener(OnClickLevelUp);
        closeBtn.onClick.AddListener(Close);
    }

    public void Open(InventoryManager.InventoryEntry entry)
    {
        currentEntry = entry;

        icon.sprite = entry.item.icon;
        levelText.text = $"{entry.level}";
        countText.text = $"{entry.count}";

        int nextLevel = entry.level + 1;
        int required = nextLevel * 2;

        requirementText.text = $"{required}";
        levelUpButton.interactable = entry.count >= required && nextLevel <= 5;

        root.SetActive(true);
    }

    void OnClickLevelUp()
    {
        if (currentEntry != null)
            InventoryManager.Instance.LevelUp(currentEntry);
    }

    public void Close()
    {
        root.SetActive(false);
    }
}