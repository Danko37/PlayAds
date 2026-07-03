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
    public event Action OnBattleWin;
    public event Action OnBattleLose;

    public void OnCollideEventRaise(CollideData data)
    {
        OnCollideEvent?.Invoke(data);
    }

    public void RaiseBattleStart() => OnBattleStart?.Invoke();
    public void RaiseBattleWin() => OnBattleWin?.Invoke();
    public void RaiseBattleLose() => OnBattleLose?.Invoke();
}