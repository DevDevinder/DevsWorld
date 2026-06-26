using UnityEngine;

[RequireComponent(typeof(NpcMemory))]
public class NpcVision : MonoBehaviour
{
    [Header("Vision")]
    public float visionDistance = 15f;

    [Range(1f, 180f)]
    public float visionAngle = 110f;

    public LayerMask detectableLayers;
    public LayerMask visionBlockingLayers;

    [Header("Scanning")]
    public float scanInterval = 0.25f;

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
            ScanForVisibleObjects();
        }
    }

    private void ScanForVisibleObjects()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            visionDistance,
            detectableLayers
        );

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform)
                continue;

            WorldMemoryTarget target = hit.GetComponentInParent<WorldMemoryTarget>();

            if (target == null)
                continue;

            if (!IsInsideVisionCone(target.transform))
                continue;

            if (!HasLineOfSight(target.transform))
                continue;
if (target.canBeSeen)
    memory.Remember(target.memoryType, target.transform);
        }
    }

    private bool IsInsideVisionCone(Transform target)
    {
        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude <= 0.01f)
            return true;

        float angleToTarget = Vector3.Angle(
            transform.forward,
            directionToTarget.normalized
        );

        return angleToTarget <= visionAngle * 0.5f;
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector3 eyePosition = transform.position + Vector3.up * 1.6f;
        Vector3 targetPosition = target.position + Vector3.up * 0.5f;

        Vector3 directionToTarget = targetPosition - eyePosition;
        float distanceToTarget = directionToTarget.magnitude;

        if (Physics.Raycast(
            eyePosition,
            directionToTarget.normalized,
            out RaycastHit hit,
            distanceToTarget,
            visionBlockingLayers
        ))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
            return;

        Gizmos.color = Color.cyan;

        Vector3 origin = transform.position + Vector3.up * 0.1f;

        Vector3 leftDirection =
            Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;

        Vector3 rightDirection =
            Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;

        Gizmos.DrawRay(origin, leftDirection * visionDistance);
        Gizmos.DrawRay(origin, rightDirection * visionDistance);

        int segments = 24;
        Vector3 previousPoint = origin + leftDirection * visionDistance;

        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(
                -visionAngle * 0.5f,
                visionAngle * 0.5f,
                i / (float)segments
            );

            Vector3 direction =
                Quaternion.Euler(0f, angle, 0f) * transform.forward;

            Vector3 nextPoint = origin + direction * visionDistance;

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}