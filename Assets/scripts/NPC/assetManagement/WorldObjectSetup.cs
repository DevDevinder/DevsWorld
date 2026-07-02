using UnityEngine;

[ExecuteAlways]
public class WorldObjectSetup : MonoBehaviour
{
    [Header("Preset")]
    public WorldObjectPreset preset;

    [Header("Setup")]
    public bool applyOnValidate = true;
    public bool addColliderIfMissing = true;
    public bool useBoxColliderIfMissing = true;

    [Header("Optional")]
    public Transform customInteractionPoint;

    private void OnValidate()
    {
        if (!applyOnValidate)
            return;

        ApplyPreset();
    }

    [ContextMenu("Apply Preset")]
    public void ApplyPreset()
    {
        if (preset == null)
            return;

        EnsureCollider();
        ConfigureMemoryTarget();

        if (preset.isNeedSource)
            ConfigureNeedSource();
    }

    private void EnsureCollider()
    {
        if (!addColliderIfMissing)
            return;

        if (GetComponent<Collider>() != null)
            return;

        if (useBoxColliderIfMissing)
            gameObject.AddComponent<BoxCollider>();
    }

    private void ConfigureMemoryTarget()
    {
        WorldMemoryTarget target = GetComponent<WorldMemoryTarget>();

        if (target == null)
            target = gameObject.AddComponent<WorldMemoryTarget>();

        target.memoryType = preset.memoryType;
        target.areaKnowledgeType = preset.areaKnowledgeType;
        target.canBeSeen = preset.canBeSeen;
        target.canBeHeard = preset.canBeHeard;
        target.valueAmount = preset.valueAmount;
        target.attentionAmount = preset.attentionAmount;
    }

    private void ConfigureNeedSource()
    {
        NeedSource source = GetComponent<NeedSource>();

        if (source == null)
            source = gameObject.AddComponent<NeedSource>();

        source.effects = CloneEffects(preset.effects);
        source.useClosestColliderPoint = preset.useClosestColliderPoint;
        source.customInteractionPoint = customInteractionPoint;
        source.interactionDistance = preset.interactionDistance;
        source.navMeshSampleRadius = preset.navMeshSampleRadius;
        source.regrows = preset.regrows;
        source.regrowPerSecond = preset.regrowPerSecond;
        source.destroyWhenEmpty = preset.destroyWhenEmpty;
    }

    private NeedSourceEffect[] CloneEffects(NeedSourceEffect[] sourceEffects)
    {
        if (sourceEffects == null)
            return new NeedSourceEffect[0];

        NeedSourceEffect[] cloned = new NeedSourceEffect[sourceEffects.Length];

        for (int i = 0; i < sourceEffects.Length; i++)
        {
            cloned[i] = new NeedSourceEffect
            {
                needType = sourceEffects[i].needType,
                restorePerSecond = sourceEffects[i].restorePerSecond,
                infinite = sourceEffects[i].infinite,
                availableAmount = sourceEffects[i].availableAmount,
                maxAmount = sourceEffects[i].maxAmount
            };
        }

        return cloned;
    }
}