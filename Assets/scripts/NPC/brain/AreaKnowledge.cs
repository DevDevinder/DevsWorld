using UnityEngine;

[System.Serializable]
public class AreaKnowledge
{
    public Vector2Int cell;
    public Vector3 worldCenter;

    [Header("Knowledge")]
    [Range(0, 100)] public float exploration;
    [Range(0, 100)] public float confidence;

    [Header("Values")]
    [Range(0, 100)] public float foodValue;
    [Range(0, 100)] public float waterValue;
    [Range(0, 100)] public float woodValue;
    [Range(0, 100)] public float shelterValue;
    [Range(0, 100)] public float socialValue;
    [Range(0, 100)] public float dangerValue;

    [Header("Attention")]
    [Range(0, 100)] public float foodAttention;
    [Range(0, 100)] public float waterAttention;
    [Range(0, 100)] public float socialAttention;
    [Range(0, 100)] public float dangerAttention;

    [Header("Timing")]
    public float lastSeenTime;
    public float lastInvestigatedTime;

    [Header("Search History")]
    [Range(0, 100)] public float foodSearchFailure;
    [Range(0, 100)] public float waterSearchFailure;
    [Range(0, 100)] public float generalSearchFailure;

    public AreaKnowledge(Vector2Int cell, Vector3 worldCenter)
    {
        this.cell = cell;
        this.worldCenter = worldCenter;

        confidence = 25f;
        lastSeenTime = Time.time;
        lastInvestigatedTime = -999f;
    }

    public void IncreaseExploration(float amount)
    {
        exploration = Mathf.Clamp(exploration + amount, 0f, 100f);
        confidence = Mathf.Clamp(confidence + amount * 0.4f, 0f, 100f);
        lastSeenTime = Time.time;
    }

    public void AddValue(AreaKnowledgeType type, float amount)
    {
        switch (type)
        {
            case AreaKnowledgeType.Food:
                foodValue = ClampAdd(foodValue, amount);
                foodSearchFailure = Mathf.Max(0f, foodSearchFailure - amount);
                break;

            case AreaKnowledgeType.Water:
                waterValue = ClampAdd(waterValue, amount);
                waterSearchFailure = Mathf.Max(0f, waterSearchFailure - amount);
                break;

            case AreaKnowledgeType.Wood:
                woodValue = ClampAdd(woodValue, amount);
                break;

            case AreaKnowledgeType.Shelter:
                shelterValue = ClampAdd(shelterValue, amount);
                break;

            case AreaKnowledgeType.Social:
                socialValue = ClampAdd(socialValue, amount);
                break;

            case AreaKnowledgeType.Danger:
                dangerValue = ClampAdd(dangerValue, amount);
                break;
        }

        generalSearchFailure = Mathf.Max(0f, generalSearchFailure - amount * 0.5f);
        confidence = Mathf.Clamp(confidence + 8f, 0f, 100f);
        lastSeenTime = Time.time;
    }

    public void AddAttention(AreaKnowledgeType type, float amount)
    {
        switch (type)
        {
            case AreaKnowledgeType.Food:
                foodAttention = ClampAdd(foodAttention, amount);
                break;

            case AreaKnowledgeType.Water:
                waterAttention = ClampAdd(waterAttention, amount);
                break;

            case AreaKnowledgeType.Social:
                socialAttention = ClampAdd(socialAttention, amount);
                break;

            case AreaKnowledgeType.Danger:
                dangerAttention = ClampAdd(dangerAttention, amount);
                break;
        }
    }

    public void SatisfyAttention(AreaKnowledgeType type)
    {
        switch (type)
        {
            case AreaKnowledgeType.Food:
                foodAttention = 0f;
                break;

            case AreaKnowledgeType.Water:
                waterAttention = 0f;
                break;

            case AreaKnowledgeType.Social:
                socialAttention = 0f;
                break;

            case AreaKnowledgeType.Danger:
                dangerAttention = 0f;
                break;
        }

        lastInvestigatedTime = Time.time;
    }

    public void MarkInvestigated()
    {
        lastInvestigatedTime = Time.time;
        generalSearchFailure = Mathf.Clamp(generalSearchFailure + 8f, 0f, 100f);
    }

    public void MarkSearchFailure(AreaKnowledgeType type, float amount)
    {
        switch (type)
        {
            case AreaKnowledgeType.Food:
                foodSearchFailure = Mathf.Clamp(foodSearchFailure + amount, 0f, 100f);
                foodValue = Mathf.Clamp(foodValue - amount * 0.35f, 0f, 100f);
                break;

            case AreaKnowledgeType.Water:
                waterSearchFailure = Mathf.Clamp(waterSearchFailure + amount, 0f, 100f);
                waterValue = Mathf.Clamp(waterValue - amount * 0.35f, 0f, 100f);
                break;
        }

        lastInvestigatedTime = Time.time;
    }

    public void DecayAttention(float amount)
    {
        foodAttention = Mathf.Max(0f, foodAttention - amount);
        waterAttention = Mathf.Max(0f, waterAttention - amount);
        socialAttention = Mathf.Max(0f, socialAttention - amount);
        dangerAttention = Mathf.Max(0f, dangerAttention - amount);
    }

    public void DecaySearchFailures(float amount)
    {
        foodSearchFailure = Mathf.Max(0f, foodSearchFailure - amount);
        waterSearchFailure = Mathf.Max(0f, waterSearchFailure - amount);
        generalSearchFailure = Mathf.Max(0f, generalSearchFailure - amount);
    }

    public float GetValue(AreaKnowledgeType type)
    {
        return type switch
        {
            AreaKnowledgeType.Food => foodValue,
            AreaKnowledgeType.Water => waterValue,
            AreaKnowledgeType.Wood => woodValue,
            AreaKnowledgeType.Shelter => shelterValue,
            AreaKnowledgeType.Social => socialValue,
            AreaKnowledgeType.Danger => dangerValue,
            _ => 0f
        };
    }

    public float GetFailure(AreaKnowledgeType type)
    {
        return type switch
        {
            AreaKnowledgeType.Food => foodSearchFailure,
            AreaKnowledgeType.Water => waterSearchFailure,
            _ => generalSearchFailure
        };
    }

    public float GetAttentionTotal()
    {
        return foodAttention + waterAttention + socialAttention + dangerAttention;
    }

    private float ClampAdd(float value, float amount)
    {
        return Mathf.Clamp(value + amount, 0f, 100f);
    }
}