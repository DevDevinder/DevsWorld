using UnityEngine;

public class WorldMemoryTarget : MonoBehaviour
{
    public MemoryType memoryType;

    [Header("Perception")]
    public bool canBeSeen = true;
    public bool canBeHeard = false;
}