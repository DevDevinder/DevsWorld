using System.Collections.Generic;
using UnityEngine;

public class NpcExplorationMemory : MonoBehaviour
{
    [Header("Exploration Memory")]
    public float cellSize = 8f;
    public float visitRadius = 2.5f;

    private readonly HashSet<Vector2Int> exploredCells = new();

    private void Update()
    {
        MarkExplored(transform.position);
    }

    public void MarkExplored(Vector3 position)
    {
        exploredCells.Add(WorldToCell(position));
    }

    public bool HasExplored(Vector3 position)
    {
        return exploredCells.Contains(WorldToCell(position));
    }

    public float GetExplorationScore(Vector3 position)
    {
        Vector2Int cell = WorldToCell(position);

        if (!exploredCells.Contains(cell))
            return 100f;

        return 10f;
    }

    private Vector2Int WorldToCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }
}