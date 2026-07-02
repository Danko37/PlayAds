using UnityEngine;
using UnityEngine.AI;

public class HeroView : MonoBehaviour
{
    private static readonly int IsRun = Animator.StringToHash("isRun");

    [SerializeField]
    private Transform _heroVisualTransform;
    [SerializeField]
    private Animator _heroAnimator;
    
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
        _heroAnimator.SetBool(IsRun, run);
    }
}
