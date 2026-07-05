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

        public int Score;

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
    }
}
