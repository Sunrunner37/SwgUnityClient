using Swg.Client.Runtime.World.Loading;
using System.Collections;
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
        public ExamineAttributes details;
        public ExaminePreview preview;
        Coroutine loading;
        void OnDisable()
        {
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
            details.Show(swgId);
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
        public void Close() => gameObject.SetActive(false);
    }
}
