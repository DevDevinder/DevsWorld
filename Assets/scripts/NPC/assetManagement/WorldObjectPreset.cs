using UnityEngine;

[CreateAssetMenu(
    fileName = "WorldObjectPreset",
    menuName = "DevWorld/World Object Preset"
)]
public class WorldObjectPreset : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "World Object";

    [Header("Memory")]
    public MemoryType memoryType;
    public AreaKnowledgeType areaKnowledgeType;

    [Header("Perception")]
    public bool canBeSeen = true;
    public bool canBeHeard = false;
    public float valueAmount = 45f;
    public float attentionAmount = 30f;

    [Header("Need Source")]
    public bool isNeedSource = true;
    public NeedSourceEffect[] effects;

    [Header("Interaction")]
    public bool useClosestColliderPoint = true;
    public float interactionDistance = 6f;
    public float navMeshSampleRadius = 10f;

    [Header("Regrowth")]
    public bool regrows = false;
    public float regrowPerSecond = 2f;

    [Header("Lifecycle")]
    public bool destroyWhenEmpty = false;
}