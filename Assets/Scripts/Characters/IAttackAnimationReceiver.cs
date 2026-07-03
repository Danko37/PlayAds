namespace Characters
{
    /// <summary>
    /// Реализуется тем, кто наносит удар по анимации (HeroView, EnemyView).
    /// Метод дёргается animation event'ом в момент удара через AttackAnimationRelay.
    /// </summary>
    public interface IAttackAnimationReceiver
    {
        void OnAttackHit();
    }
}
