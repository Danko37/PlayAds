using DG.Tweening;
using SpriteAnimation;
using UnityEngine;

namespace Characters
{
    public class HeroView : EntityBase, IAttackAnimationReceiver, IFootstepAnimationReceiver, IEquipAnimationReceiver
    {
        private static readonly int IsRun = Animator.StringToHash("isRun");
        private static readonly int Die1 = Animator.StringToHash("die");
        private static readonly int Attack1 = Animator.StringToHash("attack");
        private static readonly int Update1 = Animator.StringToHash("update");

        [Tooltip("Задержка удара, если у модели нет анимации атаки (animation event недоступен).")]
        [SerializeField]
        private float _fallbackHitDelay = 0.5f;

        [Tooltip("У модели есть анимация атаки с animation event'ом. Если выключено — удар " +
                 "наносится по таймеру (_fallbackHitDelay). Задаётся вручную: Luna не " +
                 "поддерживает чтение Animator.parameters.")]
        [SerializeField]
        private bool hasAttackAnimation = true;
        
        [SerializeField]
        private EventsSO events;
        
        [SerializeField]
        private Transform _heroVisualTransform;
        
        [SerializeField]
        private Transform _heroVisualTransformWithSword;
        
        // DF_Knight_2_idle (активна на старте)
        [SerializeField] 
        private GameObject heroWithoutSwordModel; 
        
        // DF_Knight_2_attack_02 (выключена на старте)
        [SerializeField] 
        private GameObject heroWithSwordModel;
        
        // аниматор модели с мечом
        [SerializeField] 
        private Animator swordAnimator; 
        
        [SerializeField, 
         Range(0.1f, 1f)] private float shrinkFactor = 0.8f;
        
        [SerializeField] 
        private float shrinkDuration = 0.25f;
        
        [SerializeField] 
        private float growDuration = 0.25f;
        
        [SerializeField]
        private SpriteAnimator EffectPrefab;
        
        [SerializeField]
        private Transform swordTarget;
        
        public Transform SwordTarget => swordTarget;

        private bool _hasSword;
        private bool _isRunning;
        private bool _chestOpenedRaised;
        private EnemyView _attackTarget;

        public EventsSO Events => events;

        public bool HasSword => _hasSword;

        public Transform HeroVisualTransform => _heroVisualTransform;

        public float InitialYRotation { get; private set; }
        private void Awake()
        {
            InitialYRotation = _heroVisualTransform.localRotation.eulerAngles.y;

            EffectPrefab.onAnimationFinished += EffectHandler;
        }

        private void OnDestroy()
        {
            EffectPrefab.onAnimationFinished -= EffectHandler;
        }

        private void EffectHandler()
        {
            EffectPrefab.gameObject.SetActive(false);
        }

        public void SetRun(bool run)
        {
            _isRunning = run;
            animator.SetBool(IsRun, run);
            // Звук шага теперь одиночный — по animation event'ам (см. OnFootstep).
        }

        /// <summary>
        /// Поворачивает визуал героя в сторону мирового направления (изометрическая
        /// математика: магический сдвиг −135°). Та же логика, что в GameManager.CharacterRotate
        /// и EnemyView.FaceInstant — вынесена сюда для переиспользования (напр. вбегание в интро).
        /// </summary>
        public void FaceDirection(Vector3 worldDir)
        {
            worldDir.y = 0;
            if (worldDir.sqrMagnitude < 0.0001f)
                return;

            var y = Quaternion.FromToRotation(Vector3.forward, worldDir.normalized).eulerAngles.y;
            var res = y - 135f;

            _heroVisualTransform.localRotation = res < 0
                ? Quaternion.Euler(0, 360 - Mathf.Abs(res), 0)
                : Quaternion.Euler(0, res, 0);
        }

        /// <summary>Возврат визуала в стартовый разворот (как при обычной остановке).</summary>
        public void ResetFacing()
        {
            _heroVisualTransform.localRotation = Quaternion.Euler(0, InitialYRotation, 0);
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

            if (hasAttackAnimation)
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

            // Звук удара меча (победа); смерть гоблина озвучит EnemyView.Die.
            AudioManager.Instance?.PlaySwordHit();

            AnimateScore(Score, Score + _attackTarget.Score, 0.4f);
            _attackTarget.Die();
            
            if (_attackTarget.IsFinalEnemy)
            {
                // Итог боя для игровой логики / UI.
                events.RaiseHeroWin(); 
            }
            
            _attackTarget = null;
        }

        /// <summary>
        /// Вызывается, когда враг наносит герою смертельный удар (EnemyView.OnAttackHit).
        /// Проигрывает смерть героя и поднимает событие проигрыша.
        /// </summary>
        public void OnKilled()
        {
            // Счёт героя гаснет, уезжает в 0, круг под ногами выключается (зеркально смерти врага).
            PlayDefeatScore(0.4f);

            // Звук смерти персонажа (поражение); удар врага озвучит EnemyView.OnAttackHit.
            AudioManager.Instance?.PlayHeroDeath();

            SetDie();
            events.RaiseHeroLose();
        }

        /// <summary>
        /// Меняет модель героя на "с мечом": текущая уменьшается и скрывается,
        /// модель с мечом активируется и твинингом вырастает до нормы. После смены
        /// код управляет уже новым аниматором (swordAnimator).
        /// </summary>
        public void EquipSword()
        {
            if (_hasSword || heroWithSwordModel == null || heroWithoutSwordModel == null || swordAnimator == null)
                return;

            _hasSword = true;

            // Звук апгрейда персонажа / получения меча.
            AudioManager.Instance?.PlaySwordUpgrade();

            EffectPrefab.gameObject.SetActive(true);

            var swordlessScale = heroWithoutSwordModel.transform.localScale;
            var swordTargetScale = heroWithSwordModel.transform.localScale;

            DOVirtual.DelayedCall(0.8f, () =>
            {
                AudioManager.Instance?.PlayManYes();
            });
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

                    // Модель с мечом — тот самый run+update контроллер, параметры есть по
                    // построению; ставим напрямую (Luna не поддерживает Animator.parameters).
                    animator.SetBool(IsRun, _isRunning);

                    // Модель с мечом появилась — дёргаем триггер update её аниматора.
                    animator.SetTrigger(Update1);

                    heroWithSwordModel.transform
                        .DOScale(swordTargetScale, growDuration)
                        .SetEase(Ease.OutBack);
                });
        }

        /// <summary>
        /// Вызывается animation event'ом в КОНЦЕ анимации поднятия меча (через
        /// EquipAnimationRelay). Только теперь возвращаем управление игроку. Guard от
        /// повторного вызова — на случай зацикленного/дважды навешенного события.
        /// </summary>
        public void OnEquipFinished()
        {
            if (_chestOpenedRaised)
                return;

            _chestOpenedRaised = true;
            events.RaiseChestOpened();
        }

        private void OnTriggerEnter(Collider other)
        {
            var collideEntity = other.GetComponent<EntityBase>();
            if (collideEntity ==  null)  return;

            var data = new CollideData
                { hero = this,  target = collideEntity};
            if (collideEntity.entityType == EntityType.Enemy ||
                collideEntity.entityType == EntityType.Chest)
            {
                events.OnCollideEventRaise(data);
            }
        }

        /// <summary>
        /// Вызывается animation event'ом на каждый шаг (через FootstepAnimationRelay) —
        /// одиночный звук шага.
        /// </summary>
        public void OnFootstep()
        {
            AudioManager.Instance?.PlayFootstep();
        }
    }
}
