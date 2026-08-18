#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TwoHandChooseView : BaseView
{
    [SerializeField] Button leftButton = null!;
    [SerializeField] Button rightButton = null!;
    
    [SerializeField] TextMeshProUGUI leftName = null!;
    [SerializeField] TextMeshProUGUI rightName = null!;
    
    [SerializeField] TextMeshProUGUI leftDescription = null!;
    [SerializeField] TextMeshProUGUI rightDescription = null!;

    BaseSkill leftSkill = null!;
    BaseSkill rightSkill = null!;

    public event Action<BaseSkill>? OnSelect;

    public void Initialize(SkillConfig skillConfig, IReadOnlyList<SkillType> ownedSkills)
    {
        var candidates = CollectCandidates(skillConfig, ownedSkills);
        PickSkills(candidates);

        leftName.text = leftSkill.SkillName;
        leftDescription.text = leftSkill.SkillDescription;
        rightName.text = rightSkill.SkillName;
        rightDescription.text = rightSkill.SkillDescription;

        leftButton.onClick.AddListener(() => OnSelect?.Invoke(leftSkill));
        rightButton.onClick.AddListener(() => OnSelect?.Invoke(rightSkill));
    }

    List<BaseSkill> CollectCandidates(SkillConfig skillConfig, IReadOnlyList<SkillType> ownedSkills)
    {
        var unowned = new List<BaseSkill>();
        var all = new List<BaseSkill>();
        foreach (var pair in skillConfig.skills)
        {
            if (pair.Value == null)
            {
                continue;
            }

            all.Add(pair.Value);
            if (!ownedSkills.Contains(pair.Key))
            {
                unowned.Add(pair.Value);
            }
        }

        return unowned.Count >= 2 ? unowned : all;
    }

    void PickSkills(List<BaseSkill> candidates)
    {
        var leftIndex = UnityEngine.Random.Range(0, candidates.Count);
        leftSkill = candidates[leftIndex];
        candidates.RemoveAt(leftIndex);

        var rightIndex = UnityEngine.Random.Range(0, candidates.Count);
        rightSkill = candidates[rightIndex];
    }
}
