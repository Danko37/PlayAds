using System;
using TMPro;
using UnityEngine;

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
        [SerializeField] protected TextMeshProUGUI ScoreText;
        [SerializeField]
        protected Animator animator;
        
        public int Score;

        private void Start()
        {
            SetScoreText(Score);
        }

        public void SetScoreText(int score)
        {
            if(ScoreText == null) return;
            ScoreText.text = score.ToString();
        }
    }
}
