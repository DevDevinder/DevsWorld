using UnityEngine;

public enum NpcNeedType
{
    Hunger,
    Thirst,
    Energy,
    Warmth,
    Safety,
    Social
}

public class NpcNeeds : MonoBehaviour
{
    [Header("Current Needs")]
    [Range(0, 100)] public float hunger = 100f;
    [Range(0, 100)] public float thirst = 100f;
    [Range(0, 100)] public float energy = 100f;
    [Range(0, 100)] public float warmth = 100f;
    [Range(0, 100)] public float safety = 100f;
    [Range(0, 100)] public float social = 100f;

    [Header("Decay Per Second")]
    public float hungerDecay = 0.35f;
    public float thirstDecay = 0.55f;
    public float energyDecay = 0.18f;
    public float warmthDecay = 0.08f;
    public float safetyDecay = 0.04f;
    public float socialDecay = 0.06f;

    public bool IsDead => hunger <= 0f || thirst <= 0f || energy <= 0f;

    private void Update()
    {
        hunger = Drain(hunger, hungerDecay);
        thirst = Drain(thirst, thirstDecay);
        energy = Drain(energy, energyDecay);
        warmth = Drain(warmth, warmthDecay);
        safety = Drain(safety, safetyDecay);
        social = Drain(social, socialDecay);
    }

    private float Drain(float value, float amountPerSecond)
    {
        return Mathf.Clamp(value - amountPerSecond * Time.deltaTime, 0f, 100f);
    }

    public float GetNeed(NpcNeedType needType)
    {
        return needType switch
        {
            NpcNeedType.Hunger => hunger,
            NpcNeedType.Thirst => thirst,
            NpcNeedType.Energy => energy,
            NpcNeedType.Warmth => warmth,
            NpcNeedType.Safety => safety,
            NpcNeedType.Social => social,
            _ => 100f
        };
    }

    public void RestoreNeed(NpcNeedType needType, float amount)
    {
        switch (needType)
        {
            case NpcNeedType.Hunger:
                hunger = Mathf.Clamp(hunger + amount, 0f, 100f);
                break;

            case NpcNeedType.Thirst:
                thirst = Mathf.Clamp(thirst + amount, 0f, 100f);
                break;

            case NpcNeedType.Energy:
                energy = Mathf.Clamp(energy + amount, 0f, 100f);
                break;

            case NpcNeedType.Warmth:
                warmth = Mathf.Clamp(warmth + amount, 0f, 100f);
                break;

            case NpcNeedType.Safety:
                safety = Mathf.Clamp(safety + amount, 0f, 100f);
                break;

            case NpcNeedType.Social:
                social = Mathf.Clamp(social + amount, 0f, 100f);
                break;
        }
    }

    public void DamageNeed(NpcNeedType needType, float amount)
    {
        RestoreNeed(needType, -amount);
    }
}