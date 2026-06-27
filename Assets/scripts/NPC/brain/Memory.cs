using UnityEngine;

[System.Serializable]
public class Memory
{
    public MemoryType type;
    public MemoryCategory category;

    public Transform target;
    public Vector3 position;

    [Range(0, 100)] public float confidence = 100f;
    [Range(0, 100)] public float usefulness = 50f;

    public float lastSeenTime;
    public float lastCheckedTime;
    public float lastUsefulTime;
    public float nextSearchAllowedTime;

    public float zeroConfidenceSinceTime = -1f;

    public bool confirmedMissing;

    public bool HasTarget => target != null;
    public bool IsOnCooldown => Time.time < nextSearchAllowedTime;
    public bool IsZeroConfidence => confidence <= 0f;

    public Memory(MemoryType type, MemoryCategory category, Transform target, Vector3 position)
    {
        this.type = type;
        this.category = category;
        this.target = target;
        this.position = position;

        confidence = 100f;
        usefulness = 50f;

        lastSeenTime = Time.time;
        lastCheckedTime = -999f;
        lastUsefulTime = Time.time;
        nextSearchAllowedTime = 0f;

        zeroConfidenceSinceTime = -1f;
        confirmedMissing = false;
    }

    public void Refresh(Vector3 newPosition)
    {
        position = newPosition;
        confidence = 100f;
        usefulness = Mathf.Clamp(usefulness + 10f, 0f, 100f);

        lastSeenTime = Time.time;
        lastUsefulTime = Time.time;

        zeroConfidenceSinceTime = -1f;
        confirmedMissing = false;
    }

    public void MarkSearchSuccess()
    {
        confidence = 100f;
        usefulness = Mathf.Clamp(usefulness + 15f, 0f, 100f);

        lastCheckedTime = Time.time;
        lastUsefulTime = Time.time;
        nextSearchAllowedTime = Time.time;

        zeroConfidenceSinceTime = -1f;
        confirmedMissing = false;
    }

    public void MarkSearchFailure(float cooldownSeconds, float confidenceLoss)
    {
        confidence = Mathf.Clamp(confidence - confidenceLoss, 0f, 100f);
        usefulness = Mathf.Clamp(usefulness - confidenceLoss * 0.4f, 0f, 100f);

        lastCheckedTime = Time.time;
        nextSearchAllowedTime = Time.time + cooldownSeconds;

        if (confidence <= 0f && zeroConfidenceSinceTime < 0f)
            zeroConfidenceSinceTime = Time.time;
    }

    public void MarkConfirmedMissing()
    {
        confidence = 0f;
        usefulness = 0f;
        confirmedMissing = true;
        lastCheckedTime = Time.time;

        if (zeroConfidenceSinceTime < 0f)
            zeroConfidenceSinceTime = Time.time;
    }

    public void DecayConfidence(float decayAmount, float minimumConfidence)
    {
        if (confirmedMissing)
            return;

        confidence = Mathf.Max(
            minimumConfidence,
            confidence - decayAmount
        );
    }

    public bool IsZeroConfidenceExpired(float delaySeconds)
    {
        if (zeroConfidenceSinceTime < 0f)
            return false;

        return Time.time - zeroConfidenceSinceTime >= delaySeconds;
    }

    public float GetDecisionScore(Vector3 npcPosition, float needUrgency)
    {
        if (confirmedMissing)
            return -999f;

        float distance = Vector3.Distance(npcPosition, position);
        float distancePenalty = distance * 1.5f;
        float cooldownPenalty = IsOnCooldown ? 80f : 0f;

        return needUrgency + confidence + usefulness - distancePenalty - cooldownPenalty;
    }
}