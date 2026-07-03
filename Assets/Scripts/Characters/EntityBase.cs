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
        
        public int Score;

        private void Start()
        {
            SetScore(Score);
        }

        public void SetScore(int score)
        {
            if(ScoreText == null) return;
            ScoreText.text = score.ToString();
            
            if(ScoreBackImage == null) return;

            ScoreBackImage.color = ScoreBackColor;
        }
    }
}
