using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.Characters.Models;
using Swg.Client.Runtime;
using Swg.Client.Runtime.UI.Screens;

namespace Swg.Client.Runtime.UI.Screens.CharacterList
{
    public sealed class CharacterListScreen : ClientScreen
    {
        public Text account;
        public Text message;
        public Text characterName;
        public Transform listContent;
        public CharacterListRow rowPrefab;
        public Button back;
        public Button create;
        public Button next;
        public Button delete;
        public CharacterPreview preview;
        readonly List<CharacterListRow> rows = new List<CharacterListRow>();
        readonly List<CachedCharacterAppearance> appearances = new List<CachedCharacterAppearance>();
        readonly Dictionary<int, CachedCharacterModelResult> models = new Dictionary<int, CachedCharacterModelResult>();
        int selected = -1;
        int selectionVersion;
        public int CharacterCount => rows.Count;
        public string SelectedCharacter => selected < 0 ? null : rows[selected].Character.Name;

        protected override async Task PrepareAsync()
        {
            var admission = Client.Session.Admission;
            if (admission == null)
                return;
            var settings = Client.Settings;
            string username = Client.Session.Username;
            appearances.AddRange(await Client.Assets.ReadAsync(loader =>
            {
                var result = new List<CachedCharacterAppearance>();
                foreach (var ch in admission.Characters)
                {
                    try
                    {
                        result.Add(CharacterAppearanceCache.LoadOrFallback(loader, settings.GameDirectory, username, admission.Galaxy.Name, ch));
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Character appearance unavailable: " + e.Message);
                        result.Add(null);
                    }
                }

                return result;
            }));
        }

        protected override void OnReady()
        {
            Client.Music.Request(Client.Assets, musicPath, loopMusic);
            preview.Initialize(Client.Assets);
            back.onClick.AddListener(Back);
            create.onClick.AddListener(CreateNew);
            next.interactable = false;
            delete.interactable = false;
            var admission = Client.Session.Admission;
            create.interactable = admission != null && admission.CanCreate;
            account.text = "";
            characterName.text = "";
            if (admission == null)
            {
                message.text = "Log in to view your characters.";
                return;
            }

            foreach (var ch in admission.Characters)
            {
                int index = rows.Count;
                var row = Instantiate(rowPrefab, listContent);
                string galaxy = ch.Galaxy == admission.Galaxy.Id ? admission.Galaxy.Name : "Galaxy " + ch.Galaxy;
                row.Bind(ch, galaxy, () => Select(index));
                row.planet.text = appearances[index] == null ? "" : PlanetName(appearances[index].Planet);
                row.server.text = admission.Galaxy.Status == 2 ? "Online" : "Offline";
                rows.Add(row);
            }

            if (rows.Count > 0)
                Select(0);
            else
                message.text = "No characters on this account. Choose Create to make a character.";
        }

        static string PlanetName(string name) => string.IsNullOrEmpty(name) ? "" : char.ToUpperInvariant(name[0]) + name.Substring(1);
        public async void Select(int index)
        {
            if (index < 0 || index >= rows.Count)
                return;
            selected = index;
            int version = ++selectionVersion;
            for (int i = 0; i < rows.Count; i++)
                rows[i].SetSelected(i == selected);
            characterName.text = rows[index].Character.Name;
            if (appearances[index] == null)
            {
                message.text = "Character appearance unavailable.";
                return;
            }

            try
            {
                if (!models.TryGetValue(index, out var model))
                {
                    var appearance = appearances[index];
                    model = await Client.Assets.ReadAsync(loader => CachedCharacterModel.LoadDetailed(loader, appearance));
                    if (this == null)
                        return;
                    models[index] = model;
                }

                if (this == null || version != selectionVersion)
                    return;
                preview.Show(rows[index].Character.ObjectId.ToString(), model);
                message.text = appearances[index].FromCache ? "Choose the Character you would like to play now.\nYou may also choose to Create a new Character, or Delete one of your existing Characters." : "No native appearance cache: showing this character's default species appearance.";
                foreach (var warning in model.Warnings)
                    Debug.LogWarning(warning);
            }
            catch (Exception e)
            {
                if (this != null && version == selectionVersion)
                {
                    message.text = "Character preview unavailable.";
                    Debug.LogException(e);
                }
            }
        }

        public void CreateNew()
        {
            if (!IsReady || Client.Session.Admission == null || !Client.Session.Admission.CanCreate)
                return;
            ClientNavigation.Open(ScreenId.CharacterCreation);
        }

        public void Back()
        {
            Client.Session.Disconnect();
            ClientNavigation.Open(ScreenId.Login);
        }

        void Update()
        {
            if (!IsReady || ClientNavigation.IsChanging)
                return;
            if (Client.Session.FailureReason != null)
            {
                ClientNavigation.Open(ScreenId.Login);
                return;
            }

            var key = Keyboard.current;
            if (key == null)
                return;
            if (key.escapeKey.wasPressedThisFrame)
                Back();
            else if (key.upArrowKey.wasPressedThisFrame)
                Select(Mathf.Max(0, selected - 1));
            else if (key.downArrowKey.wasPressedThisFrame)
                Select(Mathf.Min(rows.Count - 1, selected + 1));
        }
    }
}
