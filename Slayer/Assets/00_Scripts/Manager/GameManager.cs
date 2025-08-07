using System;
using System.Collections.Generic;
using UnityEngine;

public enum Game_State
{
    IDLE,
    MOVE,
    ATTACK,
    ATTACKANDMOVE,
    SKILL,
    BOSS,
    GAMECLEAR,
    NEXT
}
[System.Serializable]
public class RoundInfo
{
    public int Stage;  
    public int Wave;   
    public int MaxWave;
}
public class GameManager : Singleton<GameManager>
{
    [Range(0.0f, 5.0f)] public float speed;
    public RoundInfo CurrentRound = new RoundInfo { Stage = 1, Wave = 0, MaxWave = 5 };
    public Game_State game_State;
    public Monster TargetMonster => ActiveMonsters.Count > 0 ? ActiveMonsters[0] : null;
    public Action<int> RoundUpAction = null;
    public bool isBoss;
    private Dictionary<Game_State, Action> Game_stateActions;
    private List<Monster> ActiveMonsters { get; set; } = new List<Monster>();
    protected override void Awake()
    {
        base.Awake();
        StatManager.m_PlayerStatData.gold = 1000000;
        Debug.Log(StatManager.m_PlayerStatData.gold);
        Game_stateActions = new Dictionary<Game_State, Action>
        {
            { Game_State.IDLE, OnIdle },
            { Game_State.MOVE, OnMove },
            { Game_State.ATTACK, OnAttack },
            { Game_State.SKILL, OnSkill },
            {Game_State.BOSS, OnBoss },
            {Game_State.GAMECLEAR,  OnGameclear}
        };
    }

    public List<Monster> activeMonsters()
    {
        return ActiveMonsters;
    }
   
    private void Start()
    {
        Game_StateChange(Game_State.MOVE);
    }

    public void GetAddRound()
    {
        CurrentRound.Wave++;
        RoundUpAction?.Invoke(CurrentRound.Wave);
    }
    public void Game_StateChange(Game_State state)
    {
        game_State = state;
        if(Game_stateActions.TryGetValue(state, out var action))
        {
            action?.Invoke();
        }
    }
    public int MonstersCount()
    {
        return ActiveMonsters.Count;
    }
    public void RegisterStateAction(Game_State state, Action action)
    {
        if (Game_stateActions.ContainsKey(state))
            Game_stateActions[state] += action; 
        else
            Game_stateActions[state] = action; 
    }
    public void UnregisterStateAction(Game_State state, Action action)
    {
        if (Game_stateActions.ContainsKey(state))
        {
            Game_stateActions[state] -= action;

            if (Game_stateActions[state] == null)
                Game_stateActions.Remove(state);
        }
    }

    public void UnregisterMonster(Monster monster)
    {
        ActiveMonsters.Remove(monster);
    }

    public void SortMonstersByX()
    {
        ActiveMonsters.Sort((a, b) =>
            a.transform.position.x.CompareTo(b.transform.position.x));

        for (int i = 0; i < ActiveMonsters.Count; i++)
        {
            var monster = ActiveMonsters[i];
            var skeleton = monster.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (skeleton != null)
            {
                skeleton.GetComponent<Renderer>().sortingOrder = i;
            }
        }
    }

    public void RegisterMonster(Monster monster)
    {
        if (!ActiveMonsters.Contains(monster))
            ActiveMonsters.Add(monster);
    }

    private void OnIdle() => Debug.Log("Idle state");
    private void OnMove()
    {
        GetAddRound();
    }
    private void OnAttack() => Debug.Log("Attack state");
    private void OnSkill() => Debug.Log("Skill state");
    private void OnBoss()
    {
        isBoss = true;
    }

    private void OnGameclear()
    {
        CurrentRound.Stage++;
        CurrentRound.Wave = 0;
        isBoss = false;
    }
}
