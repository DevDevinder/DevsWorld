using UnityEngine;

[System.Serializable]
public class NeedSourceEffect
{
    public NpcNeedType needType;

    [Header("Restore")]
    public float restorePerSecond = 25f;

    [Header("Source Amount")]
    public bool infinite = false;
    public float availableAmount = 100f;
    public float maxAmount = 100f;

    public bool HasAmount => infinite || availableAmount > 0f;

    public float Consume(float deltaTime)
    {
        if (!HasAmount)
            return 0f;

        float requested = restorePerSecond * deltaTime;

        if (infinite)
            return requested;

        float consumed = Mathf.Min(availableAmount, requested);
        availableAmount -= consumed;

        return consumed;
    }

    public void Regrow(float amount)
    {
        if (infinite)
            return;

        availableAmount = Mathf.Clamp(availableAmount + amount, 0f, maxAmount);
    }
}