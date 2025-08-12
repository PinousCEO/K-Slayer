using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ParallaxLayer
{
    [Header("Pair")]
    public Transform front;   
    public Transform back;   

    [Header("Motion")]
    [Tooltip("레이어 개별 속도(전역 속도에 곱)")]
    public float speedMultiplier = 1f;

    [Header("Size")]
    [Tooltip("배경 한 장의 가로폭(월드단위). 0이면 SpriteRenderer로 자동 계산")]
    public float width = 0f;

    [Header("Optional: SortingOrder 교차")]
    [Tooltip("재배치 때 앞/뒤 스프라이트의 sortingOrder를 서로 바꿀지 여부(필요 없으면 끄기)")]
    public bool swapSortingOnLoop = false;

    // 캐시
    private SpriteRenderer srFront, srBack;
    private float cachedWidth;
    private bool initialized;

    public void Init()
    {
        if (front == null || back == null) return;

        srFront = front.GetComponent<SpriteRenderer>();
        srBack = back.GetComponent<SpriteRenderer>();

        cachedWidth = width > 0f ? width : CalcWidth(front, srFront);
        initialized = true;

        float expectedBackX = front.position.x + cachedWidth;
        if (Mathf.Abs(back.position.x - expectedBackX) > 0.01f)
        {
            back.position = new Vector3(expectedBackX, back.position.y, back.position.z);
        }
    }

    public void Tick(float globalSpeed, float dt, float loopFactor)
    {
        if (!initialized) return;

        float moveX = -globalSpeed * speedMultiplier * dt;

        front.Translate(moveX, 0f, 0f);
        back.Translate(moveX, 0f, 0f);

        float loopWidth = cachedWidth * loopFactor;

        if (front.position.x <= -loopWidth)
        {
            MovePieceToRight(ref front, back, loopWidth);
            if (swapSortingOnLoop) SwapSorting(srFront, srBack);
            SwapRef(ref front, ref back);
        }
        else if (back.position.x <= -loopWidth)
        {
            MovePieceToRight(ref back, front, loopWidth);
            if (swapSortingOnLoop) SwapSorting(srBack, srFront);
            SwapRef(ref front, ref back);
        }
    }

    private float CalcWidth(Transform t, SpriteRenderer sr)
    {
        if (sr != null && sr.sprite != null)
        {
            return sr.bounds.size.x;
        }
        return Mathf.Abs((back != null ? back.position.x : t.position.x) - t.position.x);
    }

    private void MovePieceToRight(ref Transform who, Transform other, float loopWidth)
    {
        float newX = other.position.x + loopWidth;
        who.position = new Vector3(newX, who.position.y, who.position.z);
    }

    private void SwapRef(ref Transform a, ref Transform b)
    {
        var tmp = a; a = b; b = tmp;
        var srt = srFront; srFront = srBack; srBack = srt;
    }

    private void SwapSorting(SpriteRenderer a, SpriteRenderer b)
    {
        if (a == null || b == null) return;
        int t = a.sortingOrder;
        a.sortingOrder = b.sortingOrder;
        b.sortingOrder = t;
    }
}
public class ScrollBackground : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float moveDuration = 1.5f;
    [SerializeField] private float bossFocusTime = 2.5f;

    private Coroutine cameraMoveCoroutine;

    [SerializeField] private Monster monsterPrefab;
    [SerializeField] private Monster BossMonsterPrefab;

    [Header("Parallax Layers (5개 구성)")]
    [SerializeField] private List<ParallaxLayer> layers = new List<ParallaxLayer>();

    [Tooltip("왼쪽으로 얼마나 이동하면 뒤로 보내는지(레이어 폭 기준). 1이면 정확히 폭만큼, 1.0~1.2 사이 살짝 여유를 두면 경계선 안 보임")]
    [Range(0.8f, 1.2f)]
    [SerializeField] private float loopWidthFactor = 1f;

    [SerializeField] private float spawnInterval = 5f;
    private float spawnTimer;
    void Awake()
    {
        foreach (var l in layers)
            l.Init();
    }
    private void Start()
    {
        GameManager.Instance.RegisterStateAction(Game_State.BOSS, SpawnBossMonster);
        GameManager.Instance.RegisterStateAction(Game_State.DungeonBoss, SpawnBossDungeon);
    }
    private void OnDestroy()
    {
        GameManager.Instance.UnregisterStateAction(Game_State.BOSS, SpawnBossMonster);
        GameManager.Instance.UnregisterStateAction(Game_State.DungeonBoss, SpawnBossDungeon);
    }

    public void CameraTransformChange(Transform bossTransform)
    {
        if (cameraMoveCoroutine != null)
            StopCoroutine(cameraMoveCoroutine);

        cameraMoveCoroutine = StartCoroutine(CameraFocusSequence(bossTransform));
    }

    private IEnumerator CameraFocusSequence(Transform bossTarget)
    {
        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(MoveCameraTo(bossTarget.position));

        yield return new WaitForSeconds(bossFocusTime);

        yield return StartCoroutine(MoveCameraTo(Vector3.zero));

        GameManager.Instance.Game_StateChange(Game_State.MOVE);
        CanvasScriptHolder.boss.OutInitalize();
    }

    private IEnumerator MoveCameraTo(Vector3 targetPosition)
    {
        Transform cam = Camera.main.transform;
        Vector3 start = cam.position;
        Vector3 end = new Vector3(targetPosition.x, cam.position.y, cam.position.z); // X만 따라가게

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            cam.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        cam.position = end; 
    }

    void Update()
    {
        if (GameManager.Instance.game_State == Game_State.ATTACKANDMOVE || GameManager.Instance.game_State == Game_State.MOVE)
        {
            float dt = Time.deltaTime;
            foreach (var l in layers)
                l.Tick(GameManager.Instance.speed, dt, loopWidthFactor);
        }

        if (GameManager.Instance.game_State != Game_State.MOVE) return;
        if (GameManager.Instance.isBoss) return;
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0;
            SpawnMonsters();
        }
    }

    void Swap(ref Transform a, ref Transform b)
    {
        Transform temp = a;
        a = b;
        b = temp;
    }

    void SpawnMonsters()
    {
        float y = monsterPrefab.transform.position.y;
        float z = monsterPrefab.transform.position.z;

        float monsterWidth = monsterPrefab.transform.localScale.x; 
        List<float> usedX = new List<float>();

        int spawnCount = Random.Range(3, 5); 

        for (int i = 0; i < spawnCount; i++)
        {
            float randomX;
            int attempts = 0;

            do
            {
                randomX = Random.Range(5f, 8f); 
                attempts++;
            } while (usedX.Exists(x => Mathf.Abs(x - randomX) < monsterWidth) && attempts < 20);

            usedX.Add(randomX);
            var monster = Instantiate(monsterPrefab, new Vector3(randomX, y, z), Quaternion.identity);
            monster.Initialize();
            GameManager.Instance.RegisterMonster(monster);
        }

        GameManager.Instance.SortMonstersByX();
    }

    void SpawnBossMonster()
    {
        float y = monsterPrefab.transform.position.y;
        float z = monsterPrefab.transform.position.z;

        float randomX = Random.Range(5f, 8f);
        var monster = Instantiate(BossMonsterPrefab, new Vector3(randomX, y, z), Quaternion.identity);
        monster.Initialize();

        GameManager.Instance.RegisterMonster(monster);

        GameManager.Instance.SortMonstersByX();

        CameraTransformChange(monster.transform);
    }

    void SpawnBossDungeon()
    {
        StartCoroutine(DelayMonsterGet());
    }

    IEnumerator DelayMonsterGet()
    {
        yield return new WaitForSeconds(2.0f);
        var monster = Instantiate(BossMonsterPrefab, new Vector3(1.45f, 0.2f, 0.0f), Quaternion.identity);
        monster.Initialize();

        GameManager.Instance.RegisterMonster(monster);
        GameManager.Instance.SortMonstersByX();
    }
}
