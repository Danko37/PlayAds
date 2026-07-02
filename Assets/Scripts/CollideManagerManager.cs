using System;
using Characters;
using UnityEngine;

public class CollideManagerManager : MonoBehaviour
{
    [SerializeField] private EventsSO eventsSo;

    private void Awake()
    {
        eventsSo.OnCollideEvent += OnCollideCollideWithEntity;
    }

    private void OnCollideCollideWithEntity(CollideData data)
    {
        switch (data.entityType)
        {
            case EntityType.Hero:
                StartBattle(data);
                break;
            case EntityType.Chest:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnDestroy()
    {
        eventsSo.OnCollideEvent -= OnCollideCollideWithEntity;
    }

    private void StartBattle(CollideData data)
    {
        
    }
}
