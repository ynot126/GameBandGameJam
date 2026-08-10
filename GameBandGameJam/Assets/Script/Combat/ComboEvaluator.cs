#nullable enable
using System;
using System.Collections.Generic;

public sealed class ComboEvaluator
{
    (ComboType attackId, ComboRecipe recipe)[] recipes = Array.Empty<(ComboType, ComboRecipe)>();

    public void Initialize(EnumDictionary<ComboType, ComboRecipe> comboRecipes)
    {
        if (comboRecipes == null || comboRecipes.Count == 0)
        {
            recipes = Array.Empty<(ComboType, ComboRecipe)>();
            return;
        }

        var authored = new List<(ComboType attackId, ComboRecipe recipe)>(comboRecipes.Count);
        foreach (var pair in comboRecipes)
        {
            if (pair.Key == ComboType.None)
            {
                continue;
            }

            var recipe = pair.Value;
            if (recipe?.sequence == null || recipe.sequence.Length == 0)
            {
                continue;
            }

            authored.Add((pair.Key, recipe));
        }

        recipes = authored.ToArray();
        Array.Sort(recipes, (a, b) => b.recipe.sequence.Length.CompareTo(a.recipe.sequence.Length));
    }

    /// <summary>
    /// Resolves an exact recipe match. When <paramref name="forceCommit"/> is false and the
    /// buffer is also a prefix of a longer recipe, returns false so the caller can wait.
    /// Combo cancel-chains should pass <paramref name="forceCommit"/> true so each step fires.
    /// </summary>
    public bool TryResolve(IReadOnlyList<AttackInputType> buffer, bool forceCommit, out ComboType comboType)
    {
        comboType = ComboType.None;
        if (buffer.Count == 0 || recipes.Length == 0)
        {
            return false;
        }

        ComboType? exactMatch = null;
        var isPrefixOfLonger = false;

        for (var i = 0; i < recipes.Length; i++)
        {
            var entry = recipes[i];
            var sequence = entry.recipe.sequence;

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
