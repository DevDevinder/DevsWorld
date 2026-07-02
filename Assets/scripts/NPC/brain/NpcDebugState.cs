using UnityEngine;

public class NpcDebugState : MonoBehaviour
{
    [Header("Brain")]
    public NpcActionType currentAction;
    public NeedGoalType currentSearchGoal;
    public float currentActionScore;
    public string currentDecision;

    [Header("Needs")]
    public bool needsFood;
    public bool needsWater;
    public bool needsSleep;
    public bool needsWarmth;
    public bool needsSafety;
    public bool needsSocial;

    [Header("Need Values")]
    [Range(0, 100)] public float hunger;
    [Range(0, 100)] public float thirst;
    [Range(0, 100)] public float energy;
    [Range(0, 100)] public float warmth;
    [Range(0, 100)] public float safety;
    [Range(0, 100)] public float social;

    [Header("Search Target")]
    public bool hasTarget;
    public Vector3 currentTargetPosition;
    public float distanceToTarget;

    [Header("Search Details")]
    public string searchReason;
    public float searchPriority;
    public float searchUrgency;
    public float searchHarmRisk;

    public void UpdateNeeds(NpcNeeds needs)
    {
        hunger = needs.hunger;
        thirst = needs.thirst;
        energy = needs.energy;
        warmth = needs.warmth;
        safety = needs.safety;
        social = needs.social;

        needsFood = hunger < 65f;
        needsWater = thirst < 65f;
        needsSleep = energy < 25f;
        needsWarmth = warmth < 35f;
        needsSafety = safety < 35f;
        needsSocial = social < 25f;
    }

    public void SetAction(NpcActionType action, float score)
    {
        currentAction = action;
        currentActionScore = score;
        currentDecision = action.ToString();
    }

    public void SetSearchGoal(SearchGoal goal)
    {
        if (goal == null)
        {
            currentSearchGoal = NeedGoalType.None;
            searchReason = "";
            searchPriority = 0f;
            searchUrgency = 0f;
            searchHarmRisk = 0f;
            return;
        }

        currentSearchGoal = goal.goalType;
        searchPriority = goal.priority;
        searchUrgency = goal.urgency;
        searchHarmRisk = goal.harmRisk;

        searchReason =
            $"Searching for {goal.goalType} | Priority {goal.priority:0.0} | Urgency {goal.urgency:0.0}";
    }

    public void SetTarget(Vector3 target, Vector3 npcPosition)
    {
        hasTarget = true;
        currentTargetPosition = target;
        distanceToTarget = Vector3.Distance(npcPosition, target);
    }

    public void ClearTarget()
    {
        hasTarget = false;
        currentTargetPosition = Vector3.zero;
        distanceToTarget = 0f;
    }
}