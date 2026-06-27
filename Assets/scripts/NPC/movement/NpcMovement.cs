using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcMovement : MonoBehaviour
{
    [Header("Movement")]
    public float actionDistance = 2f;

    private NavMeshAgent agent;

    public bool HasDestination => agent.hasPath;
    public bool HasReachedDestination => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.25f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void MoveTo(Vector3 position)
    {
        agent.isStopped = false;
        agent.SetDestination(position);
    }

    public void Stop()
    {
        if (agent.hasPath)
            agent.ResetPath();

        agent.isStopped = true;
    }

    public bool IsCloseTo(Vector3 position)
    {
        return Vector3.Distance(transform.position, position) <= actionDistance;
    }
}