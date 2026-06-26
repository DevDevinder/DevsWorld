using UnityEngine;

[RequireComponent(typeof(NpcMemory))]
public class NpcHearing : MonoBehaviour
{
    [Header("Hearing")]
    public float hearingRadius = 10f;
    public LayerMask audibleLayers;

    [Header("Scanning")]
    public float scanInterval = 0.5f;

    [Header("Debug")]
    public bool drawDebug = true;

    private NpcMemory memory;
    private float scanTimer;

    private void Awake()
    {
        memory = GetComponent<NpcMemory>();
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;

        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            ScanForAudibleObjects();
        }
    }

    private void ScanForAudibleObjects()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            hearingRadius,
            audibleLayers
        );

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform)
                continue;

            WorldMemoryTarget target = hit.GetComponentInParent<WorldMemoryTarget>();

            if (target == null)
                continue;

            if (!target.canBeHeard)
                continue;

            memory.Remember(target.memoryType, target.transform);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }
}