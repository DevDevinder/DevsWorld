using UnityEngine;

[RequireComponent(typeof(NpcNeeds))]
[RequireComponent(typeof(NpcMemory))]
[RequireComponent(typeof(NpcAreaKnowledge))]
public class NeedPriorityEvaluator : MonoBehaviour
{
    [Header("Need Thresholds")]
    public float hungerSearchThreshold = 65f;
    public float thirstSearchThreshold = 65f;
    public float energySearchThreshold = 25f;
    public float socialSearchThreshold = 25f;
    public float safetySearchThreshold = 35f;
    public float warmthSearchThreshold = 35f;

    [Header("Harm Weights")]
    public float thirstHarmWeight = 2.2f;
    public float hungerHarmWeight = 1.8f;
    public float safetyHarmWeight = 2.0f;
    public float warmthHarmWeight = 1.4f;
    public float energyHarmWeight = 1.1f;
    public float socialHarmWeight = 0.5f;

    [Header("Known Target Scoring")]
    public float knownTargetBonus = 45f;
    public float distancePenaltyMultiplier = 0.18f;

    [Header("Opportunity Scoring")]
    public float nearbyOpportunityDistance = 18f;
    public float opportunityBonus = 65f;

    private NpcNeeds needs;
    private NpcMemory memory;
    private NpcAreaKnowledge areaKnowledge;

    private void Awake()
    {
        needs = GetComponent<NpcNeeds>();
        memory = GetComponent<NpcMemory>();
        areaKnowledge = GetComponent<NpcAreaKnowledge>();
    }

    public SearchGoal GetBestSearchGoal()
    {
        SearchGoal best = null;
        float bestScore = float.MinValue;

        EvaluateGoal(CreateFoodGoal(), ref best, ref bestScore);
        EvaluateGoal(CreateWaterGoal(), ref best, ref bestScore);
        EvaluateGoal(CreateShelterGoal(), ref best, ref bestScore);
        EvaluateGoal(CreateSafetyGoal(), ref best, ref bestScore);
        EvaluateGoal(CreateSocialGoal(), ref best, ref bestScore);

        return best;
    }

    private void EvaluateGoal(SearchGoal goal, ref SearchGoal best, ref float bestScore)
    {
        if (goal == null || goal.goalType == NeedGoalType.None)
            return;

        float score = goal.priority;

        Memory knownMemory = memory.GetBestMemoryForNeed(goal.memoryType, goal.urgency);
        AreaKnowledge knownArea = areaKnowledge.GetBestAreaForNeed(goal.areaType, goal.urgency);

        Vector3? target = null;
        float targetQuality = 0f;

        if (knownMemory != null)
        {
            target = knownMemory.position;
            targetQuality += knownMemory.confidence + knownMemory.usefulness;
        }

        if (knownArea != null && knownArea.GetValue(goal.areaType) > targetQuality)
        {
            target = knownArea.worldCenter;
            targetQuality = knownArea.GetValue(goal.areaType) + knownArea.confidence;
        }

        if (target.HasValue)
        {
            float distance = Vector3.Distance(transform.position, target.Value);
            float distancePenalty = distance * distancePenaltyMultiplier;

            score += knownTargetBonus + targetQuality - distancePenalty;

            if (distance <= nearbyOpportunityDistance)
                score += opportunityBonus;

            goal.targetPosition = target.Value;
            goal.hasKnownTarget = true;
        }

        goal.priority = score;

        if (score > bestScore)
        {
            bestScore = score;
            best = goal;
        }
    }

    private SearchGoal CreateFoodGoal()
    {
        if (needs.hunger >= hungerSearchThreshold)
            return null;

        float urgency = 100f - needs.hunger;
        float harmRisk = urgency * hungerHarmWeight;
        float priority = urgency + harmRisk;

        return new SearchGoal(
            NeedGoalType.Food,
            AreaKnowledgeType.Food,
            MemoryType.Food,
            priority,
            urgency,
            harmRisk
        );
    }

    private SearchGoal CreateWaterGoal()
    {
        if (needs.thirst >= thirstSearchThreshold)
            return null;

        float urgency = 100f - needs.thirst;
        float harmRisk = urgency * thirstHarmWeight;
        float priority = urgency + harmRisk;

        return new SearchGoal(
            NeedGoalType.Water,
            AreaKnowledgeType.Water,
            MemoryType.Lake,
            priority,
            urgency,
            harmRisk
        );
    }

    private SearchGoal CreateShelterGoal()
    {
        if (needs.energy >= energySearchThreshold && needs.warmth >= warmthSearchThreshold)
            return null;

        float energyUrgency = Mathf.Max(0f, 100f - needs.energy);
        float warmthUrgency = Mathf.Max(0f, 100f - needs.warmth);

        float urgency = Mathf.Max(energyUrgency, warmthUrgency);
        float harmRisk = energyUrgency * energyHarmWeight + warmthUrgency * warmthHarmWeight;
        float priority = urgency + harmRisk;

        return new SearchGoal(
            NeedGoalType.Shelter,
            AreaKnowledgeType.Shelter,
            MemoryType.House,
            priority,
            urgency,
            harmRisk
        );
    }

    private SearchGoal CreateSafetyGoal()
    {
        if (needs.safety >= safetySearchThreshold)
            return null;

        float urgency = 100f - needs.safety;
        float harmRisk = urgency * safetyHarmWeight;
        float priority = urgency + harmRisk;

        return new SearchGoal(
            NeedGoalType.Safety,
            AreaKnowledgeType.Shelter,
            MemoryType.House,
            priority,
            urgency,
            harmRisk
        );
    }

    private SearchGoal CreateSocialGoal()
    {
        if (needs.social >= socialSearchThreshold)
            return null;

        float urgency = 100f - needs.social;
        float harmRisk = urgency * socialHarmWeight;
        float priority = urgency + harmRisk;

        return new SearchGoal(
            NeedGoalType.Social,
            AreaKnowledgeType.Social,
            MemoryType.NPC,
            priority,
            urgency,
            harmRisk
        );
    }
}