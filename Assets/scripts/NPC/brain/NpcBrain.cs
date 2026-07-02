using UnityEngine;

[RequireComponent(typeof(NpcNeeds))]
[RequireComponent(typeof(NpcMemory))]
[RequireComponent(typeof(NpcMovement))]
[RequireComponent(typeof(NpcAreaKnowledge))]
[RequireComponent(typeof(NpcDebugState))]
public class NpcBrain : MonoBehaviour
{
    [Header("Brain")]
    public NpcActionType currentActionType = NpcActionType.Idle;

    [Header("Decision Timing")]
    public float decisionInterval = 0.75f;

    [Header("Debug")]
    public bool debugLogs = true;

    private float decisionTimer;

    private NpcAction[] actions;
    private NpcAction currentAction;

    private NpcNeeds needs;
    private NpcDebugState debugState;

    private void Awake()
    {
        actions = GetComponents<NpcAction>();
        needs = GetComponent<NpcNeeds>();
        debugState = GetComponent<NpcDebugState>();
    }

    private void Update()
    {
        debugState.UpdateNeeds(needs);

        decisionTimer -= Time.deltaTime;

        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionInterval;
            ChooseBestAction();
        }

        currentAction?.Tick();
    }

    private void ChooseBestAction()
    {
        NpcAction bestAction = null;
        float bestScore = 0f;

        foreach (NpcAction action in actions)
        {
            if (!action.CanRun())
                continue;

            float score = action.GetScore();

            if (score > bestScore)
            {
                bestScore = score;
                bestAction = action;
            }
        }

        if (bestAction == null)
        {
            currentActionType = NpcActionType.Idle;
            debugState.SetAction(NpcActionType.Idle, 0f);
            return;
        }

        if (currentAction == bestAction)
        {
            debugState.SetAction(currentActionType, bestScore);
            return;
        }

        currentAction?.End();

        currentAction = bestAction;
        currentActionType = bestAction.ActionType;

        currentAction.Begin();

        debugState.SetAction(currentActionType, bestScore);

        if (debugLogs)
            Debug.Log($"{name} chose action: {currentActionType} | Score: {bestScore:0.0}");
    }
}