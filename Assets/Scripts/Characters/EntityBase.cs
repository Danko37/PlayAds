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
    }
}
