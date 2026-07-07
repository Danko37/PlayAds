namespace Characters
{
    // Принимает событие «меч поднят» из аниматора модели с мечом.
    public class EquipAnimationRelay : AnimationRelay
    {
        /// <summary>
        /// Навешивается как animation event в КОНЦЕ клипа поднятия меча.
        /// </summary>
        public void OnEquipFinished()
        {
            var animReceiver = _receiver as IEquipAnimationReceiver;
            animReceiver?.OnEquipFinished();
        }

        private void Awake()
        {
            _receiver = GetComponentInParent<IEquipAnimationReceiver>();
        }
    }
}
