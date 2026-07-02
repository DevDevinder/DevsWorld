using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NpcNeeds))]
[RequireComponent(typeof(NpcMovement))]
[RequireComponent(typeof(NpcAreaKnowledge))]
[RequireComponent(typeof(NpcMemory))]
[RequireComponent(typeof(NeedPriorityEvaluator))]
[RequireComponent(typeof(NpcDebugState))]
public class SearchForNeedAction : NpcAction
{
    public override NpcActionType ActionType => NpcActionType.SearchForNeed;

    [Header("Search Movement")]
    public float searchDistance = 45f;
    public float minimumTargetDistance = 12f;
    public int candidatePoints = 40;

    [Header("Known Target Travel")]
    public float knownTargetRouteChance = 1f;
    public float directTargetDistance = 120f;

    [Header("Scoring")]
    public float unexploredWeight = 1.8f;
    public float relevantValueWeight = 1.4f;
    public float attentionWeight = 0.25f;
    public float distancePenaltyMultiplier = 0.2f;
    public float failurePenaltyMultiplier = 1.2f;
    public float routeProgressWeight = 70f;

    [Header("Anti-Stuck")]
    public float minimumUsefulMoveDistance = 8f;
    public float failedFallbackRadius = 16f;

    [Header("Debug")]
    public bool debugLogs;

    private NpcMovement movement;
    private NpcAreaKnowledge areaKnowledge;
    private NeedPriorityEvaluator priorityEvaluator;
    private NpcDebugState debugState;

    private SearchGoal currentGoal;
    private NeedGoalType previousGoalType = NeedGoalType.None;

    private Vector3 currentTarget;
    private bool hasTarget;

    private void Awake()
    {
        movement = GetComponent<NpcMovement>();
        areaKnowledge = GetComponent<NpcAreaKnowledge>();
        priorityEvaluator = GetComponent<NeedPriorityEvaluator>();
        debugState = GetComponent<NpcDebugState>();
    }

    public override bool CanRun()
    {
        SearchGoal goal = priorityEvaluator.GetBestSearchGoal();
        debugState.SetSearchGoal(goal);
        return goal != null;
    }

    public override float GetScore()
    {
        SearchGoal bestGoal = priorityEvaluator.GetBestSearchGoal();
        debugState.SetSearchGoal(bestGoal);

        if (bestGoal == null)
            return 0f;

        if (currentGoal == null || bestGoal.goalType != currentGoal.goalType)
        {
            currentGoal = bestGoal;
            hasTarget = false;
        }
        else
        {
            currentGoal = bestGoal;
        }

        return currentGoal.priority;
    }

    public override void Begin()
    {
        PickNewSearchTarget();
    }

    public override void Tick()
    {
        SearchGoal latestGoal = priorityEvaluator.GetBestSearchGoal();
        debugState.SetSearchGoal(latestGoal);

        if (latestGoal == null)
        {
            debugState.ClearTarget();
            return;
        }

        bool goalChanged = latestGoal.goalType != previousGoalType;

        currentGoal = latestGoal;

        if (goalChanged)
        {
            hasTarget = false;
            previousGoalType = currentGoal.goalType;

            if (debugLogs)
                Debug.Log($"{name} switched search goal to {currentGoal.goalType}");
        }

        if (!hasTarget || movement.HasReachedDestination)
            PickNewSearchTarget();

        movement.MoveTo(currentTarget);
        debugState.SetTarget(currentTarget, transform.position);
    }

    public override void End()
    {
        hasTarget = false;
        currentGoal = null;
        previousGoalType = NeedGoalType.None;

        debugState.ClearTarget();
    }

    private void PickNewSearchTarget()
    {
        if (currentGoal == null)
            return;

        currentTarget = ChooseSearchTarget();
        hasTarget = true;

        areaKnowledge.MarkAreaInvestigated(currentTarget);
        debugState.SetTarget(currentTarget, transform.position);

        if (debugLogs)
            Debug.Log($"{name} searching for {currentGoal.goalType} at {currentTarget}");
    }

    private Vector3 ChooseSearchTarget()
    {
        if (currentGoal.hasKnownTarget && Random.value <= knownTargetRouteChance)
        {
            Vector3? routeTarget = GetTargetTowardKnownGoal();

            if (routeTarget.HasValue)
                return routeTarget.Value;
        }

        AreaKnowledge knownArea = areaKnowledge.GetBestAreaForNeed(
            currentGoal.areaType,
            currentGoal.urgency
        );

        if (knownArea != null && knownArea.GetValue(currentGoal.areaType) > 10f)
        {
            Vector3 knownAreaPoint = GetRandomPointNear(
                knownArea.worldCenter,
                WorldAreaGrid.Instance.regionSize * 0.45f
            );

            if (Vector3.Distance(transform.position, knownAreaPoint) >= minimumUsefulMoveDistance)
                return knownAreaPoint;
        }

        return FindBestUnknownSearchPoint();
    }

    private Vector3? GetTargetTowardKnownGoal()
    {
        Vector3 knownTarget = currentGoal.targetPosition;

        float distanceToTarget = Vector3.Distance(transform.position, knownTarget);

        if (distanceToTarget <= directTargetDistance)
        {
            if (TryGetReachablePoint(knownTarget, 10f, out Vector3 directPoint))
                return directPoint;
        }

        Vector3 directionToTarget = knownTarget - transform.position;
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude < 0.01f)
            return null;

        directionToTarget.Normalize();

        Vector3 bestPoint = transform.position;
        float bestScore = float.MinValue;

        for (int i = 0; i < candidatePoints; i++)
        {
            Vector3 direction = Quaternion.Euler(
                0f,
                Random.Range(-55f, 55f),
                0f
            ) * directionToTarget;

            float distance = Random.Range(minimumTargetDistance, searchDistance);
            Vector3 samplePoint = transform.position + direction * distance;

            if (!TryGetReachablePoint(samplePoint, searchDistance, out Vector3 point))
                continue;

            float actualDistance = Vector3.Distance(transform.position, point);

            if (actualDistance < minimumUsefulMoveDistance)
                continue;

            AreaKnowledge area = areaKnowledge.GetOrCreateArea(point);

            if (area == null)
                continue;

            float routeProgressScore =
                Vector3.Dot(directionToTarget, (point - transform.position).normalized) * routeProgressWeight;

            float unexploredScore =
                (100f - area.exploration) * unexploredWeight;

            float relevantValueScore =
                area.GetValue(currentGoal.areaType) * relevantValueWeight;

            float failurePenalty =
                area.GetFailure(currentGoal.areaType) * failurePenaltyMultiplier;

            float score =
                routeProgressScore +
                unexploredScore +
                relevantValueScore -
                failurePenalty;

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = point;
            }
        }

        if (bestScore <= float.MinValue + 1f)
            return null;

        return bestPoint;
    }

    private Vector3 FindBestUnknownSearchPoint()
    {
        Vector3 bestPoint = transform.position;
        float bestScore = float.MinValue;

        Vector3 outwardDirection = GetOutwardSearchDirection();

        for (int i = 0; i < candidatePoints; i++)
        {
            Vector3 direction;

            if (outwardDirection.sqrMagnitude > 0.01f)
            {
                direction = Quaternion.Euler(
                    0f,
                    Random.Range(-90f, 90f),
                    0f
                ) * outwardDirection;
            }
            else
            {
                direction = Random.insideUnitSphere;
                direction.y = 0f;
                direction.Normalize();
            }

            float distance = Random.Range(minimumTargetDistance, searchDistance);
            Vector3 samplePoint = transform.position + direction * distance;

            if (!TryGetReachablePoint(samplePoint, searchDistance, out Vector3 point))
                continue;

            float actualDistance = Vector3.Distance(transform.position, point);

            if (actualDistance < minimumUsefulMoveDistance)
                continue;

            AreaKnowledge area = areaKnowledge.GetOrCreateArea(point);

            if (area == null)
                continue;

            float score = ScoreSearchPoint(area, point, actualDistance);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = point;
            }
        }

        if (bestScore <= float.MinValue + 1f)
            return FindSafeFallbackPoint();

        return bestPoint;
    }

    private float ScoreSearchPoint(AreaKnowledge area, Vector3 point, float chosenDistance)
    {
        float unexploredScore =
            (100f - area.exploration) * unexploredWeight;

        float relevantValueScore =
            area.GetValue(currentGoal.areaType) * relevantValueWeight;

        float attentionScore =
            area.GetAttentionTotal() * attentionWeight;

        float distancePenalty =
            Vector3.Distance(transform.position, point) * distancePenaltyMultiplier;

        float failurePenalty =
            area.GetFailure(currentGoal.areaType) * failurePenaltyMultiplier;

        return
            unexploredScore +
            relevantValueScore +
            attentionScore +
            chosenDistance * 0.3f -
            distancePenalty -
            failurePenalty;
    }

    private Vector3 GetOutwardSearchDirection()
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

    private Vector3 GetRandomPointNear(Vector3 center, float radius)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * radius;
            randomOffset.y = 0f;

            Vector3 samplePoint = center + randomOffset;

            if (TryGetReachablePoint(samplePoint, radius, out Vector3 point))
                return point;
        }

        return center;
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