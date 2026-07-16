using UnityEngine;

namespace Characters
{
    public class InteractBase : MonoBehaviour
    {
        public EntityType entityType;
        
        //когдща кликаем по врагу - идем к этой точке
        [SerializeField] 
        public Transform moveTarget;
    }
}