using System;
using Characters;
using UnityEngine;

public struct CollideData
{
    public EntityType  entityType;
    public int heroScore;
    public int enemyScore;
}

[CreateAssetMenu(menuName = "Events/EventsSO")]
public class EventsSO : ScriptableObject
{
    public event Action<CollideData> OnCollideEvent;

    public void OnCollideEventRaise(CollideData data)
    {
        OnCollideEvent?.Invoke(data);
    }
}