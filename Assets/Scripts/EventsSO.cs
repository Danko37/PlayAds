using System;
using Characters;
using UnityEngine;

public struct CollideData
{
    public HeroView hero;
    public EntityBase target;
}

[CreateAssetMenu(menuName = "Events/EventsSO")]
public class EventsSO : ScriptableObject
{
    public event Action<CollideData> OnCollideEvent;

    public event Action OnBattleStart;
    // Оркестрация боя: кто кого атакует.
    public event Action<EnemyView> OnBattleWin;
    public event Action<EnemyView> OnBattleLose;

    // Итог боя для игровой логики / UI (подцепляется снаружи).
    public event Action OnHeroWin;
    public event Action OnHeroLose;

    public void OnCollideEventRaise(CollideData data)
    {
        OnCollideEvent?.Invoke(data);
    }

    public void RaiseBattleStart() => OnBattleStart?.Invoke();
    public void RaiseBattleWin(EnemyView enemy) => OnBattleWin?.Invoke(enemy);
    public void RaiseBattleLose(EnemyView enemy) => OnBattleLose?.Invoke(enemy);

    public void RaiseHeroWin() => OnHeroWin?.Invoke();
    public void RaiseHeroLose() => OnHeroLose?.Invoke();
}