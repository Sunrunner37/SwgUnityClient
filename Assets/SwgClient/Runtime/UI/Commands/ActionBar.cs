using System.Collections;
using UnityEngine;
using Swg.Client.Runtime.Commands;
using Swg.Client.Runtime.Assets.UI;

namespace Swg.Client.Runtime.UI.Commands
{
    public sealed class ActionBar : MonoBehaviour
    {
        public CommandButton[] slots;
        public bool Ready { get; private set; }
        CommandCatalog catalog;
        CommandVisuals visuals;

        void Awake()
        {
            catalog = GameServices.Resolve<CommandCatalog>();
            visuals = GameServices.Resolve<CommandVisuals>();
        }

        void OnEnable()
        {
            catalog.Changed += Refresh;
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
        }

        void Refresh()
        {
            if (!Ready)
                return;
            foreach (var slot in slots)
                slot.RefreshBinding();
        }
    }
}
