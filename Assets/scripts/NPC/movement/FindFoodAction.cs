using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NpcNeeds))]
[RequireComponent(typeof(NpcMovement))]
[RequireComponent(typeof(NpcAreaKnowledge))]
public class FindFoodAction : NpcAction
{
    public override NpcActionType ActionType => NpcActionType.Eat;

    public float hungerThreshold = 65f;
    public float emergencyHungerThreshold = 25f;

    public float randomSearchRadius = 35f;
    public int randomCandidatePoints = 20;

    private NpcNeeds needs;
    private NpcMovement movement;
    private NpcAreaKnowledge areaKnowledge;

    private void Awake()
    {
        needs = GetComponent<NpcNeeds>();
        movement = GetComponent<NpcMovement>();
        areaKnowledge = GetComponent<NpcAreaKnowledge>();
    }

    public override bool CanRun()
    {
        return needs.hunger < hungerThreshold;
    }

    public override float GetScore()
    {
        if (!CanRun())
            return 0f;

        float hungerUrgency = 100f - needs.hunger;

        if (needs.hunger <= emergencyHungerThreshold)
            hungerUrgency += 80f;

        return hungerUrgency;
    }

    public override void Tick()
    {
        if (!movement.HasDestination || movement.HasReachedDestination)
        {
            Vector3 target = ChooseFoodSearchTarget();
            movement.MoveTo(target);
        }
    }

    private Vector3 ChooseFoodSearchTarget()
    {
        AreaKnowledge knownFoodArea = areaKnowledge.GetBestAreaForNeed(
            AreaKnowledgeType.Food,
            100f - needs.hunger
        );

        if (knownFoodArea != null && knownFoodArea.foodValue > 10f)
            return GetRandomPointNear(knownFoodArea.worldCenter, WorldAreaGrid.Instance.regionSize * 0.45f);

        return FindBestUnknownSearchPoint();
    }

    private Vector3 FindBestUnknownSearchPoint()
    {
        Vector3 bestPoint = transform.position;
        float bestScore = float.MinValue;

        for (int i = 0; i < randomCandidatePoints; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * randomSearchRadius;
            randomDirection.y = 0f;

            Vector3 samplePoint = transform.position + randomDirection;

            if (!NavMesh.SamplePosition(samplePoint, out NavMeshHit hit, randomSearchRadius, NavMesh.AllAreas))
                continue;

            AreaKnowledge area = areaKnowledge.GetOrCreateArea(hit.position);

            if (area == null)
                continue;

            float unexploredScore = 100f - area.exploration;
            float foodScore = area.foodValue;
            float failurePenalty = area.foodSearchFailure;
            float distancePenalty = Vector3.Distance(transform.position, area.worldCenter) * 0.15f;

            float score = unexploredScore + foodScore - failurePenalty - distancePenalty;

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = hit.position;
            }
        }

        return bestPoint;
    }

    private Vector3 GetRandomPointNear(Vector3 center, float radius)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * radius;
            randomOffset.y = 0f;

            Vector3 samplePoint = center + randomOffset;

            if (NavMesh.SamplePosition(samplePoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }

        return center;
    }
}