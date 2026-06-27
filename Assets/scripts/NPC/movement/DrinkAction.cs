using UnityEngine;

[RequireComponent(typeof(NpcNeeds))]
[RequireComponent(typeof(NpcMemory))]
[RequireComponent(typeof(NpcMovement))]
public class DrinkAction : NpcAction
{
    public override NpcActionType ActionType => NpcActionType.Drink;

    [Header("Drink")]
    public float thirstThreshold = 70f;
    public float emergencyThirstThreshold = 25f;
    public float drinkAmountPerSecond = 35f;

    [Header("Scoring")]
    public float baseKnownWaterScore = 85f;
    public float emergencyBonus = 120f;
    public float distancePenaltyMultiplier = 0.08f;

    private NpcNeeds needs;
    private NpcMemory memory;
    private NpcMovement movement;

    private Memory chosenWaterMemory;

    private void Awake()
    {
        needs = GetComponent<NpcNeeds>();
        memory = GetComponent<NpcMemory>();
        movement = GetComponent<NpcMovement>();
    }

    public override bool CanRun()
    {
        return needs.thirst < thirstThreshold && memory.Knows(MemoryType.Lake);
    }

    public override float GetScore()
    {
        if (!CanRun())
            return 0f;

        float thirstUrgency = 100f - needs.thirst;

        chosenWaterMemory = memory.GetBestMemoryForNeed(
            MemoryType.Lake,
            thirstUrgency
        );

        if (chosenWaterMemory == null)
            return 0f;

        float distance = Vector3.Distance(transform.position, chosenWaterMemory.position);
        float distancePenalty = distance * distancePenaltyMultiplier;

        float score =
            baseKnownWaterScore +
            thirstUrgency +
            chosenWaterMemory.confidence +
            chosenWaterMemory.usefulness -
            distancePenalty;

        if (needs.thirst <= emergencyThirstThreshold)
            score += emergencyBonus;

        return score;
    }

    public override void Tick()
    {
        if (chosenWaterMemory == null)
            return;

        movement.MoveTo(chosenWaterMemory.position);

        if (movement.IsCloseTo(chosenWaterMemory.position))
        {
            movement.Stop();

            needs.RestoreNeed(
                NpcNeedType.Thirst,
                drinkAmountPerSecond * Time.deltaTime
            );

            if (needs.thirst >= 95f)
                memory.MarkMemorySearchSuccess(chosenWaterMemory);
        }
    }

    public override void End()
    {
        chosenWaterMemory = null;
    }
}