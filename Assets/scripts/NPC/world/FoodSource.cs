using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(WorldMemoryTarget))]
public class FoodSource : MonoBehaviour
{
    [Header("Food")]
    public float foodAmount = 100f;
    public float eatAmountPerSecond = 25f;

    [Header("Regrowth")]
    public bool regrows = true;
    public float regrowPerSecond = 2f;
    public float maxFoodAmount = 100f;

    public bool HasFood => foodAmount > 1f;

    private void Awake()
    {
        WorldMemoryTarget target = GetComponent<WorldMemoryTarget>();

        target.memoryType = MemoryType.Food;
        target.areaKnowledgeType = AreaKnowledgeType.Food;
        target.canBeSeen = true;
        target.canBeHeard = false;
        target.valueAmount = 35f;
        target.attentionAmount = 25f;
    }

    private void Update()
    {
        if (!regrows)
            return;

        foodAmount = Mathf.Clamp(
            foodAmount + regrowPerSecond * Time.deltaTime,
            0f,
            maxFoodAmount
        );
    }

    public float Eat(float requestedAmount)
    {
        float eaten = Mathf.Min(foodAmount, requestedAmount);
        foodAmount -= eaten;
        return eaten;
    }
}