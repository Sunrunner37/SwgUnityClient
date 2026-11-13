using System;
using System.Collections;
using System.Linq;
using Swg.Client.Runtime.Assets.UI;
using System.Collections.Generic;
using UnityEngine;
using Swg.Client.Runtime.Commands;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Swg.Client.Runtime.UI.Commands
{
    public sealed class CommandBrowser : MonoBehaviour, IUiPrefabSource
    {
        [Serializable]
        public sealed class Category
        {
            public string id;
            public Toggle toggle;
        }

        public CommandDrag drag;
        public Transform content;
        public CommandButton rowPrefab;
        public IEnumerable<GameObject> UiPrefabs
        {
            get
            {
                yield return rowPrefab.gameObject;
            }
        }
        public List<Category> categories = new List<Category>();
        readonly List<CommandButton> rows = new List<CommandButton>();
        readonly List<UnityAction<bool>> listeners = new List<UnityAction<bool>>();
        CommandCatalog catalog;
        CommandVisuals visuals;
        public bool Ready { get; private set; }
        internal IEnumerable<CommandCatalog.Command> BrowserCommands => catalog.Commands.Where(command => command.BrowserVisible);

        void Awake()
        {
            catalog = GameServices.Resolve<CommandCatalog>();
            visuals = GameServices.Resolve<CommandVisuals>();
        }

        void OnEnable()
        {
            catalog.Changed += Refresh;
            foreach (var category in categories)
            {
                UnityAction<bool> listener = selected =>
                {
                    if (selected)
                        Refresh();
                };
                category.toggle.onValueChanged.AddListener(listener);
                listeners.Add(listener);
            }

            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            yield return new WaitUntil(() => visuals.Loading.IsCompleted);
            if (visuals.Loading.IsFaulted)
            {
                Debug.LogException(visuals.Loading.Exception.GetBaseException(), this);
                yield break;
            }
            Ready = true;
            Refresh();
        }

        void OnDisable()
        {
            StopAllCoroutines();
            catalog.Changed -= Refresh;
            for (int i = 0; i < categories.Count; i++)
                categories[i].toggle.onValueChanged.RemoveListener(listeners[i]);
            listeners.Clear();
            drag.End();
        }

        public void ToggleOpen() => gameObject.SetActive(!gameObject.activeSelf);
        public void Close() => gameObject.SetActive(false);
        void Refresh()
        {
            if (!Ready)
                return;
            foreach (var row in rows)
                Destroy(row.gameObject);
            rows.Clear();
            string category = categories.Find(c => c.toggle.isOn)?.id ?? "combat";
            foreach (var command in BrowserCommands)
            {
                if (visuals.GetCategory(command) != category)
                    continue;
                var row = Instantiate(rowPrefab, content);
                row.drag = drag;
                row.Bind(command);
                rows.Add(row);
            }
        }
    }
}
