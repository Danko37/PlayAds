using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Characters
{
    public class HeroView : EntityBase, IAttackAnimationReceiver
    {
        private static readonly int IsRun = Animator.StringToHash("isRun");
        private static readonly int Die1 = Animator.StringToHash("die");
        private static readonly int Attack1 = Animator.StringToHash("attack");

        [Tooltip("Задержка удара, если у модели нет анимации атаки (animation event недоступен).")]
        [SerializeField] private float _fallbackHitDelay = 0.5f;

        private EnemyView _attackTarget;

        [SerializeField]
        private EventsSO events;
        [SerializeField]
        private Transform _heroVisualTransform;
        [SerializeField]
        private Transform _heroVisualTransformWithSword;

      
        // DF_Knight_2_idle (активна на старте)
        [SerializeField] private GameObject heroWithoutSwordModel;   
        // DF_Knight_2_attack_02 (выключена на старте)
        [SerializeField] private GameObject heroWithSwordModel;    
        // аниматор модели с мечом
        [SerializeField] private Animator swordAnimator;      
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

        public void SetDie()
        {
            animator.SetTrigger(Die1);
        }

        /// <summary>
        /// Запускает анимацию удара по врагу. Сама смерть врага срабатывает позже —
        /// от animation event'а в середине удара (см. <see cref="OnAttackHit"/>).
        /// </summary>
        public void PlayAttack(EnemyView enemy)
        {
            _attackTarget = enemy;

            if (HasParameter(animator, Attack1))
            {
                // Есть анимация удара -> смерть врага дёрнет animation event (OnAttackHit).
                animator.SetTrigger(Attack1);
            }
            else
            {
                // Нет анимации удара -> нет animation event: наносим удар сами через задержку.
                DOVirtual.DelayedCall(_fallbackHitDelay, OnAttackHit);
            }
        }

        /// <summary>
        /// Вызывается animation event'ом в момент удара (клинок в максимуме, ~середина анимации).
        /// Здесь враг умирает: анимация смерти в его контроллере + эффект.
        /// Событие в клипе удара навешивается в редакторе вручную.
        /// </summary>
        public void OnAttackHit()
        {
            if (_attackTarget == null)
                return;

            _attackTarget.Die();
            _attackTarget = null;

            // Итог боя для игровой логики / UI.
            events.RaiseHeroWin();
        }

        /// <summary>
        /// Вызывается, когда враг наносит герою смертельный удар (EnemyView.OnAttackHit).
        /// Проигрывает смерть героя и поднимает событие проигрыша.
        /// </summary>
        public void OnKilled()
        {
            SetDie();
            events.RaiseHeroLose();
        }

        /// <summary>
        /// Меняет модель героя на "с мечом": текущая уменьшается и скрывается,
        /// модель с мечом активируется и твинингом вырастает до нормы. После смены
        /// код управляет уже новым аниматором (swordAnimator).
        /// </summary>
        [ContextMenu("Equip Sword (test)")]
        public void EquipSword()
        {
            if (_hasSword || heroWithSwordModel == null || heroWithoutSwordModel == null || swordAnimator == null)
                return;

            _hasSword = true;

            // Эффект из спрайт-рендерера и прочее — вешается в инспекторе.
            onSwordEquipped?.Invoke();

            var swordlessScale = heroWithoutSwordModel.transform.localScale;
            var swordTargetScale = heroWithSwordModel.transform.localScale;

            // 1) текущая модель уменьшается, затем скрывается.
            heroWithoutSwordModel.transform
                .DOScale(swordlessScale * shrinkFactor, shrinkDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    heroWithoutSwordModel.SetActive(false);
                    heroWithoutSwordModel.transform.localScale = swordlessScale;

                    // 2) модель с мечом активируется в уменьшённом виде и растёт до нормы.
                    heroWithSwordModel.transform.localScale = swordTargetScale * shrinkFactor;
                    heroWithSwordModel.SetActive(true);

                    // 3) дальше код управляет новым аниматором.
                    animator = swordAnimator;
                    
                    //подменяем ссылки для вращения персонажа
                    _heroVisualTransform = _heroVisualTransformWithSword;
                    
                    if (HasParameter(animator, IsRun))
                        animator.SetBool(IsRun, _isRunning);

                    heroWithSwordModel.transform
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
