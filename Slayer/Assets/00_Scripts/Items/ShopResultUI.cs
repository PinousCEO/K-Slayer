using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopResultUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject uiRoot;          // UI Root
    [SerializeField] private Transform resultGrid;        // Grid Parent
    [SerializeField] private GameObject itemSlotPrefab;   // ItemSlot Prefab

    [Header("Sequence Timings")]
    [SerializeField] private float initialDelay = 0.2f;   // First Delay
    [SerializeField] private float itemInterval = 0.06f;  // Item Spawn Interval

    [Header("White Scale-In (알파 0→1, 큰 상태에서 축소)")]
    [SerializeField] private float whiteStartScale = 2f;  // White Animation Scale
    [SerializeField] private float whiteScaleDuration = 0.18f; // Sale Duration
    [SerializeField] private AnimationCurve scaleEase;     // Ease

    [Header("White→Item Crossfade")]
    [SerializeField] private float crossFadeDuration = 0.25f; // In Fade Duration
    [SerializeField] private float closeFadeDuration = 0.2f;   // Close Fade Duration
    [SerializeField] private AnimationCurve fadeEase;          // Fade Ease
    private float imageStartAlpha = 0f;       // Starting Alpha
    private float imageEndAlpha = 1f;         // Ending Alpha

    private readonly List<GameObject> spawned = new();  // All Slots
    private Coroutine seq;         // Cor Sequence
    private bool isShowing;        // is showing result
    private bool allShown;         // is result all Shown
    private bool canClose;         // close btn
    private bool skipRequested;    // skip request
    private bool clickInteraction = true;

    public event Action OnAllShown;  // all shown
    public event Action OnClosed;    // on close

    public void SetClickInteraction(bool enable) { clickInteraction = enable; }
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
        isShowing = true;
        allShown = false;
        canClose = false;
        skipRequested = false;

        if (uiRoot != null) uiRoot.SetActive(true);
        ClearChildren(resultGrid);
        spawned.Clear();

        yield return new WaitForSeconds(initialDelay);

        int total = items.Count;
        int spawnedCount = 0;
        int finishedCount = 0;

        Action onOneFinished = () => { finishedCount++; };

        while (spawnedCount < total)
        {
            if (skipRequested)
            {
                for (int i = spawnedCount; i < total; i++)
                {
                    var slotObjSkip = Instantiate(itemSlotPrefab, resultGrid);
                    SetupSlotWithItem(slotObjSkip, items[i]);
                    ForceFinish(slotObjSkip);
                    spawned.Add(slotObjSkip);
                }
                spawnedCount = total;
                finishedCount = total;
                break;
            }

            var slotObj = Instantiate(itemSlotPrefab, resultGrid);
            SetupSlotWithItem(slotObj, items[spawnedCount]);
            spawned.Add(slotObj);

            StartCoroutine(PlayItemAnimation(slotObj, onOneFinished));
            spawnedCount++;

            float t = 0f;
            while (t < itemInterval && !skipRequested)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        while (!skipRequested && finishedCount < total)
            yield return null;

        allShown = true;
        OnAllShown?.Invoke();

        while (!canClose) yield return null;

        yield return StartCoroutine(FadeOutAll());

        if (uiRoot != null) uiRoot.SetActive(false);
        isShowing = false;
        allShown = false;
        canClose = false;
        skipRequested = false;
        seq = null;

        OnClosed?.Invoke();
    }

    private void SetupSlotWithItem(GameObject slotObj, ShopItem_SObj item)
    {
        var entry = InventoryManager.Instance != null ? InventoryManager.Instance.GetEntry(item) : null;
        var slot = slotObj.GetComponent<InventorySlot>();
        if (slot != null && entry != null) slot.Setup(entry);

        var animImg = slotObj.transform.GetChild(0).GetComponent<Image>();
        if (animImg != null && item != null && item.icon != null)
        {
            animImg.sprite = item.icon;
            animImg.preserveAspect = true;
            animImg.enabled = true;
        }
    }

    private IEnumerator PlayItemAnimation(GameObject slotObj, Action onComplete)
    {
        var tr = slotObj.transform;
        var itemImg = tr.GetChild(0).GetComponent<Image>();
        var white = tr.GetChild(1).GetComponent<Image>();

        if (itemImg != null) itemImg.color = new Color(1f, 1f, 1f, imageStartAlpha);
        if (white != null)
        {
            white.transform.localScale = Vector3.one * whiteStartScale;
            white.color = new Color(1f, 1f, 1f, 0f);
        }

        if (white != null && whiteScaleDuration > 0f)
        {
            float t1 = 0f;
            var from = Vector3.one * whiteStartScale;
            var to = Vector3.one;
            while (t1 < whiteScaleDuration)
            {
                if (skipRequested) { ForceFinish(slotObj); onComplete?.Invoke(); yield break; }
                t1 += Time.deltaTime;
                float p = Mathf.Clamp01(t1 / whiteScaleDuration);
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
                if (skipRequested) { ForceFinish(slotObj); onComplete?.Invoke(); yield break; }
                t2 += Time.deltaTime;
                float p = Mathf.Clamp01(t2 / crossFadeDuration);
                float e = fadeEase != null ? fadeEase.Evaluate(p) : p;

                if (itemImg != null)
                    itemImg.color = new Color(1f, 1f, 1f, Mathf.LerpUnclamped(imageStartAlpha, imageEndAlpha, e));
                if (white != null)
                    white.color = new Color(1f, 1f, 1f, Mathf.LerpUnclamped(1f, 0f, e));
                yield return null;
            }
        }

        FinishToEndState(slotObj);
        onComplete?.Invoke();
    }

    private IEnumerator FadeOutAll()
    {
        if (closeFadeDuration <= 0f) yield break;
        float t = 0f;
        while (t < closeFadeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / closeFadeDuration);
            float a = Mathf.LerpUnclamped(1f, 0f, p);

            for (int i = 0; i < spawned.Count; i++)
            {
                var tr = spawned[i].transform;
                if (tr.childCount > 0)
                {
                    var img = tr.GetChild(0).GetComponent<Image>();
                    if (img != null) img.color = new Color(1f, 1f, 1f, a);
                }
                if (tr.childCount > 1)
                {
                    var w = tr.GetChild(1).GetComponent<Image>();
                    if (w != null) w.color = new Color(1f, 1f, 1f, 0f);
                }
            }
            yield return null;
        }
    }

    private void FinishToEndState(GameObject slotObj)
    {
        var tr = slotObj.transform;
        var itemImg = tr.GetChild(0).GetComponent<Image>();
        var white = tr.GetChild(1).GetComponent<Image>();

        if (itemImg != null) itemImg.color = new Color(1f, 1f, 1f, imageEndAlpha);
        if (white != null)
        {
            white.color = new Color(1f, 1f, 1f, 0f);
            white.transform.localScale = Vector3.one;
        }
    }

    private void ForceFinish(GameObject slotObj)
    {
        var tr = slotObj.transform;
        var itemImg = tr.GetChild(0).GetComponent<Image>();
        var white = tr.GetChild(1).GetComponent<Image>();

        if (itemImg != null) itemImg.color = new Color(1f, 1f, 1f, imageEndAlpha);
        if (white != null)
        {
            white.color = new Color(1f, 1f, 1f, 0f);
            white.transform.localScale = Vector3.one;
        }
    }

    private void Update()
    {
        if (!isShowing) return;
        if (!clickInteraction) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!allShown) skipRequested = true;
        }
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }
}
