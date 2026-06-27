using UnityEngine;

[RequireComponent(typeof(NpcNeeds))]
[RequireComponent(typeof(NpcMemory))]
[RequireComponent(typeof(NpcMovement))]
[RequireComponent(typeof(NpcAreaKnowledge))]
public class EatAction : NpcAction
{
    public override NpcActionType ActionType => NpcActionType.Eat;

    [Header("Eat")]
    public float hungerThreshold = 70f;
    public float eatingDistance = 2.2f;
    public float hungerRestoreMultiplier = 1f;

    private NpcNeeds needs;
    private NpcMemory memory;
    private NpcMovement movement;
    private NpcAreaKnowledge areaKnowledge;

    private FoodSource chosenFood;

    private void Awake()
    {
        needs = GetComponent<NpcNeeds>();
        memory = GetComponent<NpcMemory>();
        movement = GetComponent<NpcMovement>();
        areaKnowledge = GetComponent<NpcAreaKnowledge>();
    }

    public override bool CanRun()
    {
        if (needs.hunger >= hungerThreshold)
            return false;

        chosenFood = FindBestKnownFood();
        return chosenFood != null;
    }

    public override float GetScore()
    {
        if (!CanRun())
            return 0f;

        float hungerUrgency = 100f - needs.hunger;

        float distance = Vector3.Distance(
            transform.position,
            chosenFood.transform.position
        );

        float distancePenalty = distance * 0.35f;

        return hungerUrgency + 70f - distancePenalty;
    }

    public override void Tick()
    {
        if (chosenFood == null || !chosenFood.HasFood)
        {
            if (chosenFood != null)
            {
                areaKnowledge.MarkAreaSearchFailure(
                    chosenFood.transform.position,
                    AreaKnowledgeType.Food,
                    25f
                );
            }

            chosenFood = null;
            return;
        }

        movement.MoveTo(chosenFood.transform.position);

        float distance = Vector3.Distance(
            transform.position,
            chosenFood.transform.position
        );

        if (distance > eatingDistance)
            return;

        movement.Stop();

        float eaten = chosenFood.Eat(chosenFood.eatAmountPerSecond * Time.deltaTime);
        needs.RestoreNeed(NpcNeedType.Hunger, eaten * hungerRestoreMultiplier);

        areaKnowledge.ObserveValue(
            chosenFood.transform.position,
            AreaKnowledgeType.Food,
            15f * Time.deltaTime,
            0f,
            true
        );
    }

    public override void End()
    {
        chosenFood = null;
    }

    private FoodSource FindBestKnownFood()
    {
        FoodSource[] foodSources = FindObjectsByType<FoodSource>(FindObjectsSortMode.None);

        FoodSource best = null;
        float bestScore = float.MinValue;

        foreach (FoodSource food in foodSources)
        {
            if (!food.HasFood)
                continue;

            Memory memoryEntry = memory.GetClosestMemory(MemoryType.Food);

            if (memoryEntry == null)
                continue;

            float distanceToMemory = Vector3.Distance(
                memoryEntry.position,
                food.transform.position
            );

            if (distanceToMemory > 4f)
                continue;

            float distance = Vector3.Distance(transform.position, food.transform.position);
            float score = food.foodAmount - distance * 0.3f;

            if (score > bestScore)
            {
                bestScore = score;
                best = food;
            }
        }

        return best;
    }
}