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
    public float interactionDistance = 3.5f;
    public float navMeshSampleRadius = 10f;

    [Header("Regrowth")]
    public bool regrows = false;
    public float regrowPerSecond = 2f;

    [Header("Object Lifecycle")]
    public bool destroyWhenEmpty = false;

    [Header("Debug")]
    public bool debugDraw = true;
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
        float totalRestored = 0f;

        foreach (NeedSourceEffect effect in effects)
        {
            if (effect.needType != needType)
                continue;

            totalRestored += effect.Consume(deltaTime);
        }

        return totalRestored;
    }

    public Vector3 GetInteractionPoint(Vector3 npcPosition)
    {
        if (customInteractionPoint != null)
        {
            lastInteractionPoint = customInteractionPoint.position;
            return customInteractionPoint.position;
        }

        Vector3 closestSourcePoint = GetClosestSourcePoint(npcPosition);

        if (NavMesh.SamplePosition(closestSourcePoint, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            lastInteractionPoint = hit.position;
            return hit.position;
        }

        lastInteractionPoint = closestSourcePoint;
        return closestSourcePoint;
    }

    public bool IsInRange(Vector3 npcPosition)
    {
        Vector3 closestSourcePoint = GetClosestSourcePoint(npcPosition);

        lastDistanceToSource = Vector3.Distance(npcPosition, closestSourcePoint);
        lastInRange = lastDistanceToSource <= interactionDistance;

        return lastInRange;
    }

    private Vector3 GetClosestSourcePoint(Vector3 npcPosition)
    {
        if (sourceCollider == null)
            sourceCollider = GetComponent<Collider>();

        if (sourceCollider == null)
            return transform.position;

        Vector3 closestPoint = sourceCollider.ClosestPoint(npcPosition);
        closestPoint.y = npcPosition.y;

        return closestPoint;
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

    private void OnDrawGizmosSelected()
    {
        if (!debugDraw)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(lastInteractionPoint, 0.6f);

        Gizmos.color = lastInRange ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}