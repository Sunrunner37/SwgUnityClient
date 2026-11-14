using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Characters.Skills;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Containers;
using Swg.Client.Runtime.UI.Theming;
using TMPro;
using Swg.Client.Runtime.Assets.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Skills
{
    public sealed class SkillsWindow : MonoBehaviour, IUiPrefabSource
    {
        [Serializable]
        public sealed class SkillBox
        {
            public Button button;
            public TMP_Text label;
            public Image progress;
            public Image progressBackground;
            [NonSerialized] public SkillCatalog.Skill skill;
        }
        public UiTheme theme;
        public Toggle characterTab;
        public Toggle allTab;
        public TMP_Text listTitle;
        public RectTransform professionList;
        public Button professionPrefab;
        public GameObject dividerPrefab;
        public GameObject characterLists;
        public TMP_Text experience;
        public TMP_Text modifiers;
        public TMP_Text certifications;
        public TMP_Text professionTitle;
        public SkillBox master;
        public SkillBox novice;
        public SkillBox[] branches;
        public UiTextLinks[] destinations;
        public TMP_Text selectedTitle;
        public RectTransform grantedModifiers;
        public RectTransform grantedUnlocks;
        public TMP_Text points;
        public TMP_Text requirements;
        public TMP_Text skillPointRequirement;
        public Button surrenderButton;
        public CapacityBar experienceBar;
        public SkillsDetailRow detailRowPrefab;
        public UiTooltip tooltip;
        public CapacityBar pointsBar;
        public bool Ready { get; private set; }
        public IEnumerable<GameObject> UiPrefabs => new[] { professionPrefab.gameObject, dividerPrefab, detailRowPrefab.gameObject };
        WorldState state;
        ClientSkills skills;
        CommandVisuals visuals;
        SkillCatalog.Skill profession;
        SkillCatalog.Skill selected;
        SkillCatalog.Skill surrenderTarget;
        readonly List<GameObject> rows = new List<GameObject>();
        readonly List<(SkillCatalog.Skill Skill, TMP_Text Label, Button Button)> professionLabels = new List<(SkillCatalog.Skill, TMP_Text, Button)>();
        bool allProfessions;
        bool dirty;
        Coroutine description;

        void Awake()
        {
            state = GameServices.Resolve<WorldState>();
            skills = GameServices.Resolve<ClientSkills>();
            visuals = GameServices.Resolve<CommandVisuals>();
            foreach (var link in destinations) link.Clicked += SelectDestination;
            foreach (var box in Boxes())
            {
                var captured = box;
                box.button.onClick.AddListener(() => SelectSkill(captured.skill));
                box.button.GetComponent<SkillBoxHover>().Entered += () => PreviewSurrender(captured.skill);
            }
        }
        IEnumerable<SkillBox> Boxes() => new[] { master, novice }.Concat(branches);
        void OnEnable()
        {
            state.SceneObjects.Subscribe(state.PlayerSwgId, PlayerChanged);
            state.ExperienceChanged += MarkDirty;
            state.Commands.Changed += MarkDirty;
            theme.Changed += MarkDirty;
            characterTab.onValueChanged.AddListener(CharacterTab);
            allTab.onValueChanged.AddListener(AllTab);
            surrenderButton.onClick.AddListener(Surrender);
            StartCoroutine(Prepare());
        }
        IEnumerator Prepare()
        {
            yield return new WaitUntil(() => skills.Loading.IsCompleted && visuals.Loading.IsCompleted);
            if (visuals.Loading.IsFaulted) { Debug.LogException(visuals.Loading.Exception.GetBaseException(), this); yield break; }
            if (skills.Loading.IsFaulted) { Debug.LogException(skills.Loading.Exception.GetBaseException(), this); yield break; }
            Ready = true;
            SelectTab(allProfessions);
            RefreshSelectedSkill();
        }
        void OnDisable()
        {
            state.SceneObjects.Unsubscribe(state.PlayerSwgId, PlayerChanged);
            state.ExperienceChanged -= MarkDirty;
            state.Commands.Changed -= MarkDirty;
            theme.Changed -= MarkDirty;
            characterTab.onValueChanged.RemoveListener(CharacterTab);
            allTab.onValueChanged.RemoveListener(AllTab);
            surrenderButton.onClick.RemoveListener(Surrender);
            surrenderTarget = null;
            tooltip.Hide();
            StopAllCoroutines();
            description = null;
            Ready = false;
        }
        void PlayerChanged(SceneObjectChange changes) { if ((changes & (SceneObjectChange.Skills | SceneObjectChange.Replaced)) != 0) MarkDirty(); }
        void MarkDirty() => dirty = true;
        void CharacterTab(bool value) { if (value) SelectTab(false); }
        void AllTab(bool value) { if (value) SelectTab(true); }
        public void SelectTab(bool all)
        {
            allProfessions = all;
            characterTab.SetIsOnWithoutNotify(!all);
            allTab.SetIsOnWithoutNotify(all);
            characterLists.SetActive(!all);
            listTitle.text = all ? "All Professions" : "My Professions";
            if (!Ready) return;
            RebuildProfessions();
            MarkDirty();
        }
        void LateUpdate()
        {
            if (!Ready || !dirty) return;
            dirty = false;
            var visible = skills.Catalog.Professions.Where(item => allProfessions || skills.Owns(item)).ToArray();
            if (!professionLabels.Select(row => row.Skill).SequenceEqual(visible)) RebuildProfessions();
            foreach (var row in professionLabels)
            {
                bool selectedProfession = row.Skill == profession;
                row.Label.text = (skills.Owns(row.Skill) ? ">>  " : "    ") + row.Skill.Label;
                var colors = row.Button.colors;
                colors.normalColor = selectedProfession ? theme.accent : Color.clear;
                colors.selectedColor = colors.normalColor;
                colors.highlightedColor = theme.accent;
                row.Button.colors = colors;
                row.Label.color = selectedProfession ? theme.onAccent : theme.bodyText;
            }
            experience.text = string.Join("\n", state.Experience.OrderBy(pair => skills.Catalog.Label("exp_n", pair.Key)).Select(pair => Line(skills.Catalog.Label("exp_n", pair.Key), pair.Value)));
            modifiers.text = skills.Player == null ? "" : string.Join("\n", skills.Player.SkillModifiers.Where(pair => pair.Value != 0 && !pair.Key.StartsWith("private_", StringComparison.Ordinal)).OrderBy(pair => skills.Catalog.Label("stat_n", pair.Key)).Select(pair => Line(skills.Catalog.Label("stat_n", pair.Key), pair.Value)));
            certifications.text = string.Join("\n", skills.Certifications);
            foreach (var box in Boxes()) Refresh(box);
            points.text = "Skill Points Available:             " + skills.AvailablePoints + " / " + SkillCatalog.MaximumSkillPoints;
            pointsBar.Fill = (float)skills.AvailablePoints / SkillCatalog.MaximumSkillPoints;
            RefreshRequirements();
        }
        static string Line(string label, int value) => label + "<pos=78%>" + (value >= 0 ? "+" : "") + value;
        void RebuildProfessions()
        {
            foreach (var row in rows) Destroy(row);
            rows.Clear();
            professionLabels.Clear();
            bool hadBasic = false;
            bool divided = false;
            foreach (var item in skills.Catalog.Professions.Where(item => allProfessions || skills.Owns(item)))
            {
                bool basic = skills.Catalog.IsBasic(item);
                if (hadBasic && !basic && !divided)
                {
                    rows.Add(Instantiate(dividerPrefab, professionList));
                    divided = true;
                }
                hadBasic |= basic;
                var row = Instantiate(professionPrefab, professionList);
                row.name = item.Name;
                row.onClick.AddListener(() => SelectProfession(item));
                rows.Add(row.gameObject);
                professionLabels.Add((item, row.GetComponentInChildren<TMP_Text>(), row));
            }
            if (profession == null || !professionLabels.Any(row => row.Skill == profession))
                SelectProfession(professionLabels.FirstOrDefault().Skill);
        }
        public void SelectProfession(SkillCatalog.Skill value)
        {
            profession = value;
            selected = null;
            surrenderTarget = null;
            if (description != null) StopCoroutine(description);
            description = null;
            selectedTitle.text = "";
            Clear(grantedModifiers);
            Clear(grantedUnlocks);
            professionTitle.text = value?.Label.ToUpperInvariant() ?? "No professions learned";
            var tree = value == null ? null : skills.Catalog.Tree(value);
            master.skill = tree?.Master;
            novice.skill = tree?.Novice;
            for (int column = 0; column < destinations.Length; column++)
            {
                for (int tier = 0; tier < 4; tier++)
                    branches[column * 4 + tier].skill = tree?.Branches[column][tier];
                destinations[column].SetText(tree == null ? "" : string.Join("\n", tree.Destinations[column].Select(item => "<link=\"" + item.Name + "\">To: " + item.Label + "</link>")));
            }
            MarkDirty();
        }
        void Refresh(SkillBox box)
        {
            box.button.gameObject.SetActive(box.skill != null);
            if (box.skill == null) return;
            bool learned = skills.Learned(box.skill);
            box.label.text = box.skill.Label.Replace(": ", "\n");
            box.label.color = learned ? theme.onAccent : theme.bodyText;
            var colors = box.button.colors;
            Color normal = learned ? theme.learnedSkill : theme.skill;
            if (box.skill == selected) normal = Color.Lerp(normal, theme.accent, .35f);
            box.button.targetGraphic.color = normal;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, theme.accent, .3f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(Color.white, theme.accent, .55f);
            box.button.colors = colors;
            bool showProgress = skills.IsNext(box.skill) && box.skill.ExperienceCost > 0;
            box.progressBackground.gameObject.SetActive(showProgress);
            box.progress.gameObject.SetActive(showProgress);
            box.progress.fillAmount = skills.Progress(box.skill);
            box.progress.color = ExperienceColor(box.skill);
        }
        public void SelectSkill(SkillCatalog.Skill value)
        {
            if (value == null) return;
            selected = value;
            RefreshSelectedSkill();
            MarkDirty();
        }
        void RefreshSelectedSkill()
        {
            if (selected == null) return;
            var value = selected;
            PreviewSurrender(value);
            selectedTitle.text = value.Label;
            Clear(grantedModifiers);
            foreach (var pair in value.Modifiers.Where(pair => !pair.Key.StartsWith("private_", StringComparison.Ordinal)).OrderBy(pair => skills.Catalog.Label("stat_n", pair.Key)))
                Instantiate(detailRowPrefab, grantedModifiers).Bind(skills.Catalog.Label("stat_n", pair.Key), (pair.Value >= 0 ? "+" : "") + pair.Value, null, null, tooltip);
            Clear(grantedUnlocks);
            if (description != null) StopCoroutine(description);
            description = StartCoroutine(Describe(value));
        }
        IEnumerator Describe(SkillCatalog.Skill value)
        {
            var load = skills.Unlocks(value);
            yield return new WaitUntil(() => load.IsCompleted);
            if (load.IsFaulted) Debug.LogException(load.Exception.GetBaseException(), this);
            else foreach (var item in load.Result)
                Instantiate(detailRowPrefab, grantedUnlocks).Bind(item.Label, "", visuals.GetIcon(item.Command), item.Description, tooltip);
            description = null;
        }
        void SelectDestination(string name)
        {
            var destination = skills.Catalog.Skills[name];
            if (!skills.Owns(destination)) SelectTab(true);
            SelectProfession(destination);
        }
        static void Clear(Transform content)
        {
            foreach (Transform row in content) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
        }
        void PreviewSurrender(SkillCatalog.Skill skill)
        {
            // Keep the hovered skill available while the pointer moves to the footer button.
            surrenderTarget = skill != null && skills.Learned(skill) ? skill : null;
            MarkDirty();
        }
        void Surrender()
        {
            if (surrenderTarget != null) skills.Surrender(surrenderTarget);
        }
        Color ExperienceColor(SkillCatalog.Skill skill) => skills.Progress(skill) >= 1 ? theme.capacityFill : theme.skillExperience;
        void RefreshRequirements()
        {
            if (surrenderTarget != null && !skills.Learned(surrenderTarget)) surrenderTarget = null;
            surrenderButton.gameObject.SetActive(surrenderTarget != null);
            skillPointRequirement.horizontalAlignment = surrenderTarget != null ? HorizontalAlignmentOptions.Center : HorizontalAlignmentOptions.Left;
            skillPointRequirement.text = surrenderTarget != null ? "Surrender Skill to recover " + surrenderTarget.Points + " skill points."
                : selected == null ? "" : "This skill requires " + selected.Points + " skill points to learn.";
            requirements.text = selected == null || selected.ExperienceCost <= 0 ? "" : "This skill requires " + selected.ExperienceCost + " points of " + skills.Catalog.Label("exp_n", selected.ExperienceType) + ". You currently have " + skills.Experience(selected) + ".";
            bool show = selected != null && selected.ExperienceCost > 0 && skills.IsNext(selected);
            experienceBar.gameObject.SetActive(show);
            if (show) { experienceBar.Fill = skills.Progress(selected); experienceBar.color = ExperienceColor(selected); }
        }
        public void ToggleOpen() { gameObject.SetActive(!gameObject.activeSelf); if (gameObject.activeSelf) transform.SetAsLastSibling(); }
        public void Close() => gameObject.SetActive(false);
    }
}
