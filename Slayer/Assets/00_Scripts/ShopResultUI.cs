using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopResultUI : MonoBehaviour
{
    [SerializeField] private GameObject blackScreen;
    [SerializeField] private Transform resultGrid;
    [SerializeField] private GameObject itemSlotPrefab;

    private bool isShowing = false;
    private bool skipRequested = false;
    private bool allShown = false;
    private bool canClose = false;

    void Start()
    {
        blackScreen.SetActive(false);
        resultGrid.gameObject.SetActive(false);
    }

    public void ShowResult(List<ShopItem_SObj> items)
    {
        if (isShowing) return;
        StartCoroutine(ShowSequence(items));
    }

    private IEnumerator ShowSequence(List<ShopItem_SObj> items)
    {
        isShowing = true;
        skipRequested = false;
        allShown = false;
        canClose = false;

        blackScreen.SetActive(true);
        resultGrid.gameObject.SetActive(true);

        foreach (Transform child in resultGrid)
            Destroy(child.gameObject);

        List<GameObject> slots = new();

        foreach (var item in items)
        {
            GameObject slot = Instantiate(itemSlotPrefab, resultGrid);
            Transform slotT = slot.transform;

            var cg = slotT.GetComponent<CanvasGroup>();
            var itemImg = slotT.GetChild(0).GetComponent<Image>();
            var white = slotT.GetChild(1).GetComponent<Image>();

            if (cg != null) cg.alpha = 1f;
            if (itemImg != null)
            {
                itemImg.sprite = item.icon;
                itemImg.color = new Color(1, 1, 1, 0);
            }

            if (white != null)
            {
                white.color = new Color(1, 1, 1, 0);
                white.transform.localScale = Vector3.one * 2f;
            }

            slotT.localScale = Vector3.one;
            slots.Add(slot);
        }

        yield return new WaitForSeconds(0.2f);

        if (!skipRequested)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                yield return StartCoroutine(PlayItemAnimation(slots[i]));
                yield return new WaitForSeconds(0.1f);
                if (skipRequested) break;
            }
        }

        if (skipRequested)
        {
            bool[] finished = new bool[slots.Count];

            for (int i = 0; i < slots.Count; i++)
            {
                int idx = i;
                StartCoroutine(PlayItemAnimation(slots[idx], () => finished[idx] = true));
            }

            while (!AllTrue(finished))
                yield return null;
        }

        allShown = true;

        while (!canClose)
            yield return null;

        blackScreen.SetActive(false);
        resultGrid.gameObject.SetActive(false);
        isShowing = false;
    }

    private IEnumerator PlayItemAnimation(GameObject slotObj, System.Action onComplete = null)
    {
        var slot = slotObj.transform;
        var itemImg = slot.GetChild(0).GetComponent<Image>();
        var white = slot.GetChild(1).GetComponent<Image>();

        float duration = 0.25f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);

            if (white != null)
            {
                white.color = new Color(1, 1, 1, 1 - progress);
                white.transform.localScale = Vector3.Lerp(Vector3.one * 2f, Vector3.one, progress);
            }

            if (itemImg != null)
                itemImg.color = new Color(1, 1, 1, progress);

            yield return null;
        }

        if (white != null)
        {
            white.color = new Color(1, 1, 1, 0);
            white.transform.localScale = Vector3.one;
        }

        if (itemImg != null)
            itemImg.color = Color.white;

        onComplete?.Invoke();
    }

    private bool AllTrue(bool[] flags)
    {
        foreach (bool b in flags)
            if (!b) return false;
        return true;
    }

    void Update()
    {
        if (!isShowing) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!allShown)
            {
                skipRequested = true;
            }
            else
            {
                canClose = true;
            }
        }
    }

    public bool IsShowingResult() => isShowing;
}
