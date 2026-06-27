using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NpcMovement))]
[RequireComponent(typeof(NpcAreaKnowledge))]
[RequireComponent(typeof(NpcNeeds))]
public class ExploreAction : NpcAction
{
    public override NpcActionType ActionType => NpcActionType.Explore;

    [Header("Explore")]
    public float exploreDistance = 45f;
    public float minimumTargetDistance = 12f;
    public int candidatePoints = 40;

    [Header("Scoring")]
    public float unexploredWeight = 2.2f;
    public float distanceFromCurrentAreaWeight = 0.7f;
    public float knownBadAreaPenalty = 45f;

    [Header("Needs")]
    public float criticalNeedScore = 2f;
    public float lowNeedScore = 8f;
    public float normalExploreScore = 25f;

    [Header("Anti-Stuck")]
    public float minimumUsefulMoveDistance = 8f;
    public float failedFallbackRadius = 16f;

    private NpcMovement movement;
    private NpcAreaKnowledge areaKnowledge;
    private NpcNeeds needs;

    private Vector3 currentTarget;
    private bool hasTarget;

    private void Awake()
    {
        movement = GetComponent<NpcMovement>();
        areaKnowledge = GetComponent<NpcAreaKnowledge>();
        needs = GetComponent<NpcNeeds>();
    }

    public override float GetScore()
    {
        if (needs.hunger <= 25f || needs.thirst <= 25f)
            return criticalNeedScore;

        if (needs.hunger <= 45f || needs.thirst <= 45f)
            return lowNeedScore;

        return normalExploreScore;
    }

    public override void Begin()
    {
        PickNewExploreTarget();
    }

    public override void Tick()
    {
        if (!hasTarget || movement.HasReachedDestination)
            PickNewExploreTarget();

        movement.MoveTo(currentTarget);
    }

    public override void End()
    {
        hasTarget = false;
    }

    private void PickNewExploreTarget()
    {
        currentTarget = FindFrontierExplorePoint();
        hasTarget = true;

        areaKnowledge.MarkAreaInvestigated(currentTarget);
    }

    private Vector3 FindFrontierExplorePoint()
    {
        Vector3 bestPoint = transform.position;
        float bestScore = float.MinValue;

        Vector3 awayFromCurrentKnownArea = GetAwayFromCurrentAreaDirection();

        for (int i = 0; i < candidatePoints; i++)
        {
            Vector3 direction;

            if (awayFromCurrentKnownArea.sqrMagnitude > 0.01f)
            {
                direction = Quaternion.Euler(
                    0f,
                    Random.Range(-90f, 90f),
                    0f
                ) * awayFromCurrentKnownArea;
            }
            else
            {
                direction = Random.insideUnitSphere;
                direction.y = 0f;
                direction.Normalize();
            }

            float distance = Random.Range(minimumTargetDistance, exploreDistance);
            Vector3 samplePoint = transform.position + direction * distance;

            if (!TryGetReachablePoint(samplePoint, exploreDistance, out Vector3 reachablePoint))
                continue;

            float actualDistance = Vector3.Distance(transform.position, reachablePoint);

            if (actualDistance < minimumUsefulMoveDistance)
                continue;

            float score = ScoreExplorePoint(reachablePoint, actualDistance);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = reachablePoint;
            }
        }

        if (bestScore <= float.MinValue + 1f)
            return FindSafeFallbackPoint();

        return bestPoint;
    }

    private float ScoreExplorePoint(Vector3 point, float chosenDistance)
    {
        AreaKnowledge area = areaKnowledge.GetOrCreateArea(point);

        if (area == null)
            return -999f;

        float unexploredScore = (100f - area.exploration) * unexploredWeight;
        float attentionScore = area.GetAttentionTotal() * 0.25f;
        float distanceScore = chosenDistance * distanceFromCurrentAreaWeight;

        float badKnownAreaPenalty = 0f;

        if (area.exploration > 65f && area.GetAttentionTotal() <= 5f)
            badKnownAreaPenalty += knownBadAreaPenalty;

        if (needs.hunger < 45f && area.foodValue <= 5f)
            badKnownAreaPenalty += knownBadAreaPenalty;

        if (needs.thirst < 45f && area.waterValue <= 5f)
            badKnownAreaPenalty += knownBadAreaPenalty;

        return unexploredScore + attentionScore + distanceScore - badKnownAreaPenalty;
    }

    private Vector3 GetAwayFromCurrentAreaDirection()
    {
        AreaKnowledge currentArea = areaKnowledge.GetOrCreateArea(transform.position);

        if (currentArea == null)
            return transform.forward;

        Vector3 away = transform.position - currentArea.worldCenter;
        away.y = 0f;

        if (away.sqrMagnitude < 0.01f)
            return transform.forward;

        return away.normalized;
    }

    private bool TryGetReachablePoint(Vector3 samplePoint, float sampleRadius, out Vector3 point)
    {
        point = transform.position;

        if (!NavMesh.SamplePosition(samplePoint, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            return false;

        NavMeshPath path = new NavMeshPath();

        if (!NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
            return false;

        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        point = hit.position;
        return true;
    }

    private Vector3 FindSafeFallbackPoint()
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * failedFallbackRadius;
            randomDirection.y = 0f;

            Vector3 samplePoint = transform.position + randomDirection;

            if (TryGetReachablePoint(samplePoint, failedFallbackRadius, out Vector3 point))
                return point;
        }

        return transform.position;
    }
}