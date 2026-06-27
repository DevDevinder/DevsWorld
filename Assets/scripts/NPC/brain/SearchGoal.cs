using UnityEngine;

[System.Serializable]
public class SearchGoal
{
    public NeedGoalType goalType;
    public AreaKnowledgeType areaType;
    public MemoryType memoryType;

    public float priority;
    public float urgency;
    public float harmRisk;

    public Vector3 targetPosition;
    public bool hasKnownTarget;

    public SearchGoal(
        NeedGoalType goalType,
        AreaKnowledgeType areaType,
        MemoryType memoryType,
        float priority,
        float urgency,
        float harmRisk
    )
    {
        this.goalType = goalType;
        this.areaType = areaType;
        this.memoryType = memoryType;
        this.priority = priority;
        this.urgency = urgency;
        this.harmRisk = harmRisk;
    }
}