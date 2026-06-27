using UnityEngine;

[RequireComponent(typeof(NpcNeeds))]
[RequireComponent(typeof(NpcMovement))]
public class SleepAction : NpcAction
{
    public override NpcActionType ActionType => NpcActionType.Sleep;

    [Header("Sleep")]
    public float sleepThreshold = 25f;
    public float wakeThreshold = 90f;
    public float energyRestorePerSecond = 25f;

    private NpcNeeds needs;
    private NpcMovement movement;

    private void Awake()
    {
        needs = GetComponent<NpcNeeds>();
        movement = GetComponent<NpcMovement>();
    }

    public override bool CanRun()
    {
        return needs.energy <= sleepThreshold;
    }

    public override float GetScore()
    {
        if (!CanRun())
            return 0f;

        return 100f - needs.energy + 30f;
    }

    public override void Tick()
    {
        movement.Stop();

        needs.RestoreNeed(
            NpcNeedType.Energy,
            energyRestorePerSecond * Time.deltaTime
        );
    }
}