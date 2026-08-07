#nullable enable
using System.Collections.Generic;

public sealed class InputBuffer
{
    readonly List<AttackInputType> sequence = new();
    float comboResetWindow;
    float lastInputTime = -999f;
    bool isOpen = true;

    public IReadOnlyList<AttackInputType> Sequence => sequence;
    public bool IsOpen => isOpen;

    public void Initialize(float resetWindow)
    {
        comboResetWindow = resetWindow;
        Clear();
        isOpen = true;
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (!open)
        {
            Clear();
        }
    }

    public bool TryRegister(AttackInputType input, float time)
    {
        if (!isOpen)
        {
            return false;
        }

        if (sequence.Count > 0 && time - lastInputTime > comboResetWindow)
        {
            sequence.Clear();
        }

        sequence.Add(input);
        lastInputTime = time;
        return true;
    }

    public bool HasTimedOut(float time)
    {
        return sequence.Count > 0 && time - lastInputTime > comboResetWindow;
    }

    public void Clear()
    {
        sequence.Clear();
        lastInputTime = -999f;
    }
}
