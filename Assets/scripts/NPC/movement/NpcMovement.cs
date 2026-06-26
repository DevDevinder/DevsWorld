using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NpcBrain))]
[RequireComponent(typeof(NpcMemory))]
[RequireComponent(typeof(NpcNeeds))]
public class NpcMovement : MonoBehaviour
{
    [Header("Movement")]
    public float exploreRadius = 12f;
    public float actionDistance = 2f;

    [Header("Debug")]
    public bool debugLogs = true;

    private NavMeshAgent agent;
    private NpcBrain brain;
    private NpcMemory memory;
    private NpcNeeds needs;

    private Vector3 currentExploreTarget;
    private bool hasExploreTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        brain = GetComponent<NpcBrain>();
        memory = GetComponent<NpcMemory>();
        needs = GetComponent<NpcNeeds>();
    }

    private void Update()
    {
        switch (brain.currentAction)
        {
            case NpcActionType.Idle:
                StopMoving();
                break;

            case NpcActionType.Explore:
            case NpcActionType.FindWater:
            case NpcActionType.FindFood:
                Explore();
                break;

            case NpcActionType.Drink:
                MoveToKnownLake();
                break;

            case NpcActionType.Sleep:
                StopMoving();
                break;
        }
    }

    private void Explore()
    {
        if (!hasExploreTarget || HasReachedDestination())
        {
            currentExploreTarget = GetRandomNavMeshPoint(transform.position, exploreRadius);
            hasExploreTarget = true;
            agent.SetDestination(currentExploreTarget);

            if (debugLogs)
                Debug.Log($"{name} is exploring.");
        }
    }

    private void MoveToKnownLake()
    {
        Memory lakeMemory = memory.GetClosestMemory(MemoryType.Lake);

        if (lakeMemory == null)
        {
            brain.currentAction = NpcActionType.FindWater;
            return;
        }

        agent.SetDestination(lakeMemory.position);

        float distance = Vector3.Distance(transform.position, lakeMemory.position);

        if (distance <= actionDistance)
        {
            StopMoving();
            needs.RestoreNeed(NpcNeedType.Thirst, 35f);

            if (debugLogs)
                Debug.Log($"{name} drank from the lake.");
        }
    }

    private void StopMoving()
    {
        if (agent.hasPath)
            agent.ResetPath();

        hasExploreTarget = false;
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending)
            return false;

        return agent.remainingDistance <= agent.stoppingDistance + 0.25f;
    }

    private Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += origin;
            randomDirection.y = origin.y;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }

        return origin;
    }
}