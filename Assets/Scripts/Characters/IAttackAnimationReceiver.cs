namespace Characters
{
    /// <summary>
    /// Реализуется тем, кто наносит удар по анимации (HeroView, EnemyView).
    /// Метод дёргается animation event'ом в момент удара через AttackAnimationRelay.
    /// </summary>
    public interface IAttackAnimationReceiver : IAnimationReceiver
    {
        void OnAttackHit();
    }
    
    /// <summary>
    /// Реализуется тем, кто бежит по анимации (HeroView, EnemyView).
    /// Метод дёргается animation event'ом в момент шага через AttackAnimationRelay.
    /// </summary>
    public interface IFootstepAnimationReceiver : IAnimationReceiver
    {
        void OnFootstep();
    }

    public interface IAnimationReceiver
    {
        
    }
}
