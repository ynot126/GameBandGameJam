#nullable enable
using System;
using System.Collections.Generic;

public sealed class ComboEvaluator
{
    ComboRecipe[] recipes = Array.Empty<ComboRecipe>();

    public void Initialize(ComboRecipe[] comboRecipes)
    {
        if (comboRecipes == null || comboRecipes.Length == 0)
        {
            recipes = Array.Empty<ComboRecipe>();
            return;
        }

        recipes = new ComboRecipe[comboRecipes.Length];
        Array.Copy(comboRecipes, recipes, comboRecipes.Length);
        Array.Sort(recipes, (a, b) => b.sequence.Length.CompareTo(a.sequence.Length));
    }

    /// <summary>
    /// Resolves an exact recipe match. When <paramref name="forceCommit"/> is false and the
    /// buffer is also a prefix of a longer recipe, returns false so the caller can wait.
    /// Combo cancel-chains should pass <paramref name="forceCommit"/> true so each step fires.
    /// </summary>
    public bool TryResolve(IReadOnlyList<AttackInputType> buffer, bool forceCommit, out AttackId attackId)
    {
        attackId = AttackId.None;
        if (buffer.Count == 0 || recipes.Length == 0)
        {
            return false;
        }

        ComboRecipe? exactMatch = null;
        var isPrefixOfLonger = false;

        for (var i = 0; i < recipes.Length; i++)
        {
            var recipe = recipes[i];
            if (recipe.sequence == null || recipe.sequence.Length == 0)
            {
                continue;
            }

            if (IsExactMatch(buffer, recipe.sequence))
            {
                exactMatch ??= recipe;
                continue;
            }

            if (IsPrefix(buffer, recipe.sequence))
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

        attackId = exactMatch.attackId;
        return attackId != AttackId.None;
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
