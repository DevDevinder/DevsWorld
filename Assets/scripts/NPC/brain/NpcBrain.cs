using UnityEngine;

[RequireComponent(typeof(NpcNeeds))]
[RequireComponent(typeof(NpcMemory))]
public class NpcBrain : MonoBehaviour
{
    [Header("Brain")]
    public NpcActionType currentAction = NpcActionType.Idle;

    [Header("Decision Timing")]
    public float decisionInterval = 1f;

    private float decisionTimer;

    private NpcNeeds needs;
    private NpcMemory memory;

    private void Awake()
    {
        needs = GetComponent<NpcNeeds>();
        memory = GetComponent<NpcMemory>();
    }

    private void Update()
    {
        decisionTimer -= Time.deltaTime;

        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionInterval;
            DecideNextAction();
        }
    }

    private void DecideNextAction()
    {
        NpcActionType bestAction = NpcActionType.Idle;
        float bestScore = 0f;

        ScoreAction(NpcActionType.Explore, GetExploreScore(), ref bestAction, ref bestScore);
        ScoreAction(NpcActionType.FindWater, GetFindWaterScore(), ref bestAction, ref bestScore);
        ScoreAction(NpcActionType.Drink, GetDrinkScore(), ref bestAction, ref bestScore);
        ScoreAction(NpcActionType.FindFood, GetFindFoodScore(), ref bestAction, ref bestScore);
        ScoreAction(NpcActionType.Eat, GetEatScore(), ref bestAction, ref bestScore);
        ScoreAction(NpcActionType.Sleep, GetSleepScore(), ref bestAction, ref bestScore);

        if (currentAction != bestAction)
        {
            currentAction = bestAction;
            Debug.Log($"{name} decided to: {currentAction}");
        }
    }

    private void ScoreAction(
        NpcActionType action,
        float score,
        ref NpcActionType bestAction,
        ref float bestScore
    )
    {
        if (score > bestScore)
        {
            bestScore = score;
            bestAction = action;
        }
    }

    private float GetExploreScore()
    {
        return 10f;
    }

    private float GetFindWaterScore()
    {
        if (needs.thirst > 60f)
            return 0f;

        if (memory.Knows(MemoryType.Lake))
            return 0f;

        return 100f - needs.thirst;
    }

    private float GetDrinkScore()
    {
        if (needs.thirst > 70f)
            return 0f;

        if (!memory.Knows(MemoryType.Lake))
            return 0f;

        return 100f - needs.thirst + 30f;
    }

    private float GetFindFoodScore()
    {
        if (needs.hunger > 60f)
            return 0f;

        return 100f - needs.hunger;
    }

    private float GetEatScore()
    {
        if (needs.hunger > 70f)
            return 0f;

        // Later this will check inventory.
        return 0f;
    }

    private float GetSleepScore()
    {
        if (needs.energy > 35f)
            return 0f;

        return 100f - needs.energy + 20f;
    }
}