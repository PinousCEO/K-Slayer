using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemNoti : MonoBehaviour
{
    public static Action<ItemType, double> OnGetItem;

    [SerializeField] private Transform content;
    [SerializeField] private ItemNotiPart part;

    public List<ItemNotiPart> partList = new List<ItemNotiPart>();

    private void Start()
    {
        OnGetItem += InitPart;
    }

    private void OnDestroy()
    {
        OnGetItem -= InitPart;
    }

    public void InitPart(ItemType item, double count)
    {
        var go = Instantiate(part, content);

        var notiPart = go.GetComponent<ItemNotiPart>();
        notiPart.Init(this, item, count);

        partList.Insert(0, notiPart);

        if (partList.Count > 5)
        {
            DestroyNextPart();
        }

        UpdateScales();
    }

    public void DestroyNextPart()
    {
        var last = partList[partList.Count - 1];
        Destroy(last.gameObject);
        partList.RemoveAt(partList.Count - 1);
    }

    private void UpdateScales()
    {
        for (int i = 0; i < partList.Count; i++)
        {
            float scaleX = 1.05f - (i * 0.05f);
            scaleX = Mathf.Clamp(scaleX, 0.85f, 1.05f);

            var tr = partList[i].transform;
            Vector3 scale = tr.localScale;
            scale.x = scaleX;
            tr.localScale = scale;
        }
    }
}
