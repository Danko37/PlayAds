using UnityEngine;
using UnityEngine.AI;

public class HeroView : MonoBehaviour
{
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
        
        InitialYRotation = _heroVisualTransform.rotation.eulerAngles.y;
        
        Debug.Log(_heroVisualTransform.localRotation);
    }
}
