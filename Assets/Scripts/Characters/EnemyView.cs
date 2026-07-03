using DG.Tweening;
using UnityEngine;

namespace Characters
{
    public class EnemyView : EntityBase
    {
        private static readonly int Die1 = Animator.StringToHash("die");

        [SerializeField] private Collider bodyCollider;
        [SerializeField] private float deathAnimDuration = 1f;
        [SerializeField] private float disappearDuration = 0.3f;

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

            // TODO: VFX эффект исчезновения трупа
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
