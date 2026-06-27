using System.Collections.Generic;
using UnityEngine;

public class NpcMemory : MonoBehaviour
{
    [Header("Memories")]
    public List<Memory> memories = new();

    [Header("Confidence Decay")]
    public float entityConfidenceDecayPerSecond = 0.2f;
    public float areaConfidenceDecayPerSecond = 0.05f;
    public float minimumStaleConfidence = 25f;

    [Header("Removal")]
    public float zeroConfidenceForgetDelay = 120f;
    public bool onlyForgetZeroConfidenceIfAlternativeExists = true;

    [Header("Search Behaviour")]
    public float failedSearchCooldown = 20f;
    public float searchFailureConfidenceLoss = 35f;
    public float areaMergeDistance = 10f;

    private void Update()
    {
        UpdateMemories();
    }

    private void UpdateMemories()
    {
        for (int i = memories.Count - 1; i >= 0; i--)
        {
            Memory memory = memories[i];

            if (!memory.confirmedMissing)
            {
                float decayRate = memory.category == MemoryCategory.Entity
                    ? entityConfidenceDecayPerSecond
                    : areaConfidenceDecayPerSecond;

                memory.DecayConfidence(decayRate * Time.deltaTime, minimumStaleConfidence);
            }

            if (ShouldRemoveMemory(memory))
                memories.RemoveAt(i);
        }
    }

    private bool ShouldRemoveMemory(Memory memory)
    {
        if (!memory.IsZeroConfidenceExpired(zeroConfidenceForgetDelay))
            return false;

        if (!onlyForgetZeroConfidenceIfAlternativeExists)
            return true;

        return HasAlternativeMemory(memory);
    }

    private bool HasAlternativeMemory(Memory oldMemory)
    {
        foreach (Memory memory in memories)
        {
            if (memory == oldMemory)
                continue;

            if (memory.type != oldMemory.type)
                continue;

            if (memory.confirmedMissing || memory.confidence <= 0f)
                continue;

            return true;
        }

        return false;
    }

    public void Remember(MemoryType type, Transform target)
    {
        if (target == null)
            return;

        RememberEntity(type, target);
        RememberArea(type, target.position);
    }

    public void RememberEntity(MemoryType type, Transform target)
    {
        Memory existing = FindEntityMemory(target);

        if (existing != null)
        {
            existing.Refresh(target.position);
            return;
        }

        memories.Add(new Memory(
            type,
            MemoryCategory.Entity,
            target,
            target.position
        ));

        Debug.Log($"{name} remembered entity: {type}");
    }

    public void RememberArea(MemoryType type, Vector3 position)
    {
        Memory existing = FindNearbyAreaMemory(type, position);

        if (existing != null)
        {
            existing.Refresh(position);
            return;
        }

        memories.Add(new Memory(
            type,
            MemoryCategory.Area,
            null,
            position
        ));

        Debug.Log($"{name} remembered area: {type}");
    }

    public Memory GetBestMemoryForNeed(MemoryType type, float needUrgency)
    {
        Memory best = null;
        float bestScore = float.MinValue;

        foreach (Memory memory in memories)
        {
            if (memory.type != type)
                continue;

            if (memory.confirmedMissing)
                continue;

            float score = memory.GetDecisionScore(transform.position, needUrgency);

            if (score > bestScore)
            {
                bestScore = score;
                best = memory;
            }
        }

        return best;
    }

    public Memory GetClosestMemory(MemoryType type)
    {
        Memory best = null;
        float bestDistance = float.MaxValue;

        foreach (Memory memory in memories)
        {
            if (memory.type != type)
                continue;

            if (memory.confirmedMissing)
                continue;

            float distance = Vector3.Distance(transform.position, memory.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = memory;
            }
        }

        return best;
    }

    public bool Knows(MemoryType type)
    {
        foreach (Memory memory in memories)
        {
            if (memory.type == type && !memory.confirmedMissing && memory.confidence > 0f)
                return true;
        }

        return false;
    }

    public void MarkMemorySearchSuccess(Memory memory)
    {
        if (memory == null)
            return;

        memory.MarkSearchSuccess();
    }

    public void MarkMemorySearchFailure(Memory memory)
    {
        if (memory == null)
            return;

        memory.MarkSearchFailure(
            failedSearchCooldown,
            searchFailureConfidenceLoss
        );
    }

    public void MarkMemoryConfirmedMissing(Memory memory)
    {
        if (memory == null)
            return;

        memory.MarkConfirmedMissing();
    }

    private Memory FindEntityMemory(Transform target)
    {
        foreach (Memory memory in memories)
        {
            if (memory.category == MemoryCategory.Entity && memory.target == target)
                return memory;
        }

        return null;
    }

    private Memory FindNearbyAreaMemory(MemoryType type, Vector3 position)
    {
        foreach (Memory memory in memories)
        {
            if (memory.category != MemoryCategory.Area)
                continue;

            if (memory.type != type)
                continue;

            float distance = Vector3.Distance(memory.position, position);

            if (distance <= areaMergeDistance)
                return memory;
        }

        return null;
    }
}