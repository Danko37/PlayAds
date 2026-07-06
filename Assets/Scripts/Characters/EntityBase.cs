using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Characters
{
    public enum EntityType
    {
        Hero,
        Enemy,
        Chest
    }

    public class EntityBase : InteractBase
    {
        public int Score;
        
        [SerializeField]
        protected Image ScoreBackImage;
        
        [SerializeField] 
        protected Color ScoreBackColor;
        
        [SerializeField] 
        protected TextMeshProUGUI ScoreText;
        
        [SerializeField]
        protected Animator animator;
        
        [SerializeField]
        protected GameObject ColorCircleGo;

        [Tooltip("CanvasGroup со счётом — гаснет при проигрыше этой сущности.")]
        [SerializeField]
        protected CanvasGroup scoreCanvasGroup;

        [Header("Пульс при клике")]
        [Tooltip("Что масштабировать при клике (модель). Пусто — весь объект.")]
        [SerializeField] protected Transform pulseTarget;
        [SerializeField] protected float pulseScale = 1.3f;
        [SerializeField] protected float pulseTime = 0.2f;

        private bool _pulsing;

        private void Start()
        {
            SetScore(Score);
        }

        public void SetScore(int score)
        {
            SetTextScore(score);
            SetTextBackColor();
            
            Score = score;
        }

        private void SetTextScore(int score)
        {
            if(ScoreText == null) return;
            ScoreText.text = score.ToString();
        }

        private void SetTextBackColor()
        {
            ScoreBackImage.color = ScoreBackColor;
        }

        /// <summary>
        /// Плавно анимирует значение счётчика очков (from -> to) с пружинкой для красоты.
        /// </summary>
        public void AnimateScore(int from, int to, float time)
        {
            int current = from;

            DOTween.To(() => current, v =>
                {
                    // Тик на каждое изменившееся очко (перетекание счёта в бою/сундуке).
                    if (v != current)
                        AudioManager.Instance?.PlayScoreTick();

                    current = v;
                    SetScore(v);
                }, to, time)
                .SetEase(Ease.OutCubic);

            if (ScoreText != null)
                ScoreText.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f);
        }

        /// <summary>
        /// Визуал поражения этой сущности: счёт гаснет (CanvasGroup), значение уезжает в 0,
        /// круг под ногами выключается. Одинаково для врага (Die) и героя (OnKilled).
        /// </summary>
        public void PlayDefeatScore(float time)
        {
            if (scoreCanvasGroup != null)
                scoreCanvasGroup.DOFade(0f, time);

            AnimateScore(Score, 0, time);

            if (ColorCircleGo != null)
                ColorCircleGo.SetActive(false);
        }

        /// <summary>
        /// Короткий «пульс» скейлом при клике по сущности — обратная связь, что кликнули
        /// по интерактиву. Масштабируется pulseTarget (модель), либо весь объект.
        /// </summary>
        public void PulseClick()
        {
            if (_pulsing)
                return;

            var t = pulseTarget != null ? pulseTarget : transform;
            _pulsing = true;

            Vector3 baseScale = t.localScale;
            t.DOScale(baseScale * pulseScale, pulseTime * 0.5f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    t.localScale = baseScale;
                    _pulsing = false;
                });
        }
    }
}
