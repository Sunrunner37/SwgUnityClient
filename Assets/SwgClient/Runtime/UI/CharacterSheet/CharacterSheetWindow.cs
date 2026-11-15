using System;
using System.Collections;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.UI.Containers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.CharacterSheet
{
    public sealed class CharacterSheetWindow : MonoBehaviour
    {
        [Serializable]
        public sealed class AttributeRow
        {
            public TMP_Text value;
            public TMP_Text wounds;
            public TMP_Text modifiers;
            public TMP_Text encumbrance;
            public CapacityBar bar;
            public Image wound;
        }
        public TMP_Text title;
        public Toggle statusTab;
        public Toggle personalTab;
        public GameObject status;
        public GameObject personal;
        public CapacityBar food;
        public CapacityBar drink;
        public TMP_Text fatigue;
        public TMP_Text standing;
        public TMP_Text imperial;
        public TMP_Text rebel;
        public AttributeRow[] attributes;
        public TMP_Text species;
        public TMP_Text home;
        public TMP_Text married;
        public TMP_Text bindLocation;
        public TMP_Text bankLocation;
        public TMP_Text lots;
        public TMP_Text badges;
        public TMP_Text biography;
        public StatMigrationWindow migration;
        public bool Ready { get; private set; }
        WorldState state;
        CurrentPlayer currentPlayer;
        ClientCharacterSheet sheet;
        SceneObjectNameResolver names;
        CharacterSheetChange dirty;
        bool titleDirty;

        void Awake()
        {
            currentPlayer = GameServices.Resolve<CurrentPlayer>();
            state = GameServices.Resolve<WorldState>();
            sheet = GameServices.Resolve<ClientCharacterSheet>();
            names = GameServices.Resolve<SceneObjectNameResolver>();
        }
        void OnEnable()
        {
            currentPlayer.CharacterSheet.Changed += Changed;
            state.SceneObjects.Subscribe(currentPlayer.SwgId, PlayerChanged);
            statusTab.onValueChanged.AddListener(SelectStatus);
            personalTab.onValueChanged.AddListener(SelectPersonal);
            dirty = CharacterSheetChange.All & ~CharacterSheetChange.Migration;
            titleDirty = true;
            sheet.Open();
            SelectStatus(true);
            StartCoroutine(LoadMetadata());
        }
        IEnumerator LoadMetadata()
        {
            yield return new WaitUntil(() => sheet.Loading.IsCompleted);
            if (sheet.Loading.IsFaulted) { Debug.LogException(sheet.Loading.Exception.GetBaseException(), this); yield break; }
            Ready = true;
        }
        void OnDisable()
        {
            currentPlayer.CharacterSheet.Changed -= Changed;
            state.SceneObjects.Unsubscribe(currentPlayer.SwgId, PlayerChanged);
            statusTab.onValueChanged.RemoveListener(SelectStatus);
            personalTab.onValueChanged.RemoveListener(SelectPersonal);
            StopAllCoroutines();
            Ready = false;
        }
        void PlayerChanged(SceneObjectChange changes)
        {
            if ((changes & (SceneObjectChange.Vitals | SceneObjectChange.Replaced)) != 0)
                dirty |= CharacterSheetChange.Status;
            if ((changes & (SceneObjectChange.Appearance | SceneObjectChange.Replaced)) != 0)
            {
                titleDirty = true;
                dirty |= CharacterSheetChange.Personal;
            }
        }
        void Changed(CharacterSheetChange changes) => dirty |= changes & ~CharacterSheetChange.Migration;
        void LateUpdate()
        {
            if (!Ready || sheet.Player == null) return;
            if (titleDirty)
            {
                title.text = names.Resolve(sheet.Player).ToUpperInvariant();
                titleDirty = false;
            }
            if (status.activeSelf && (dirty & CharacterSheetChange.Status) != 0)
            {
                RefreshStatus();
                dirty &= ~CharacterSheetChange.Status;
            }
            if (personal.activeSelf)
            {
                if ((dirty & CharacterSheetChange.Personal) != 0)
                {
                    RefreshPersonal();
                    dirty &= ~CharacterSheetChange.Personal;
                }
                if ((dirty & CharacterSheetChange.Badges) != 0)
                {
                    badges.text = sheet.BadgesText();
                    dirty &= ~CharacterSheetChange.Badges;
                }
            }
        }
        void SelectStatus(bool value) { if (value) SelectTab(false); }
        void SelectPersonal(bool value) { if (value) SelectTab(true); }
        public void SelectTab(bool showPersonal)
        {
            status.SetActive(!showPersonal);
            personal.SetActive(showPersonal);
            statusTab.SetIsOnWithoutNotify(!showPersonal);
            personalTab.SetIsOnWithoutNotify(showPersonal);
        }
        void RefreshStatus()
        {
            var player = sheet.Player;
            var data = sheet.Data;
            food.Fill = data.FoodFullness;
            drink.Fill = data.DrinkFullness;
            fatigue.text = player.BattleFatigue.ToString();
            standing.text = data.Faction == 0 ? "Civilian" : data.FactionStatus switch { 1 => "Combatant", 2 => "Special Forces", _ => "On Leave" };
            imperial.text = data.ImperialPoints.ToString();
            rebel.text = data.RebelPoints.ToString();
            var values = ClientCharacterSheet.Attributes(player.Ham);
            for (int i = 0; i < attributes.Length; i++)
            {
                var row = attributes[i];
                var value = values[i];
                row.value.text = value.Current + " / " + value.AvailableMaximum;
                row.wounds.text = value.Wounds.ToString();
                row.modifiers.text = value.Modifiers.ToString();
                row.encumbrance.text = value.Encumbrance.ToString();
                var rect = (RectTransform)row.bar.transform;
                rect.anchorMax = new Vector2(value.RelativeWidth(player.Ham.LargestMaximum), 1);
                row.bar.Fill = value.CurrentFraction;
                row.wound.rectTransform.anchorMin = new Vector2(1 - value.WoundFraction, 0);
                row.wound.gameObject.SetActive(value.Wounds > 0);
            }
        }
        void RefreshPersonal()
        {
            var data = sheet.Data;
            species.text = sheet.SpeciesName;
            home.text = string.IsNullOrEmpty(data.ResidencePlanet) ? "Wanderer" : sheet.Planet(data.ResidencePlanet);
            married.text = string.IsNullOrEmpty(data.Spouse) ? "Unmarried" : data.Spouse;
            bindLocation.text = sheet.Planet(data.CloningPlanet);
            bankLocation.text = sheet.Planet(data.BankPlanet);
            lots.text = data.LotsRemaining.ToString();
            biography.text = "<noparse>" + data.Biography + "</noparse>";
        }
        public void ToggleOpen() { gameObject.SetActive(!gameObject.activeSelf); if (gameObject.activeSelf) transform.SetAsLastSibling(); }
        public void Close() => gameObject.SetActive(false);
        public void OpenMigration() => migration.Open();
    }
}
