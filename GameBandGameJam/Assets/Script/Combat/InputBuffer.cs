#nullable enable
using System.Collections.Generic;

public sealed class InputBuffer
{
    readonly List<AttackInputType> sequence = new();
    float comboResetWindow;
    float lastInputTime = -999f;
    bool isOpen = true;

    public IReadOnlyList<AttackInputType> Sequence => sequence;
    public int Count => sequence.Count;
    public bool IsOpen => isOpen;
    public float LastInputTime => lastInputTime;

    public void Initialize(float resetWindow)
    {
        comboResetWindow = resetWindow;
        Clear();
        isOpen = true;
    }

    public void SetResetWindow(float resetWindow)
    {
        comboResetWindow = resetWindow;
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
    }

    public void Append(AttackInputType input, float time)
    {
        sequence.Add(input);
        lastInputTime = time;
    }

    public void ReplaceWith(AttackInputType input, float time)
    {
        sequence.Clear();
        sequence.Add(input);
        lastInputTime = time;
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

    /// <summary>
    /// Refreshes the continuation timer without changing the sequence
    /// (e.g. when the cancel window opens after startup/active frames).
    /// </summary>
    public void Touch(float time)
    {
        if (sequence.Count > 0)
        {
            lastInputTime = time;
        }
    }
}
