using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.VisualScripting;

[System.Serializable]
public class PageData
{
    public BottomBar pageType;
    public GameObject pageObject;
    public Button button;

    [HideInInspector] public Image buttonImage;
}

public enum BottomBar
{
    Default,
    Skills,
    Equipment,
    Company,
    Adventure,
    Shop
}

public class UIManager : MonoBehaviour
{
    [SerializeField] private List<PageData> pageList = new();
    private Dictionary<BottomBar, GameObject> pageMap = new();
    private Dictionary<BottomBar, PageData> pageDataMap = new();
    private BottomBar? currentPageType = null;
    private GameObject currentPageObject = null;

    public Sprite normalSprite;
    public Sprite selectedSprite;

    public static UIManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        Init();
    }

    void Init()
    {
        foreach (var page in pageList)
        {
            if (page.pageObject != null && !pageMap.ContainsKey(page.pageType))
                pageMap.Add(page.pageType, page.pageObject);

            if (!pageDataMap.ContainsKey(page.pageType))
                pageDataMap.Add(page.pageType, page);

            if (page.button != null)
            {
                var capturedPage = page.pageType;
                page.button.onClick.RemoveAllListeners();
                page.button.onClick.AddListener(() => ShowPage(capturedPage));

                page.buttonImage = page.button.GetComponent<Image>();
            }

            page.pageObject.SetActive(false);
        }


        ShowPage(BottomBar.Default);
    }

    public void ShowPage(BottomBar target)
    {
        if (currentPageType == target)
            return;

        if (currentPageObject != null)
            currentPageObject.SetActive(false);

        if (pageMap.TryGetValue(target, out var targetPage))
        {
            targetPage.SetActive(true);
            currentPageObject = targetPage;
            currentPageType = target;
        }

        foreach (var kvp in pageDataMap)
        {
            var data = kvp.Value;
            if (data.buttonImage != null)
            {
                data.buttonImage.sprite = (kvp.Key == target) ? selectedSprite : normalSprite;
            }
        }
    }

    public void _OnClickShowPage(int pageIndex)
    {
        if (System.Enum.IsDefined(typeof(BottomBar), pageIndex))
            ShowPage((BottomBar)pageIndex);
    }
}
