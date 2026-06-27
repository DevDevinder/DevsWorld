using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NpcNeeds))]
[RequireComponent(typeof(NpcMovement))]
[RequireComponent(typeof(NpcAreaKnowledge))]
[RequireComponent(typeof(NpcMemory))]
public class SearchForNeedAction : NpcAction
{
    public override NpcActionType ActionType => NpcActionType.SearchForNeed;

    [Header("Need Thresholds")]
    public float hungerSearchThreshold = 65f;
    public float thirstSearchThreshold = 65f;
    public float emergencyThreshold = 25f;

    [Header("Search Movement")]
    public float searchDistance = 45f;
    public float minimumTargetDistance = 12f;
    public int candidatePoints = 40;

    [Header("Last Known Water")]
    public float knownWaterDirectScoreBonus = 140f;
    public float knownWaterEmergencyBonus = 180f;
    public float knownWaterRouteChance = 1f;

    [Header("Known Area Use")]
    public float minimumKnownAreaValue = 12f;
    public float knownAreaTargetRadiusMultiplier = 0.45f;

    [Header("Scoring")]
    public float unexploredWeight = 1.8f;
    public float relevantValueWeight = 1.4f;
    public float distancePenaltyMultiplier = 0.2f;
    public float failurePenaltyMultiplier = 1.2f;

    [Header("Anti-Stuck")]
    public float minimumUsefulMoveDistance = 8f;
    public float failedFallbackRadius = 16f;

    private NpcNeeds needs;
    private NpcMovement movement;
    private NpcAreaKnowledge areaKnowledge;
    private NpcMemory memory;

    private AreaKnowledgeType currentSearchType;
    private Vector3 currentTarget;
    private bool hasTarget;

    private void Awake()
    {
        needs = GetComponent<NpcNeeds>();
        movement = GetComponent<NpcMovement>();
        areaKnowledge = GetComponent<NpcAreaKnowledge>();
        memory = GetComponent<NpcMemory>();
    }

    public override bool CanRun()
    {
        return ShouldSearchForFood() || ShouldSearchForWater();
    }

    public override float GetScore()
    {
        if (!CanRun())
            return 0f;

        float foodScore = GetFoodSearchScore();
        float waterScore = GetWaterSearchScore();

        if (waterScore > foodScore)
        {
            currentSearchType = AreaKnowledgeType.Water;
            return waterScore;
        }

        currentSearchType = AreaKnowledgeType.Food;
        return foodScore;
    }

    public override void Begin()
    {
        PickNewSearchTarget();
    }

    public override void Tick()
    {
        if (!hasTarget || movement.HasReachedDestination)
            PickNewSearchTarget();

        movement.MoveTo(currentTarget);
    }

    public override void End()
    {
        hasTarget = false;
    }

    private bool ShouldSearchForFood()
    {
        return needs.hunger < hungerSearchThreshold;
    }

    private bool ShouldSearchForWater()
    {
        return needs.thirst < thirstSearchThreshold;
    }

    private float GetFoodSearchScore()
    {
        if (!ShouldSearchForFood())
            return 0f;

        float urgency = 100f - needs.hunger;

        if (needs.hunger <= emergencyThreshold)
            urgency += 80f;

        return urgency + 20f;
    }

    private float GetWaterSearchScore()
    {
        if (!ShouldSearchForWater())
            return 0f;

        float urgency = 100f - needs.thirst;

        if (memory.Knows(MemoryType.Lake))
            urgency += knownWaterDirectScoreBonus;

        if (needs.thirst <= emergencyThreshold)
            urgency += knownWaterEmergencyBonus;

        return urgency + 20f;
    }

    private void PickNewSearchTarget()
    {
        currentTarget = ChooseSearchTarget();
        hasTarget = true;

        areaKnowledge.MarkAreaInvestigated(currentTarget);
    }

    private Vector3 ChooseSearchTarget()
    {
        if (currentSearchType == AreaKnowledgeType.Water && Random.value <= knownWaterRouteChance)
        {
            Vector3? waterTarget = GetTargetTowardKnownWater();

            if (waterTarget.HasValue)
                return waterTarget.Value;
        }

        float needUrgency = GetCurrentNeedUrgency();

        AreaKnowledge knownArea = areaKnowledge.GetBestAreaForNeed(
            currentSearchType,
            needUrgency
        );

        if (knownArea != null && knownArea.GetValue(currentSearchType) >= minimumKnownAreaValue)
        {
            Vector3 knownAreaPoint = GetRandomPointNear(
                knownArea.worldCenter,
                WorldAreaGrid.Instance.regionSize * knownAreaTargetRadiusMultiplier
            );

            if (Vector3.Distance(transform.position, knownAreaPoint) >= minimumUsefulMoveDistance)
                return knownAreaPoint;
        }

        return FindBestUnknownSearchPoint();
    }

    private Vector3? GetTargetTowardKnownWater()
    {
        Memory lakeMemory = memory.GetBestMemoryForNeed(
            MemoryType.Lake,
            100f - needs.thirst
        );

        if (lakeMemory == null)
            return null;

        if (TryGetReachablePoint(lakeMemory.position, 10f, out Vector3 directLakePoint))
            return directLakePoint;

        Vector3 directionToLake = lakeMemory.position - transform.position;
        directionToLake.y = 0f;

        if (directionToLake.sqrMagnitude < 0.01f)
            return null;

        directionToLake.Normalize();

        Vector3 bestPoint = transform.position;
        float bestScore = float.MinValue;

        for (int i = 0; i < candidatePoints; i++)
        {
            Vector3 direction = Quaternion.Euler(
                0f,
                Random.Range(-55f, 55f),
                0f
            ) * directionToLake;

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
                Vector3.Dot(directionToLake, (point - transform.position).normalized) * 60f;

            float unexploredScore =
                (100f - area.exploration) * unexploredWeight;

            float waterValueScore =
                area.waterValue * relevantValueWeight;

            float failurePenalty =
                area.waterSearchFailure * failurePenaltyMultiplier;

            float score =
                routeProgressScore +
                unexploredScore +
                waterValueScore -
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
            area.GetValue(currentSearchType) * relevantValueWeight;

        float attentionScore =
            area.GetAttentionTotal() * 0.25f;

        float distancePenalty =
            Vector3.Distance(transform.position, point) * distancePenaltyMultiplier;

        float failurePenalty =
            area.GetFailure(currentSearchType) * failurePenaltyMultiplier;

        return
            unexploredScore +
            relevantValueScore +
            attentionScore +
            chosenDistance * 0.3f -
            distancePenalty -
            failurePenalty;
    }

    private float GetCurrentNeedUrgency()
    {
        return currentSearchType switch
        {
            AreaKnowledgeType.Food => 100f - needs.hunger,
            AreaKnowledgeType.Water => 100f - needs.thirst,
            _ => 0f
        };
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