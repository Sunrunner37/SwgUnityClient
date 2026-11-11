using System;
using System.Collections.Generic;
using Swg.Client.Core;
using Swg.Client.Core.World;
using UnityEngine;

namespace Swg.Client.Runtime.World
{
    /// <summary>Tags a rendered scene-object root. Mesh parts resolve it through GetComponentInParent.</summary>
    [DisallowMultipleComponent]
    public sealed class ManagedSceneObject : MonoBehaviour
    {
        static readonly Dictionary<SwgId, GameObject> objects = new Dictionary<SwgId, GameObject>();
        static GameObject player;
        public static GameObject CurrentPlayer
        {
            get
            {
                if (player == null)
                    player = objects[GameServices.Resolve<WorldState>().PlayerSwgId];
                return player;
            }

            private set => player = value;
        }

        public SwgId SwgId { get; private set; }

        internal static void ResetPlayerCache()
        {
            CurrentPlayer = null;
        }

        public void Bind(SwgId swgId)
        {
            if (!SwgId.IsNone)
                throw new InvalidOperationException("This component is already bound.");
            if (swgId.IsNone)
                throw new ArgumentException("A managed scene object needs a nonzero SwgId.", nameof(swgId));
            if (objects.TryGetValue(swgId, out var existing) && existing != null && existing != gameObject)
                throw new InvalidOperationException("SwgId " + swgId + " already has a managed GameObject.");
            SwgId = swgId;
            objects[swgId] = gameObject;
        }

        public static bool TryGetObjectFrom(SwgId swgId, out GameObject value)
        {
            if (objects.TryGetValue(swgId, out value) && value != null)
                return true;
            return false;
        }

        internal void Unbind()
        {
            if (player == gameObject)
                CurrentPlayer = null;
            if (!SwgId.IsNone && objects.TryGetValue(SwgId, out var current) && current == gameObject)
                objects.Remove(SwgId);
            SwgId = default;
        }

        void OnDestroy() => Unbind();
    }
}
