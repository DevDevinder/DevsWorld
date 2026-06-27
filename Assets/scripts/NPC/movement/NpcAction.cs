using UnityEngine;

public abstract class NpcAction : MonoBehaviour
{
    public abstract NpcActionType ActionType { get; }

    public virtual bool CanRun()
    {
        return true;
    }

    public abstract float GetScore();

    public virtual void Begin() { }

    public virtual void Tick() { }

    public virtual void End() { }
}