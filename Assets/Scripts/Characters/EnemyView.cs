using DG.Tweening;
using UnityEngine;

namespace Characters
{
    public class EnemyView : EntityBase, IAttackAnimationReceiver
    {
        private static readonly int Die1 = Animator.StringToHash("die");
        private static readonly int Attack1 = Animator.StringToHash("attack");

        [SerializeField] private Collider bodyCollider;
        [SerializeField] private float deathAnimDuration = 0.3f;
        [SerializeField] private float disappearDuration = 0.3f;

        [Header("Атака по герою")]
        [Tooltip("Трансформ модели врага, которую поворачиваем к герою.")]
        [SerializeField] private Transform visualTransform;
        
        [Tooltip("Изометрический сдвиг поворота (как -135 у героя). Подстрой под модель.")]
        [SerializeField] private float rotationOffset = 135f;
        
        [Tooltip("Задержка удара, если у модели нет анимации атаки (animation event недоступен).")]
        [SerializeField] private float fallbackHitDelay = 0.5f;
        
        [Tooltip("Враг с оружием (меч) — влияет на звук удара по герою. Снять для безоружного.")]
        [SerializeField] private bool isArmed = true;
        
        [SerializeField]
        private bool isFinalEnemy;
        public bool IsFinalEnemy => isFinalEnemy;

        private HeroView _attackTarget;

        /// <summary>
        /// Мгновенно поворачивает врага в сторону цели (та же изометрическая логика, что у героя).
        /// </summary>
        public void FaceInstant(Vector3 targetWorldPos)
        {
            var t = visualTransform != null ? visualTransform : transform;

            Vector3 dir = targetWorldPos - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            var worldDir = Quaternion.FromToRotation(Vector3.forward, dir.normalized).eulerAngles.y;
            var res = worldDir - rotationOffset;

            t.localRotation = res < 0
                ? Quaternion.Euler(0, 360 - Mathf.Abs(res), 0)
                : Quaternion.Euler(0, res, 0);
        }

        /// <summary>
        /// Запускает анимацию удара по герою. Смерть героя срабатывает позже —
        /// от animation event'а в середине удара (см. <see cref="OnAttackHit"/>).
        /// </summary>
        public void Attack(HeroView hero)
        {
            _attackTarget = hero;

            if (animator != null && HasParameter(animator, Attack1))
            {
                // Есть анимация удара -> смерть героя дёрнет animation event (OnAttackHit).
                animator.SetTrigger(Attack1);
            }
            else
            {
                // Нет анимации удара -> нет animation event: наносим удар сами через задержку.
                DOVirtual.DelayedCall(fallbackHitDelay, OnAttackHit);
            }
        }

        /// <summary>
        /// Вызывается animation event'ом в момент удара врага (через AttackAnimationRelay).
        /// Наносит герою смертельный удар.
        /// </summary>
        public void OnAttackHit()
        {
            if (_attackTarget == null)
                return;

            // Звук удара врага (меч или кулак — в зависимости от вооружённости).
            AudioManager.Instance?.PlayEnemyAttack(isArmed);

            // Очки героя перетекают врагу (зеркально победе героя): у врага счётчик растёт.
            AnimateScore(Score, Score + _attackTarget.Score, 0.4f);

            _attackTarget.OnKilled();
            _attackTarget = null;
        }

        /// <summary>
        /// Проигрывает смерть врага: анимация смерти (если есть в контроллере),
        /// затем труп исчезает tween-эффектом и объект уничтожается.
        /// </summary>
        public void Die()
        {
            // Отключаем коллайдер, чтобы не было повторных срабатываний триггера.
            if (bodyCollider == null)
                bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null)
                bodyCollider.enabled = false;

            // Триггерим анимацию смерти только если параметр есть в контроллере
            // (у моделей без death-клипа его нет — просто пропускаем без варнингов).
            if (animator != null && HasParameter(animator, Die1))
                animator.SetTrigger(Die1);

            // Звук смерти гоблина.
            AudioManager.Instance?.PlayEnemyDeath();

            // Счёт гаснет, уезжает в 0, круг под ногами выключается.
            PlayDefeatScore(0.4f);

            DOVirtual.DelayedCall(deathAnimDuration, () =>
            {
                if (this == null) return;
                
                transform.DOScale(0f, disappearDuration)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        if (this != null)
                            Destroy(gameObject);
                    });
            });
        }

        private static bool HasParameter(Animator animator, int paramHash)
        {
            if (animator.runtimeAnimatorController == null)
                return false;

            foreach (var p in animator.parameters)
            {
                if (p.nameHash == paramHash)
                    return true;
            }

            return false;
        }
    }
}
