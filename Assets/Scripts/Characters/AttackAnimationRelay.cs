namespace Characters
{
    //Принимает события атаки из аниматора того, кто атакеует 
    public class AttackAnimationRelay : AnimationRelay
    {
        /// <summary>
        /// Навешивается как animation event в момент удара (клинок в максимуме, ~середина клипа).
        /// </summary>
        public void OnAttackHit()
        {
            var animReceiver = _receiver as IAttackAnimationReceiver;
            animReceiver?.OnAttackHit();
        }
        
        private void Awake()
        {
            _receiver = GetComponentInParent<IAttackAnimationReceiver>();
        }
    }
}
