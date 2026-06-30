using UnityEngine;
using UnityEngine.AI;

public class HeroView : MonoBehaviour
{
    void Start()
    {
        var agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        Debug.Log(agent.isOnNavMesh);
    }
}
