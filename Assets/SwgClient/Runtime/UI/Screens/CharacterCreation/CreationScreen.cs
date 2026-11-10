using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Assets.Tre;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Swg.Client.Core.Characters.Creation;

namespace Swg.Client.Runtime.UI.Screens.CharacterCreation
{
    public sealed class CreationScreen : ClientScreen
    {
        ClientConnection connection;
        public Text account;
        public Text speciesName;
        public Text description;
        public ScrollRect descriptionScroll;
        public List<Button> speciesButtons = new List<Button>();
        public Button gender;
        public Button back;
        public Button next;
        public CharacterPreview preview;
        CharacterCreationData creation;
        int species;
        bool female;
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
            System.Threading.Tasks.Task<CharacterCreationData> task;
            try
            {
                Debug.Log("CreationScreen: Loading species", this);
                task = assets.ReadAsync(loader => new CharacterCreationData(loader));
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                yield break;
            }

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception.GetBaseException(), this);
                yield break;
            }

            if (task.IsCanceled)
                yield break;
            try
            {
                creation = task.Result;
                PopulateSpecies();
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                yield break;
            }

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
            Debug.Log("CreationScreen: Ready", this);
        }

        void PopulateSpecies()
        {
            var accepted = connection.Admission;
            account.text = accepted == null ? "Offline character preview" : accepted.Galaxy.Name + "  |  Account " + accepted.AccountId + "  |  " + accepted.Characters.Count + " character(s)";
            next.interactable = false;
            back.onClick.AddListener(Back);
            gender.onClick.AddListener(ToggleGender);
            for (int i = 0; i < speciesButtons.Count; i++)
            {
                int index = i;
                speciesButtons[i].gameObject.SetActive(i < creation.Species.Count);
                if (i >= creation.Species.Count)
                    continue;
                speciesButtons[i].GetComponentInChildren<Text>().text = creation.Species[i].Name;
                speciesButtons[i].onClick.AddListener(() => SelectSpecies(index));
            }

            preview.Initialize(assets);
            UpdateSpecies();
        }

        public void SelectSpecies(int index)
        {
            if (creation == null)
                return;
            species = Mathf.Clamp(index, 0, creation.Species.Count - 1);
            UpdateSpecies();
        }

        public void ToggleGender()
        {
            female = !female;
            if (creation != null)
                UpdateSpecies();
        }

        void UpdateSpecies()
        {
            var entry = creation.Species[species];
            speciesName.text = entry.Name;
            description.text = entry.Description;
            description.rectTransform.anchoredPosition = Vector2.zero;
            descriptionScroll.verticalNormalizedPosition = 1;
            gender.GetComponentInChildren<Text>().text = female ? "GENDER: FEMALE" : "GENDER: MALE";
            for (int i = 0; i < speciesButtons.Count; i++)
                speciesButtons[i].GetComponent<Image>().color = i == species ? new Color(0, .75f, .85f, .35f) : Color.clear;
            try
            {
                preview.Show(female ? entry.Female : entry.Male);
            }
            catch (System.Exception e)
            {
                speciesName.text = entry.Name + " - preview unavailable";
                Debug.LogException(e);
            }
        }

        public void Back()
        {
            ScreenChanger.Open(connection.Admission == null ? ScreenId.Login : ScreenId.CharacterList);
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
                SelectSpecies(species - 1);
            else if (key.downArrowKey.wasPressedThisFrame)
                SelectSpecies(species + 1);
            else if (key.tabKey.wasPressedThisFrame)
                ToggleGender();
        }
    }
}
