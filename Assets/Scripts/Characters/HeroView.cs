using System;
using UnityEngine;
using UnityEngine.AI;

namespace Characters
{
    public class HeroView : EntityBase
    {
        private static readonly int IsRun = Animator.StringToHash("isRun");

        [SerializeField] 
        private EventsSO events;
        [SerializeField]
        private Transform _heroVisualTransform;
    
        public Transform HeroVisualTransform => _heroVisualTransform;
    
        [SerializeField]
        private NavMeshAgent _navMeshAgent;
    
        public float InitialYRotation { get; private set; }
    
        public NavMeshAgent NavMeshAgent => _navMeshAgent;
        void Start()
        {
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.autoBraking = false;
            _navMeshAgent.speed = 8f;
            _navMeshAgent.acceleration = 1000f;
            _navMeshAgent.angularSpeed = 1000f;
            _navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        
            InitialYRotation = _heroVisualTransform.localRotation.eulerAngles.y;
        
            Debug.Log(_heroVisualTransform.localRotation);
        }

        public void SetRun(bool run)
        {
            animator.SetBool(IsRun, run);
        }

        private void OnTriggerEnter(Collider other)
        {
            var collideEntity = other.GetComponent<EntityBase>();
            if (collideEntity ==  null)  return;

            var data = new CollideData
                { entityType = collideEntity.entityType, heroScore = Score, enemyScore = collideEntity.Score };
            if (collideEntity.entityType == EntityType.Enemy)
            {
                events.OnCollideEventRaise(data);
            }

            
        }
    }
}
