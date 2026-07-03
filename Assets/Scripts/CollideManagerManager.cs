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
        switch (data.target.entityType)
        {
            case EntityType.Enemy:
                if (data.target is EnemyView enemy)
                {
                    StartBattle(enemy, data.hero); 
                }
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

    private void StartBattle(EnemyView enemy, HeroView  hero)
    {
        
    }
}
