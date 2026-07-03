using System;
using System.Collections;
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
        StartCoroutine(BattleRoutine(enemy, hero));
    }

    private IEnumerator BattleRoutine(EnemyView enemy, HeroView hero)
    {
        // Ставим героя в паузу и даём бою "прочитаться".
        eventsSo.RaiseBattleStart();
        yield return new WaitForSeconds(0.2f);

        // Без меча герой проигрывает в любом случае.
        // С мечом герой побеждает только при строгом превосходстве. Ничья = оба проиграли.
        bool heroWon = hero.HasSword && hero.Score > enemy.Score;

        if (heroWon)
        {
            var scoreAnimationTime = 0.4f;
            
            int from = hero.Score;
            hero.Score += enemy.Score;
            
            hero.AnimateScore(from, hero.Score, scoreAnimationTime);

            enemy.Die();

            // Даём счётчику отыграть, затем возобновляем движение.
            yield return new WaitForSeconds(scoreAnimationTime);
            eventsSo.RaiseBattleWin();
        }
        else
        {
            eventsSo.RaiseBattleLose();
        }
    }
}
