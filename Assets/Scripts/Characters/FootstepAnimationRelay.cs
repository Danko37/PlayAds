namespace Characters
{
    public class FootstepAnimationRelay : AnimationRelay
    {
        /// <summary>
        /// Навешивается как animation event в момент удара (клинок в максимуме, ~середина клипа).
        /// </summary>
        public void OnFootstep()
        {
            var animReceiver = _receiver as IFootstepAnimationReceiver;
            animReceiver?.OnFootstep();
        }
        
        private void Awake()
        {
            _receiver = GetComponentInParent<IFootstepAnimationReceiver>();
        }
    }
}