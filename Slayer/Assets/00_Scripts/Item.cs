using UnityEngine;

public class Item : MonoBehaviour
{
    private Vector2 startPos;
    private Vector2 targetPos;
    private float duration;
    private float elapsed;
    private float height;
    private bool isFlying = true;

    [SerializeField] private float pickupRange = 0.5f;

    private Transform player;

    private ItemType itemType;
    private double count;

    bool isCollect = false;

    public void Init(Vector2 from, ItemType itemType, double count)
    {
        Vector2 offset = new Vector2(
            Random.Range(-0.5f, 0.5f),
            Random.Range(-0.5f, 0.5f) // y°ªµµ ·£´ý
        );

        Vector2 target = (Vector2)transform.position + offset;

        float height = Random.Range(0.5f, 1.0f);
        float duration = Random.Range(0.7f, 1.2f);

        this.itemType = itemType;
        this.count = count;

        startPos = from;
        targetPos = target;
        this.height = height;
        this.duration = duration;
        elapsed = 0f;
        isFlying = true;

        player = SkillManager.Instance.playerTransform.transform;
    }

    void Update()
    {
        if (isCollect) return;
        if (isFlying)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector2 linear = Vector2.Lerp(startPos, targetPos, t);
            float arc = height * 4 * (t - t * t);

            transform.position = new Vector3(linear.x, linear.y + arc, 0f);

            if (t >= 1f)
            {
                isFlying = false;
            }
        }
        else
        {
            if (GameManager.Instance.game_State == Game_State.MOVE || GameManager.Instance.game_State == Game_State.ATTACKANDMOVE)
                transform.Translate(Vector3.left * GameManager.Instance.speed * Time.deltaTime);

            if (player != null && Mathf.Abs(player.position.x - transform.position.x) < pickupRange)
            {
                isCollect = true;
                Collect();
            }
        }
    }

    private void Collect()
    {
        ItemNoti.OnGetItem?.Invoke(itemType, count);

        if (itemType == ItemType.Coin)
            StatManager.SetCoin(count);

        Destroy(gameObject);
    }
}
