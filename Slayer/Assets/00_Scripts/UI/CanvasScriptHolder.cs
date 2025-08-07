using UnityEngine;

public class CanvasScriptHolder : MonoBehaviour
{
    public static Main_UI main;
    public static Boss_UI boss;

    private void Awake()
    {
        main = GetComponentInChildren<Main_UI>(includeInactive: true);
        boss = GetComponentInChildren<Boss_UI>(includeInactive: true);
    }

    private void Start()
    {
        GameManager.Instance.RegisterStateAction(Game_State.BOSS, OnBoss);
    }

    private void OnDestroy()
    {
        GameManager.Instance.UnregisterStateAction(Game_State.BOSS, OnBoss);
    }

    private void OnBoss()
    {
        boss.Initialize();
    }
}
