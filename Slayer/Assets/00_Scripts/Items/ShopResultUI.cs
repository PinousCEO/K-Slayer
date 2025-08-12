using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopResultUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private Transform resultGrid;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Sequence Timings")]
    [SerializeField] private float initialDelay = 0.2f;
    [SerializeField] private float itemInterval = 0.06f;

    [Header("White Scale-In")]
    [SerializeField] private float whiteStartScale = 2f;
    [SerializeField] private float whiteScaleDuration = 0.18f;
    [SerializeField] private AnimationCurve scaleEase;

    [Header("White→Item Crossfade")]
    [SerializeField] private float crossFadeDuration = 0.25f;
    [SerializeField] private float closeFadeDuration = 0.2f;
    [SerializeField] private AnimationCurve fadeEase;

    private const float imageStartAlpha = 0f;
    private const float imageEndAlpha = 1f;

    private readonly List<GameObject> spawned = new();
    private Coroutine seq;
    private bool isShowing;
    private bool allShown;
    private bool canClose;
    private bool skipRequested;
    private bool clickInteraction = true;
    private int finishedCount;

    public event Action OnAllShown;
    public event Action OnClosed;

    [Serializable]
    private sealed class SlotCache : MonoBehaviour
    {
        public Image itemImg;
        public Image whiteImg;
    }

    public void SetClickInteraction(bool enable) => clickInteraction = enable;
    public void ConfirmClose() { if (isShowing) canClose = true; }
    public void SkipAnimation() { if (isShowing && !allShown) skipRequested = true; }
    public bool IsShowingResult() => isShowing;

    public void ShowResult(List<ShopItem_SObj> items)
    {
        if (items == null || items.Count == 0) return;
        if (seq != null) StopCoroutine(seq);
        seq = StartCoroutine(ShowSequence(items));
    }

    private IEnumerator ShowSequence(List<ShopItem_SObj> items)
    {
        BeginState();

        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

        int total = items.Count;
        spawned.Capacity = Mathf.Max(spawned.Capacity, total);

        for (int i = 0; i < total; i++)
        {
            if (skipRequested)
            {
                for (int j = i; j < total; j++)
                {
                    var obj = Instantiate(itemSlotPrefab, resultGrid, false);
                    SetupSlotWithItem(obj, items[j]);
                    ForceFinish(obj);
                    spawned.Add(obj);
                }
                finishedCount = total;
                break;
            }

            var slotObj = Instantiate(itemSlotPrefab, resultGrid, false);
            SetupSlotWithItem(slotObj, items[i]);
            spawned.Add(slotObj);
            StartCoroutine(PlayItemAnimation(slotObj));

            float t = 0f;
            while (t < itemInterval && !skipRequested)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        while (!skipRequested && finishedCount < total) yield return null;

        allShown = true;
        OnAllShown?.Invoke();

        while (!canClose) yield return null;

        yield return FadeOutAll();

        EndState();
        OnClosed?.Invoke();
    }

    private void BeginState()
    {
        isShowing = true;
        allShown = false;
        canClose = false;
        skipRequested = false;
        finishedCount = 0;

        if (uiRoot != null && !uiRoot.activeSelf) uiRoot.SetActive(true);
        ClearChildren(resultGrid);
        spawned.Clear();
    }

    private void EndState()
    {
        if (uiRoot != null) uiRoot.SetActive(false);
        isShowing = false;
        allShown = false;
        canClose = false;
        skipRequested = false;
        finishedCount = 0;
        seq = null;
        spawned.Clear();
    }

    private void SetupSlotWithItem(GameObject slotObj, ShopItem_SObj item)
    {
        var entry = InventoryManager.Instance != null ? InventoryManager.Instance.GetEntry(item) : null;
        var slot = slotObj.GetComponent<InventorySlot>();
        if (slot != null && entry != null) slot.Setup(entry);

        var cache = slotObj.GetComponent<SlotCache>();
        if (cache == null)
        {
            cache = slotObj.AddComponent<SlotCache>();
            cache.itemImg = slotObj.transform.GetChild(0).GetComponent<Image>();
            cache.whiteImg = slotObj.transform.childCount > 1 ? slotObj.transform.GetChild(1).GetComponent<Image>() : null;
        }

        if (cache.itemImg != null)
        {
            if (item != null && item.icon != null)
            {
                cache.itemImg.sprite = item.icon;
                cache.itemImg.preserveAspect = true;
                cache.itemImg.enabled = true;
            }
            cache.itemImg.color = new Color(1f, 1f, 1f, imageStartAlpha);
        }

        if (cache.whiteImg != null)
        {
            cache.whiteImg.transform.localScale = Vector3.one * whiteStartScale;
            cache.whiteImg.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private IEnumerator PlayItemAnimation(GameObject slotObj)
    {
        var cache = slotObj.GetComponent<SlotCache>();
        var itemImg = cache != null ? cache.itemImg : null;
        var white = cache != null ? cache.whiteImg : null;

        if (white != null && whiteScaleDuration > 0f)
        {
            float t1 = 0f;
            var from = Vector3.one * whiteStartScale;
            var to = Vector3.one;
            while (t1 < whiteScaleDuration)
            {
                if (skipRequested) { ForceFinish(slotObj); finishedCount++; yield break; }
                t1 += Time.deltaTime;
                float p = t1 / whiteScaleDuration;
                float e = scaleEase != null ? scaleEase.Evaluate(p) : p;

                white.transform.localScale = Vector3.LerpUnclamped(from, to, e);
                white.color = new Color(1f, 1f, 1f, Mathf.LerpUnclamped(0f, 1f, e));
                yield return null;
            }
            white.transform.localScale = Vector3.one;
            white.color = new Color(1f, 1f, 1f, 1f);
        }

        if (crossFadeDuration > 0f)
        {
            float t2 = 0f;
            while (t2 < crossFadeDuration)
            {
                if (skipRequested) { ForceFinish(slotObj); finishedCount++; yield break; }
                t2 += Time.deltaTime;
                float p = t2 / crossFadeDuration;
                float e = fadeEase != null ? fadeEase.Evaluate(p) : p;

                if (itemImg != null)
                    itemImg.color = new Color(1f, 1f, 1f, Mathf.LerpUnclamped(imageStartAlpha, imageEndAlpha, e));
                if (white != null)
                    white.color = new Color(1f, 1f, 1f, Mathf.LerpUnclamped(1f, 0f, e));

                yield return null;
            }
        }

        FinishToEndState(slotObj);
        finishedCount++;
    }

    private IEnumerator FadeOutAll()
    {
        if (closeFadeDuration <= 0f) yield break;

        float t = 0f;
        while (t < closeFadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.LerpUnclamped(1f, 0f, t / closeFadeDuration);

            for (int i = 0; i < spawned.Count; i++)
            {
                var cache = spawned[i].GetComponent<SlotCache>();
                if (cache == null) continue;
                if (cache.itemImg != null)
                    cache.itemImg.color = new Color(1f, 1f, 1f, a);
                if (cache.whiteImg != null)
                    cache.whiteImg.color = new Color(1f, 1f, 1f, 0f);
            }
            yield return null;
        }
    }

    private void FinishToEndState(GameObject slotObj)
    {
        var cache = slotObj.GetComponent<SlotCache>();
        if (cache == null) return;

        if (cache.itemImg != null)
            cache.itemImg.color = new Color(1f, 1f, 1f, imageEndAlpha);

        if (cache.whiteImg != null)
        {
            cache.whiteImg.color = new Color(1f, 1f, 1f, 0f);
            cache.whiteImg.transform.localScale = Vector3.one;
        }
    }

    private void ForceFinish(GameObject slotObj)
    {
        var cache = slotObj.GetComponent<SlotCache>();
        if (cache == null) return;

        if (cache.itemImg != null)
            cache.itemImg.color = new Color(1f, 1f, 1f, imageEndAlpha);

        if (cache.whiteImg != null)
        {
            cache.whiteImg.color = new Color(1f, 1f, 1f, 0f);
            cache.whiteImg.transform.localScale = Vector3.one;
        }
    }

    private void Update()
    {
        if (!isShowing || !clickInteraction) return;
        if (Input.GetMouseButtonDown(0) && !allShown) skipRequested = true;
    }

    private static void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }
}
