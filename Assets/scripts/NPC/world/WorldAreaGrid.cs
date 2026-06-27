using UnityEngine;

public class WorldAreaGrid : MonoBehaviour
{
    public static WorldAreaGrid Instance { get; private set; }

    [Header("Knowledge Regions")]
    public float regionSize = 24f;
    public Vector3 origin;

    private void Awake()
    {
        Instance = this;
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - origin;

        return new Vector2Int(
            Mathf.FloorToInt(local.x / regionSize),
            Mathf.FloorToInt(local.z / regionSize)
        );
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        return origin + new Vector3(
            cell.x * regionSize + regionSize * 0.5f,
            0f,
            cell.y * regionSize + regionSize * 0.5f
        );
    }
}