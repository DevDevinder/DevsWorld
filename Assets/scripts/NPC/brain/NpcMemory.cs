using System.Collections.Generic;
using UnityEngine;

public class NpcMemory : MonoBehaviour
{
    public List<Memory> memories = new();

    [Header("Memory Settings")]
    public float forgettingRate = 2f;

    private void Update()
    {
        ForgetOverTime();
    }

    void ForgetOverTime()
    {
        for (int i = memories.Count - 1; i >= 0; i--)
        {
            memories[i].confidence -= forgettingRate * Time.deltaTime;

            if (memories[i].confidence <= 0)
                memories.RemoveAt(i);
        }
    }

    public void Remember(MemoryType type, Transform target)
    {
        foreach (var memory in memories)
        {
            if (memory.target == target)
            {
                memory.Refresh();
                return;
            }
        }

        memories.Add(new Memory(type, target));

        Debug.Log($"{name} remembered a {type}");
    }

    public Memory GetClosestMemory(MemoryType type)
    {
        Memory best = null;
        float bestDistance = float.MaxValue;

        foreach (var memory in memories)
        {
            if (memory.type != type)
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
        foreach (var memory in memories)
        {
            if (memory.type == type)
                return true;
        }

        return false;
    }
}