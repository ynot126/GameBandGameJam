#nullable enable
using System;
using System.Collections.Generic;

public sealed class ComboEvaluator
{
    (ComboType attackId, ComboData combo)[] combos = Array.Empty<(ComboType, ComboData)>();

    public void Initialize(EnumDictionary<ComboType, ComboData> comboData)
    {
        if (comboData == null || comboData.Count == 0)
        {
            combos = Array.Empty<(ComboType, ComboData)>();
            return;
        }

        var authored = new List<(ComboType attackId, ComboData combo)>(comboData.Count);
        foreach (var pair in comboData)
        {
            if (pair.Key == ComboType.None)
            {
                continue;
            }

            var data = pair.Value;
            if (data?.sequence == null || data.sequence.Length == 0)
            {
                continue;
            }

            authored.Add((pair.Key, data));
        }

        combos = authored.ToArray();
        Array.Sort(combos, (a, b) => b.combo.sequence.Length.CompareTo(a.combo.sequence.Length));
    }

    /// <summary>
    /// Resolves an exact recipe match. When <paramref name="forceCommit"/> is false and the
    /// buffer is also a prefix of a longer recipe, returns false so the caller can wait.
    /// Combo cancel-chains should pass <paramref name="forceCommit"/> true so each step fires.
    /// </summary>
    public bool TryResolve(IReadOnlyList<AttackInputType> buffer, bool forceCommit, out ComboType comboType)
    {
        comboType = ComboType.None;
        if (buffer.Count == 0 || combos.Length == 0)
        {
            return false;
        }

        ComboType? exactMatch = null;
        var isPrefixOfLonger = false;

        for (var i = 0; i < combos.Length; i++)
        {
            var entry = combos[i];
            var sequence = entry.combo.sequence;

            if (IsExactMatch(buffer, sequence))
            {
                exactMatch ??= entry.attackId;
                continue;
            }

            if (IsPrefix(buffer, sequence))
            {
                isPrefixOfLonger = true;
            }
        }

        if (exactMatch == null)
        {
            return false;
        }

        if (!forceCommit && isPrefixOfLonger)
        {
            return false;
        }

        comboType = exactMatch.Value;
        return comboType != ComboType.None;
    }

    static bool IsExactMatch(IReadOnlyList<AttackInputType> buffer, AttackInputType[] recipe)
    {
        if (buffer.Count != recipe.Length)
        {
            return false;
        }

        for (var i = 0; i < recipe.Length; i++)
        {
            if (buffer[i] != recipe[i])
            {
                return false;
            }
        }

        return true;
    }

    static bool IsPrefix(IReadOnlyList<AttackInputType> buffer, AttackInputType[] recipe)
    {
        if (buffer.Count >= recipe.Length)
        {
            return false;
        }

        for (var i = 0; i < buffer.Count; i++)
        {
            if (buffer[i] != recipe[i])
            {
                return false;
            }
        }

        return true;
    }
}
