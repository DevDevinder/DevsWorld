using System.Collections.Generic;
using UnityEngine;

public class NpcAreaKnowledge : MonoBehaviour
{
    [Header("Area Knowledge")]
    public List<AreaKnowledge> knownAreas = new();

    [Header("Decay")]
    public float attentionDecayPerSecond = 4f;
    public float searchFailureDecayPerSecond = 1.2f;

    [Header("Exploration Scoring")]
    public float recentlySeenSeconds = 20f;
    public float recentlyInvestigatedSeconds = 12f;

    public float recentlySeenPenalty = 45f;
    public float recentlyInvestigatedPenalty = 85f;

    public float distancePenaltyMultiplier = 0.2f;

    [Header("Curiosity")]
    public float unexploredWeight = 1.6f;
    public float attentionWeight = 1f;

    [Header("Need-Based Exploration Penalties")]
    public float lowFoodPenaltyWhenHungry = 35f;
    public float lowWaterPenaltyWhenThirsty = 35f;

    [Header("Debug")]
    public bool debugLogs;

    private NpcNeeds needs;

    private void Awake()
    {
        needs = GetComponent<NpcNeeds>();
    }

    private void Update()
    {
        DecayAreaData();
    }

    private void DecayAreaData()
    {
        float attentionDecay = attentionDecayPerSecond * Time.deltaTime;
        float failureDecay = searchFailureDecayPerSecond * Time.deltaTime;

        foreach (AreaKnowledge area in knownAreas)
        {
            area.DecayAttention(attentionDecay);
            area.DecaySearchFailures(failureDecay);
        }
    }

    public AreaKnowledge GetOrCreateArea(Vector3 worldPosition)
    {
        if (WorldAreaGrid.Instance == null)
        {
            Debug.LogWarning("No WorldAreaGrid found in scene.");
            return null;
        }

        Vector2Int cell = WorldAreaGrid.Instance.WorldToCell(worldPosition);

        foreach (AreaKnowledge area in knownAreas)
        {
            if (area.cell == cell)
                return area;
        }

        AreaKnowledge newArea = new AreaKnowledge(
            cell,
            WorldAreaGrid.Instance.CellToWorldCenter(cell)
        );

        knownAreas.Add(newArea);
        return newArea;
    }

    public void MarkVisible(Vector3 worldPosition, float explorationAmount)
    {
        AreaKnowledge area = GetOrCreateArea(worldPosition);

        if (area == null)
            return;

        area.IncreaseExploration(explorationAmount);
    }

    public void ObserveValue(
        Vector3 worldPosition,
        AreaKnowledgeType type,
        float valueAmount,
        float attentionAmount,
        bool visuallyConfirmed
    )
    {
        AreaKnowledge area = GetOrCreateArea(worldPosition);

        if (area == null)
            return;

        area.AddValue(type, valueAmount);

        if (visuallyConfirmed && IsSufficientConfirmedSource(area, type))
        {
            area.SatisfyAttention(type);
            area.IncreaseExploration(18f);
        }
        else
        {
            area.AddAttention(type, attentionAmount);
        }

        if (debugLogs)
            Debug.Log($"{name} updated {type} knowledge in area {area.cell}");
    }

    private bool IsSufficientConfirmedSource(AreaKnowledge area, AreaKnowledgeType type)
    {
        return type switch
        {
            AreaKnowledgeType.Water => area.waterValue >= 45f,
            AreaKnowledgeType.Food => area.foodValue >= 70f,
            AreaKnowledgeType.Wood => area.woodValue >= 60f,
            AreaKnowledgeType.Shelter => area.shelterValue >= 60f,
            AreaKnowledgeType.Social => area.socialValue >= 70f,
            _ => false
        };
    }

    public void MarkAreaInvestigated(Vector3 worldPosition)
    {
        AreaKnowledge area = GetOrCreateArea(worldPosition);

        if (area == null)
            return;

        area.MarkInvestigated();
    }

    public void MarkAreaSearchFailure(Vector3 worldPosition, AreaKnowledgeType type, float amount)
    {
        AreaKnowledge area = GetOrCreateArea(worldPosition);

        if (area == null)
            return;

        area.MarkSearchFailure(type, amount);
    }

    public AreaKnowledge GetBestAreaForNeed(AreaKnowledgeType type, float needUrgency)
    {
        AreaKnowledge best = null;
        float bestScore = float.MinValue;

        foreach (AreaKnowledge area in knownAreas)
        {
            float distance = Vector3.Distance(transform.position, area.worldCenter);
            float distancePenalty = distance * 0.35f;
            float failurePenalty = area.GetFailure(type);

            float score =
                needUrgency +
                area.GetValue(type) +
                area.confidence -
                distancePenalty -
                failurePenalty;

            if (score > bestScore)
            {
                bestScore = score;
                best = area;
            }
        }

        return best;
    }

    public AreaKnowledge GetBestAreaForExploration()
    {
        AreaKnowledge best = null;
        float bestScore = float.MinValue;

        foreach (AreaKnowledge area in knownAreas)
        {
            float score = GetExplorationScore(area);

            if (score > bestScore)
            {
                bestScore = score;
                best = area;
            }
        }

        return best;
    }

    public float GetExplorationScore(AreaKnowledge area)
    {
        float unexploredRatio = 1f - area.exploration / 100f;

        float unexploredScore =
            (100f - area.exploration) * unexploredWeight;

        float attentionScore =
            area.GetAttentionTotal() * attentionWeight * unexploredRatio;

        float distance = Vector3.Distance(transform.position, area.worldCenter);
        float distancePenalty = distance * distancePenaltyMultiplier;

        float recentSeenPenalty = WasRecentlySeen(area)
            ? recentlySeenPenalty * (1f - unexploredRatio)
            : 0f;

        float recentInvestigatedPenalty = WasRecentlyInvestigated(area)
            ? recentlyInvestigatedPenalty
            : 0f;

        float needPenalty = GetNeedBasedLowValuePenalty(area);
        float failurePenalty = area.generalSearchFailure * 0.5f;

        return
            unexploredScore +
            attentionScore -
            distancePenalty -
            recentSeenPenalty -
            recentInvestigatedPenalty -
            needPenalty -
            failurePenalty;
    }

    private float GetNeedBasedLowValuePenalty(AreaKnowledge area)
    {
        if (needs == null)
            return 0f;

        float penalty = 0f;

        if (needs.hunger < 45f && area.foodValue < 10f)
            penalty += lowFoodPenaltyWhenHungry;

        if (needs.thirst < 45f && area.waterValue < 10f)
            penalty += lowWaterPenaltyWhenThirsty;

        return penalty;
    }

    private bool WasRecentlySeen(AreaKnowledge area)
    {
        return Time.time - area.lastSeenTime <= recentlySeenSeconds;
    }

    private bool WasRecentlyInvestigated(AreaKnowledge area)
    {
        return Time.time - area.lastInvestigatedTime <= recentlyInvestigatedSeconds;
    }
}