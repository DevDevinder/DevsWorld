using UnityEngine;

[RequireComponent(typeof(NpcNeeds))]
[RequireComponent(typeof(NpcMovement))]
[RequireComponent(typeof(NpcAreaKnowledge))]
[RequireComponent(typeof(NpcDebugState))]
public class UseNeedSourceAction : NpcAction
{
    public override NpcActionType ActionType => NpcActionType.UseNeedSource;

    [Header("Use Source")]
    public float needThreshold = 70f;
    public float emergencyThreshold = 25f;

    [Header("Scoring")]
    public float baseSourceScore = 180f;
    public float emergencyBonus = 160f;
    public float distancePenaltyMultiplier = 0.04f;

    [Header("Debug")]
    public NeedSource debugChosenSource;
    public NpcNeedType debugChosenNeed;
    public bool debugIsInRange;
    public bool debugCanRestore;
    public float debugRestoredLastTick;

    private NpcNeeds needs;
    private NpcMovement movement;
    private NpcAreaKnowledge areaKnowledge;
    private NpcDebugState debugState;

    private NeedSource chosenSource;
    private NpcNeedType chosenNeed;
    private Vector3 interactionPoint;

    private void Awake()
    {
        needs = GetComponent<NpcNeeds>();
        movement = GetComponent<NpcMovement>();
        areaKnowledge = GetComponent<NpcAreaKnowledge>();
        debugState = GetComponent<NpcDebugState>();
    }

    public override bool CanRun()
    {
        return FindBestUsableSource(out chosenSource, out chosenNeed);
    }

    public override float GetScore()
    {
        if (!FindBestUsableSource(out chosenSource, out chosenNeed))
            return 0f;

        interactionPoint = chosenSource.GetInteractionPoint(transform.position);

        float needValue = needs.GetNeed(chosenNeed);
        float urgency = 100f - needValue;
        float distance = Vector3.Distance(transform.position, interactionPoint);

        float score = baseSourceScore + urgency - distance * distancePenaltyMultiplier;

        if (needValue <= emergencyThreshold)
            score += emergencyBonus;

        return score;
    }

    public override void Tick()
    {
        debugRestoredLastTick = 0f;

        if (chosenSource == null || !chosenSource.CanRestore(chosenNeed))
            FindBestUsableSource(out chosenSource, out chosenNeed);

        if (chosenSource == null)
        {
            debugState.ClearTarget();
            return;
        }

        debugChosenSource = chosenSource;
        debugChosenNeed = chosenNeed;
        debugCanRestore = chosenSource.CanRestore(chosenNeed);

        interactionPoint = chosenSource.GetInteractionPoint(transform.position);

        movement.MoveTo(interactionPoint);
        debugState.SetTarget(interactionPoint, transform.position);

        debugIsInRange = chosenSource.IsInRange(transform.position);

        if (!debugIsInRange)
            return;

        movement.Stop();

        float restored = chosenSource.Use(chosenNeed, Time.deltaTime);
        debugRestoredLastTick = restored;

        if (restored <= 0f)
            return;

        needs.RestoreNeed(chosenNeed, restored);

        areaKnowledge.ObserveValue(
            chosenSource.transform.position,
            GetAreaKnowledgeTypeForNeed(chosenNeed),
            15f * Time.deltaTime,
            0f,
            true
        );

        if (needs.GetNeed(chosenNeed) >= 95f)
        {
            chosenSource = null;
            debugState.ClearTarget();
        }
    }

    public override void End()
    {
        chosenSource = null;
        debugState.ClearTarget();
    }

    private bool FindBestUsableSource(out NeedSource bestSource, out NpcNeedType bestNeed)
    {
        bestSource = null;
        bestNeed = NpcNeedType.Hunger;

        NeedSource[] sources = FindObjectsByType<NeedSource>(FindObjectsSortMode.None);

        float bestScore = float.MinValue;

        foreach (NeedSource source in sources)
        {
            if (source.effects == null)
                continue;

            foreach (NeedSourceEffect effect in source.effects)
            {
                NpcNeedType needType = effect.needType;

                if (needs.GetNeed(needType) >= needThreshold)
                    continue;

                if (!source.CanRestore(needType))
                    continue;

                Vector3 point = source.GetInteractionPoint(transform.position);

                float distance = Vector3.Distance(transform.position, point);
                float urgency = 100f - needs.GetNeed(needType);

                float score = urgency - distance * distancePenaltyMultiplier;

                if (needs.GetNeed(needType) <= emergencyThreshold)
                    score += emergencyBonus;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestSource = source;
                    bestNeed = needType;
                }
            }
        }

        return bestSource != null;
    }

    private AreaKnowledgeType GetAreaKnowledgeTypeForNeed(NpcNeedType needType)
    {
        return needType switch
        {
            NpcNeedType.Hunger => AreaKnowledgeType.Food,
            NpcNeedType.Thirst => AreaKnowledgeType.Water,
            NpcNeedType.Energy => AreaKnowledgeType.Shelter,
            NpcNeedType.Warmth => AreaKnowledgeType.Shelter,
            NpcNeedType.Safety => AreaKnowledgeType.Shelter,
            NpcNeedType.Social => AreaKnowledgeType.Social,
            _ => AreaKnowledgeType.Food
        };
    }
}