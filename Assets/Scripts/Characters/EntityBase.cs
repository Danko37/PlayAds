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
        [SerializeField]
        protected Animator animator;
        
        public int Score;
    }
}
