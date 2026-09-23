using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AugaUnity
{
    public class SkillsPanelController : MonoBehaviour
    {
        public GameObject SkillsContainer;
        public SkillsPanelSkillController SkillPrefab;

        protected readonly Dictionary<Skills.SkillType, SkillsPanelSkillController> _skills = new Dictionary<Skills.SkillType, SkillsPanelSkillController>();
        protected int _skillsCount;
        protected SkillsDialog _skillsDialog;

        // Valheim 1.0 port (#209): green for a bonus, red for a malus (Auga's message-log colours).
        public const string SkillBonusColor = "9DFF5A";
        public const string SkillMalusColor = "CD2121";

        public virtual void Start()
        {
            SkillPrefab.gameObject.SetActive(false);
            _skillsDialog = GetComponent<SkillsDialog>();
            UpdateSkillsDialog();
            InvokeRepeating(nameof(UpdateSkillsDialog), 0f, 1f);
        }

        private void UpdateSkillsDialog()
        {
            if (!isActiveAndEnabled)
                return;

            var player = Player.m_localPlayer;
            if (player != null)
            {
                UpdateSkills(player);
                _skillsDialog.Setup(player);
            }
        }

        public virtual void UpdateSkills(Player player)
        {
            var skills = player.GetSkills();

            foreach (var skillDef in skills.m_skills)
            {
                _skills.TryGetValue(skillDef.m_skill, out var currentSkillElement);

                if (skills.m_skillData.ContainsKey(skillDef.m_skill))
                {
                    if (currentSkillElement == null)
                    {
                        var effect = Instantiate(SkillPrefab, SkillsContainer.transform, false);
                        effect.SkillType = skillDef.m_skill;
                        _skills.Add(skillDef.m_skill, effect);
                    }
                    else
                    {
                        currentSkillElement.SetActive(true);
                        ApplySkillModifier(skills, currentSkillElement);
                    }
                }
                else if (currentSkillElement != null)
                {
                    currentSkillElement.SetActive(true);
                }
            }

            if (_skillsCount != _skills.Count)
            {
                _skillsCount = _skills.Count;
                SortSkillElements();
            }
        }

        // Valheim 1.0 port (#209): vanilla 1.0's skills dialog shows the "+N" that status effects (SEMan.ModifySkillLevel,
        // the hook skill-boosting effects and mods like EpicLoot use) add on top of the raw level. The Auga row prints
        // m_level only and has no bonus element, so append it to the level text coloured by sign.
        protected virtual void ApplySkillModifier(Skills skills, SkillsPanelSkillController element)
        {
            if (element == null || element.LevelText == null || !skills.m_skillData.TryGetValue(element.SkillType, out var skill))
                return;

            // GetSkillLevel is floored, so compare against the floored base as vanilla does; the base is shown floored
            // here too so that base + modifier reads as the effective level.
            var baseLevel = Mathf.FloorToInt(skill.m_level);
            var modifier = Mathf.FloorToInt(skills.GetSkillLevel(element.SkillType)) - baseLevel;
            if (modifier != 0)
            {
                element.LevelText.text = $"$level {baseLevel} <color=#{(modifier > 0 ? SkillBonusColor : SkillMalusColor)}>{modifier:+0;-0}</color>";
            }
        }

        public virtual void SortSkillElements()
        {
            var children = SkillsContainer.transform.Cast<Transform>().Select(x => x.GetComponent<SkillsPanelSkillController>()).ToList();
            children.Sort((a, b) => a.SkillType.CompareTo(b.SkillType));
            for (var i = 0; i < children.Count; ++i)
            {
                children[i].transform.SetSiblingIndex(i);
            }
        }
    }
}
