using UnityEngine;

public class WorldMemoryTarget : MonoBehaviour
{
    public MemoryType memoryType;

    [Header("Perception")]
    public bool canBeSeen = true;
    public bool canBeHeard = false;

    [Header("Area Knowledge")]
    public AreaKnowledgeType areaKnowledgeType;
    public float valueAmount = 20f;
    public float attentionAmount = 25f;
}