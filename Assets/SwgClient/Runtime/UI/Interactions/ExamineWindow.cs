using Swg.Client.Runtime.World.Loading;
using System;
using System.Collections;
using System.Text;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using TMPro;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Interactions
{
    public sealed class ExamineWindow : MonoBehaviour
    {
        public TMP_Text title;
        public TMP_Text attributes;
        public ExaminePreview preview;
        ClientObjectInteractions interactions;
        StringTableLocalizer localizer;
        Coroutine loading;
        void Awake()
        {
            interactions = GameServices.Resolve<ClientObjectInteractions>();
            localizer = GameServices.Resolve<StringTableLocalizer>();
        }
        void OnEnable() => interactions.AttributesChanged += RefreshAttributes;
        void OnDisable()
        {
            interactions.AttributesChanged -= RefreshAttributes;
            if (loading != null)
                StopCoroutine(loading);
            loading = null;
            preview.Clear();
        }
        public void Open(SwgId swgId)
        {
            if (!GameServices.Resolve<WorldState>().SceneObjects.TryGet(swgId, out var sceneObject))
                return;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            title.text = GameServices.Resolve<SceneObjectNameResolver>().Resolve(sceneObject).ToUpperInvariant();
            if (loading != null)
                StopCoroutine(loading);
            preview.Clear();
            loading = StartCoroutine(LoadPreview(sceneObject));
            interactions.RequestExamine(swgId);
        }
        IEnumerator LoadPreview(SceneObject sceneObject)
        {
            var read = GameServices.Resolve<WorldContentLoader>().LoadAppearanceAsync(sceneObject);
            yield return new WaitUntil(() => read.IsCompleted);
            if (read.IsFaulted)
                Debug.LogException(read.Exception.GetBaseException(), this);
            else if (!read.IsCanceled)
                preview.Show(read.Result);
            loading = null;
        }
        void RefreshAttributes()
        {
            var text = new StringBuilder();
            foreach (var attribute in interactions.Attributes)
            {
                string name = localizer.Lookup("obj_attr_n", attribute.Name);
                if (name.StartsWith("@", StringComparison.Ordinal))
                    name = attribute.Name.Replace('_', ' ');
                text.Append(name).Append(":    ").AppendLine(localizer.Localize(attribute.Value));
            }
            attributes.text = text.ToString();
        }
        public void Close() => gameObject.SetActive(false);
    }
}
