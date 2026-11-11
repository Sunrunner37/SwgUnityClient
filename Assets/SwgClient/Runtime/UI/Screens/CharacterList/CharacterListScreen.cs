using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.Assets.Tre;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.Characters.Models;

namespace Swg.Client.Runtime.UI.Screens.CharacterList
{
    public sealed class CharacterListScreen : ClientScreen
    {
        ClientConnection connection;
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

        public CanvasGroup content;
        void Awake()
        {
            content.alpha = 0;
            content.interactable = false;
            content.blocksRaycasts = false;
            preview.previewCamera.enabled = false;
        }

        TreRuntimeAssets assets;
        IEnumerator Start()
        {
            connection = GameServices.Resolve<ClientConnection>();
            assets = GameServices.Resolve<TreRuntimeAssets>();
            next.interactable = false;
            create.interactable = false;
            delete.interactable = false;
            Debug.Log("CharacterListScreen: Loading characters", this);
            var admission = connection.Admission;
            if (admission == null)
            {
                yield return new WaitUntil(() => assets.Ready.IsCompleted);
                if (assets.Ready.IsFaulted)
                {
                    Debug.LogException(assets.Ready.Exception.GetBaseException(), this);
                    yield break;
                }
            }

            if (admission != null)
            {
                var settings = GameServices.Resolve<ClientSettings>();
                string cacheDirectory = settings.AppearanceCacheDirectory;
                string server = settings.LoginEndpoint;
                string username = connection.Username;
                var task = assets.ReadAsync(loader =>
                {
                    var result = new List<CachedCharacterAppearance>();
                    foreach (var ch in admission.Characters)
                    {
                        try
                        {
                            result.Add(CharacterAppearanceCache.LoadOrFallback(loader, cacheDirectory, server, username, admission.Galaxy.Name, ch));
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("Character appearance unavailable: " + e.Message);
                            result.Add(null);
                        }
                    }

                    return result;
                });
                yield return new WaitUntil(() => task.IsCompleted);
                if (task.IsFaulted)
                {
                    Debug.LogException(task.Exception.GetBaseException(), this);
                    yield break;
                }

                if (task.IsCanceled)
                    yield break;
                appearances.AddRange(task.Result);
            }

            try
            {
                PopulateCharacterList();
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                yield break;
            }

            if (rows.Count > 0)
                yield return SelectCharacter(0);
            yield return null;
            try
            {
                TreTextureBinding.ThrowIfFailed(content.transform);
                content.alpha = 1;
                content.interactable = true;
                content.blocksRaycasts = true;
                preview.previewCamera.enabled = true;
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                yield break;
            }

            IsReady = true;
            Debug.Log("CharacterListScreen: Ready", this);
        }

        void PopulateCharacterList()
        {
            preview.Initialize(assets);
            back.onClick.AddListener(Back);
            create.onClick.AddListener(CreateNew);
            next.interactable = false;
            next.onClick.AddListener(EnterWorld);
            delete.interactable = false;
            var admission = connection.Admission;
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

            if (rows.Count == 0)
                message.text = "No characters on this account. Choose Create to make a character.";
        }

        static string PlanetName(string name) => string.IsNullOrEmpty(name) ? "" : char.ToUpperInvariant(name[0]) + name.Substring(1);
        public void Select(int index) => StartCoroutine(SelectCharacter(index));
        IEnumerator SelectCharacter(int index)
        {
            if (index < 0 || index >= rows.Count)
                yield break;
            selected = index;
            next.interactable = connection.Admission != null && rows[index].Character.Galaxy == connection.Admission.Galaxy.Id;
            int version = ++selectionVersion;
            for (int i = 0; i < rows.Count; i++)
                rows[i].SetSelected(i == selected);
            characterName.text = rows[index].Character.Name;
            if (appearances[index] == null)
            {
                message.text = "Character appearance unavailable.";
                yield break;
            }

            if (!models.TryGetValue(index, out var model))
            {
                var appearance = appearances[index];
                var task = assets.ReadAsync(loader => CachedCharacterModel.LoadDetailed(loader, appearance));
                yield return new WaitUntil(() => task.IsCompleted);
                if (task.IsFaulted)
                {
                    message.text = "Character preview unavailable.";
                    Debug.LogException(task.Exception.GetBaseException(), this);
                    yield break;
                }

                if (task.IsCanceled)
                    yield break;
                model = task.Result;
                if (this == null)
                    yield break;
                models[index] = model;
            }

            if (this == null || version != selectionVersion)
                yield break;
            try
            {
                preview.Show(rows[index].Character.SwgId.ToString(), model);
                message.text = "Choose the Character you would like to play now.\nYou may also choose to Create a new Character, or Delete one of your existing Characters.";
                foreach (var warning in model.Warnings)
                    Debug.LogWarning(warning);
            }
            catch (Exception error)
            {
                message.text = "Character preview unavailable.";
                Debug.LogException(error, this);
            }
        }

        public void EnterWorld()
        {
            if (!IsReady || selected < 0 || !next.interactable)
                return;
            connection.SelectCharacter(rows[selected].Character.SwgId);
            next.interactable = false;
            ScreenChanger.Open(ScreenId.LoadingThematic);
        }

        public void CreateNew()
        {
            if (!IsReady || connection.Admission == null || !connection.Admission.CanCreate)
                return;
            ScreenChanger.Open(ScreenId.CharacterCreation);
        }

        public void Back()
        {
            connection.Disconnect();
            ScreenChanger.Open(ScreenId.Login);
        }

        void Update()
        {
            if (!IsReady)
                return;
            if (connection.FailureReason != null)
            {
                ScreenChanger.Open(ScreenId.Login);
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
