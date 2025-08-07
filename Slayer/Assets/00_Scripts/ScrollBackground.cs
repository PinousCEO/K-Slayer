using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollBackground : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float moveDuration = 1.5f;
    [SerializeField] private float bossFocusTime = 2.5f;

    private Coroutine cameraMoveCoroutine;

    [SerializeField] private Monster monsterPrefab;
    [SerializeField] private Monster BossMonsterPrefab;
    public Transform bg1;
    public Transform bg2;
    private float bgWidth = 32f;

    [SerializeField] private float spawnInterval = 5f;
    private float spawnTimer;

    private void Start()
    {
        GameManager.Instance.RegisterStateAction(Game_State.BOSS, SpawnBossMonster);
    }
    private void OnDestroy()
    {
        GameManager.Instance.UnregisterStateAction(Game_State.BOSS, SpawnBossMonster);
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
            bg1.Translate(Vector3.left * GameManager.Instance.speed * Time.deltaTime);
            bg2.Translate(Vector3.left * GameManager.Instance.speed * Time.deltaTime);

            if (bg1.position.x <= -bgWidth)
            {
                bg1.position = new Vector3(bg2.position.x + bgWidth, bg1.position.y, bg1.position.z);
                Swap(ref bg1, ref bg2);
            }
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
}
