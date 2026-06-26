using UnityEngine;

[System.Serializable]
public class Memory
{
    public MemoryType type;

    public Transform target;

    public Vector3 position;

    public float confidence = 100f;

    public float lastSeenTime;

    public bool IsValid => target != null;

    public Memory(MemoryType type, Transform target)
    {
        this.type = type;
        this.target = target;
        this.position = target.position;
        this.lastSeenTime = Time.time;
    }

    public void Refresh()
    {
        if (target == null)
            return;

        position = target.position;
        lastSeenTime = Time.time;
        confidence = 100f;
    }
}