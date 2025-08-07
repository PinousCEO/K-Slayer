using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField] private float moveUpSpeed = 0.5f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Vector3 initialScale = new Vector3(1.5f, 1.5f, 1f);
    [SerializeField] private Vector3 finalScale = new Vector3(1f, 1f, 1f);

    private float timer;
    private Color startColor;

    public static void Create(Vector3 position, double damage, Color color)
    {
        var prefab = Resources.Load<DamageText>("UI/DamageText");
        var instance = Instantiate(prefab, position + new Vector3(0, 0.5f, 0), Quaternion.identity);
        instance.text.text = damage.ToString();
        instance.text.color = color;
    }

    private void Start()
    {
        startColor = text.color;
        transform.localScale = initialScale;
        Destroy(gameObject, fadeDuration);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // 위치 이동
        transform.position += Vector3.up * moveUpSpeed * Time.deltaTime;

        // 크기 축소
        transform.localScale = Vector3.Lerp(initialScale, finalScale, timer / fadeDuration);

        // 페이드 아웃
        float fadeT = Mathf.Clamp01(timer / fadeDuration);
        text.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0), fadeT);
    }
}
