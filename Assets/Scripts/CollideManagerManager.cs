using System;
using System.Collections;
using Characters;
using UnityEngine;

/// <summary>
/// Класс обрабатывает столкновения с игровыми сузностями и выполняет действия в зависимости от типа сущьности
/// </summary>
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
                if (data.target is Chest chest)
                {
                    OpenChest(chest, data.hero);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnDestroy()
    {
        eventsSo.OnCollideEvent -= OnCollideCollideWithEntity;
    }

    private void OpenChest(Chest chest, HeroView hero)
    {
        // Герой встаёт у сундука, ввод выключается (см. GameManager.HandleChestOpenStart).
        eventsSo.RaiseChestOpenStart();

        // Сундук проигрывает анимацию открытия и «выдаёт» меч. Когда меч долетает до героя —
        // экипируем меч, переносим очки сундука герою и возвращаем управление.
        chest.OpenChest(hero, () =>
        {
            // Очки сундука переходят герою (как от врагов): счётчик героя растёт, счётчик сундука обнуляется.
            var scoreAnimationTime = 0.4f;
            
            hero.AnimateScore(hero.Score, hero.Score + chest.Score, scoreAnimationTime);
            //chest.Score = 0;

            // Меняем модель и запускаем анимацию поднятия меча. Управление вернётся НЕ сейчас,
            // а в конце этой анимации — по animation event'у (HeroView.OnEquipFinished).
            hero.EquipSword();
        });
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
            
            //enemy.AnimateScore(enemy.Score, 0, scoreAnimationTime);
            // Герой поворачивается к врагу и бьёт. Смерть врага срабатывает
            // от animation event'а удара (HeroView.OnAttackHit -> enemy.Die()).
            eventsSo.RaiseBattleWin(enemy);
        }
        else
        {
            // Враг поворачивается к герою и бьёт. Смерть героя срабатывает
            // от animation event'а удара врага (EnemyView.OnAttackHit -> hero.OnKilled()).
            eventsSo.RaiseBattleLose(enemy);
        }
    }
}
