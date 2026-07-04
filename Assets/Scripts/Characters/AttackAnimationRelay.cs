using UnityEngine;

namespace Characters
{
    //Принимает события атаки из аниматора того, кто атакеует 
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
