using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(WorldMemoryTarget))]
public class NeedSource : MonoBehaviour
{
    [Header("Need Source")]
    public NeedSourceEffect[] effects;

    [Header("Interaction")]
    public bool useClosestColliderPoint = true;
    public Transform customInteractionPoint;
    public float interactionDistance = 6f;
    public float navMeshSampleRadius = 10f;

    [Header("Regrowth")]
    public bool regrows = false;
    public float regrowPerSecond = 2f;

    [Header("Object Lifecycle")]
    public bool destroyWhenEmpty = false;

    [Header("Debug")]
    public Vector3 lastInteractionPoint;
    public float lastDistanceToSource;
    public bool lastInRange;

    private Collider sourceCollider;

    private void Awake()
    {
        sourceCollider = GetComponent<Collider>();
        ConfigureMemoryTarget();
    }

    private void Update()
    {
        if (regrows)
        {
            foreach (NeedSourceEffect effect in effects)
                effect.Regrow(regrowPerSecond * Time.deltaTime);
        }

        if (destroyWhenEmpty && !HasAnyAvailableEffect())
            Destroy(gameObject);
    }

    private void ConfigureMemoryTarget()
    {
        WorldMemoryTarget target = GetComponent<WorldMemoryTarget>();

        NpcNeedType primaryNeed = GetPrimaryNeedType();

        target.memoryType = GetMemoryTypeForNeed(primaryNeed);
        target.areaKnowledgeType = GetAreaKnowledgeTypeForNeed(primaryNeed);
        target.canBeSeen = true;
        target.valueAmount = 45f;
        target.attentionAmount = 30f;
    }

    public bool CanRestore(NpcNeedType needType)
    {
        foreach (NeedSourceEffect effect in effects)
        {
            if (effect.needType == needType && effect.HasAmount)
                return true;
        }

        return false;
    }

    public float Use(NpcNeedType needType, float deltaTime)
    {
        float total = 0f;

        foreach (NeedSourceEffect effect in effects)
        {
            if (effect.needType == needType)
                total += effect.Consume(deltaTime);
        }

        return total;
    }

    public Vector3 GetInteractionPoint(Vector3 npcPosition)
    {
        if (customInteractionPoint != null)
        {
            lastInteractionPoint = customInteractionPoint.position;
            return customInteractionPoint.position;
        }

        Vector3 sourcePoint = GetClosestSourcePoint(npcPosition);

        if (NavMesh.SamplePosition(sourcePoint, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            lastInteractionPoint = hit.position;
            return hit.position;
        }

        lastInteractionPoint = sourcePoint;
        return sourcePoint;
    }

    public bool IsInRange(Vector3 npcPosition)
    {
        Vector3 closest = GetClosestSourcePoint(npcPosition);

        lastDistanceToSource = Vector3.Distance(npcPosition, closest);
        lastInRange = lastDistanceToSource <= interactionDistance;

        return lastInRange;
    }

    private Vector3 GetClosestSourcePoint(Vector3 npcPosition)
    {
        if (sourceCollider == null)
            sourceCollider = GetComponent<Collider>();

        if (sourceCollider == null)
            return transform.position;

        Vector3 closest = sourceCollider.ClosestPoint(npcPosition);
        closest.y = npcPosition.y;
        return closest;
    }

    public bool HasAnyAvailableEffect()
    {
        foreach (NeedSourceEffect effect in effects)
        {
            if (effect.HasAmount)
                return true;
        }

        return false;
    }

    public NpcNeedType GetPrimaryNeedType()
    {
        if (effects == null || effects.Length == 0)
            return NpcNeedType.Hunger;

        return effects[0].needType;
    }

    private MemoryType GetMemoryTypeForNeed(NpcNeedType needType)
    {
        return needType switch
        {
            NpcNeedType.Hunger => MemoryType.Food,
            NpcNeedType.Thirst => MemoryType.Lake,
            NpcNeedType.Energy => MemoryType.House,
            NpcNeedType.Warmth => MemoryType.Fire,
            NpcNeedType.Safety => MemoryType.House,
            NpcNeedType.Social => MemoryType.NPC,
            _ => MemoryType.Food
        };
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