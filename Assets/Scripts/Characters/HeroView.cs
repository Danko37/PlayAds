using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Characters
{
    public class HeroView : EntityBase
    {
        private static readonly int IsRun = Animator.StringToHash("isRun");
        private static readonly int Die1 = Animator.StringToHash("die");

        [SerializeField]
        private EventsSO events;
        [SerializeField]
        private Transform _heroVisualTransform;

        [Header("Model Swap (безоружный -> с мечом)")]
        [SerializeField] private GameObject swordlessModel;   // DF_Knight_2_idle (активна на старте)
        [SerializeField] private GameObject swordModel;       // DF_Knight_2_attack_02 (выключена на старте)
        [SerializeField] private Animator swordAnimator;      // аниматор модели с мечом
        [SerializeField, Range(0.1f, 1f)] private float shrinkFactor = 0.8f;
        [SerializeField] private float shrinkDuration = 0.15f;
        [SerializeField] private float growDuration = 0.25f;
        [Tooltip("Хук для эффекта из спрайт-рендерера во время смены модели.")]
        [SerializeField] private UnityEvent onSwordEquipped;

        private bool _hasSword;
        private bool _isRunning;

        public EventsSO Events => events;

        public bool HasSword => _hasSword;

        public Transform HeroVisualTransform => _heroVisualTransform;

        [SerializeField]
        private NavMeshAgent _navMeshAgent;

        public float InitialYRotation { get; private set; }

        public NavMeshAgent NavMeshAgent => _navMeshAgent;
        void Start()
        {
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.autoBraking = false;
            _navMeshAgent.speed = 8f;
            _navMeshAgent.acceleration = 1000f;
            _navMeshAgent.angularSpeed = 1000f;
            _navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        
            InitialYRotation = _heroVisualTransform.localRotation.eulerAngles.y;
        }
        
        public void SetRun(bool run)
        {
            _isRunning = run;
            animator.SetBool(IsRun, run);
        }

        public void Die()
        {
            animator.SetTrigger(Die1);
        }

        /// <summary>
        /// Меняет модель героя на "с мечом": текущая уменьшается и скрывается,
        /// модель с мечом активируется и твинингом вырастает до нормы. После смены
        /// код управляет уже новым аниматором (swordAnimator).
        /// </summary>
        [ContextMenu("Equip Sword (test)")]
        public void EquipSword()
        {
            if (_hasSword || swordModel == null || swordlessModel == null || swordAnimator == null)
                return;

            _hasSword = true;

            // Эффект из спрайт-рендерера и прочее — вешается в инспекторе.
            onSwordEquipped?.Invoke();

            var swordlessScale = swordlessModel.transform.localScale;
            var swordTargetScale = swordModel.transform.localScale;

            // 1) текущая модель уменьшается, затем скрывается.
            swordlessModel.transform
                .DOScale(swordlessScale * shrinkFactor, shrinkDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    swordlessModel.SetActive(false);
                    swordlessModel.transform.localScale = swordlessScale;

                    // 2) модель с мечом активируется в уменьшённом виде и растёт до нормы.
                    swordModel.transform.localScale = swordTargetScale * shrinkFactor;
                    swordModel.SetActive(true);

                    // 3) дальше код управляет новым аниматором.
                    animator = swordAnimator;
                    if (HasParameter(animator, IsRun))
                        animator.SetBool(IsRun, _isRunning);

                    swordModel.transform
                        .DOScale(swordTargetScale, growDuration)
                        .SetEase(Ease.OutBack);
                });
        }

        private static bool HasParameter(Animator animator, int paramHash)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            foreach (var p in animator.parameters)
            {
                if (p.nameHash == paramHash)
                    return true;
            }

            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var collideEntity = other.GetComponent<EntityBase>();
            if (collideEntity ==  null)  return;

            var data = new CollideData
                { hero = this,  target = collideEntity};
            if (collideEntity.entityType == EntityType.Enemy)
            {
                events.OnCollideEventRaise(data);
            }
        }
    }
}
