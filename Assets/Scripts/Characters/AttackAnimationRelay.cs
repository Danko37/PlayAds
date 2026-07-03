using UnityEngine;

namespace Characters
{
    /// <summary>
    /// Ставится на GameObject с Animator'ом (модель героя с мечом или модель врага).
    /// Animation event вызывает методы только у компонентов на том же объекте, что и Animator,
    /// а логика (HeroView/EnemyView) висит на родителе — этот релей пробрасывает вызов туда.
    /// </summary>
    public class AttackAnimationRelay : MonoBehaviour
    {
        private IAttackAnimationReceiver _receiver;

        private void Awake()
        {
            _receiver = GetComponentInParent<IAttackAnimationReceiver>();
        }

        /// <summary>
        /// Навешивается как animation event в момент удара (клинок в максимуме, ~середина клипа).
        /// </summary>
        public void OnAttackHit()
        {
            _receiver?.OnAttackHit();
        }
    }
}
