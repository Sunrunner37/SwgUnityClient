using TMPro;
using Swg.Client.Runtime.World.Loading;
using Swg.Client.Runtime.Assets.Tre;
using System;
using System.Collections;
using System.Threading;
using Swg.Client.Core.Assets.Loading;
using Swg.Client.Runtime.World;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Screens.Loading
{
    public sealed class LoadingScreen : ClientScreen
    {
        public LoadingScreenView view;
        public WorldEntryController entry;
        public string WorldPhase => entry == null ? "Loading..." : entry.Phase;
        public TMP_Text status => view.status;
        public Slider progress => view.progress;
        public Button cancel => view.cancel;

        readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        bool failed;
        public CanvasGroup content;
        void Awake()
        {
            content.alpha = 0;
            content.interactable = false;
            content.blocksRaycasts = false;
        }

        TreRuntimeAssets assets;
        IEnumerator Start()
        {
            assets = GameServices.Resolve<TreRuntimeAssets>();
            try
            {
                if (entry == null || view == null || view.status == null || view.progress == null)
                    throw new InvalidOperationException("LoadingScreen requires an entry controller and a view with status and progress controls.");
                UpdateView();
                status.text = "Loading...";
                Debug.Log("LoadingScreen: Entering world", this);
                entry.Begin();
            }
            catch (Exception error)
            {
                Fail(error);
                yield break;
            }

            yield return new WaitUntil(() => entry.Destination != null || entry.Failure != null);
            if (entry.Failure != null)
            {
                Fail(entry.Failure);
                yield break;
            }

            string planet = entry.Destination;
            string table = view.contentTable;
            string fallback = view.fallbackTable;
            int selection = UnityEngine.Random.Range(0, int.MaxValue);
            var token = lifetime.Token;
            var task = assets.ReadAsync(loader =>
            {
                token.ThrowIfCancellationRequested();
                return LoadingScreenDataLoader.Load(loader, planet, table, fallback, selection);
            });
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted)
            {
                Fail(task.Exception.GetBaseException());
                yield break;
            }

            if (task.IsCanceled)
                yield break;
            try
            {
                view.Show(task.Result, assets);
            }
            catch (Exception error)
            {
                Fail(error);
                yield break;
            }

            yield return null;
            try
            {
                TreTextureBinding.ThrowIfFailed(content.transform);
                content.alpha = 1;
                content.interactable = true;
                content.blocksRaycasts = true;
            }
            catch (Exception error)
            {
                Fail(error);
                yield break;
            }

            yield return new WaitUntil(() => entry.PlayerReady || entry.Failure != null);
            if (entry.Failure != null)
            {
                Fail(entry.Failure);
                yield break;
            }

            try
            {
                UpdateView();
                IsReady = true;
                Debug.Log("LoadingScreen: Ready", this);
                entry.Activate(gameObject.scene);
            }
            catch (Exception error)
            {
                Fail(error);
            }
        }

        void Fail(Exception error)
        {
            failed = true;
            IsReady = false;
            entry?.Abort();
            Debug.LogException(error, this);
            if (view != null && view.status != null)
                status.text = "Unable to load world.";
        }

        void UpdateView()
        {
            progress.SetValueWithoutNotify(entry.Progress);
            view.radial?.SetValue(entry.Progress);
        }

        void Update()
        {
            if (!failed && entry != null && view != null)
                UpdateView();
        }

        void OnDestroy()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
