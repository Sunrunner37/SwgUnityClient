using Swg.Client.Core.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Inventory contents are the replicated containment graph; no second item store is maintained.</summary>
    public sealed class ClientInventory : IDisposable
    {
        readonly WorldState state;
        readonly CurrentPlayer currentPlayer;
        readonly Action<SwgId> requestContents;
        readonly SynchronizationContext context;
        bool disposed;
        public SwgId ContainerSwgId { get; private set; }
        public IReadOnlyList<TangibleSceneObject> Items { get; private set; } = Array.Empty<TangibleSceneObject>();
        public int Capacity { get; private set; }
        public int UsedCapacity => state.SceneObjects.ChildrenOf(ContainerSwgId).OfType<TangibleSceneObject>().Sum(item => Math.Max(1, item.Volume));
        public CreatureSceneObject Player => currentPlayer.Creature;
        public event Action Changed;
        public ClientInventory(WorldState state, Action<SwgId> requestContents, CurrentPlayer currentPlayer)
        {
            this.currentPlayer = currentPlayer;
            this.state = state;
            this.requestContents = requestContents;
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("Create inventory on Unity's main thread.");
            state.SceneObjects.ObjectChanged += ObjectChanged;
            state.SceneObjects.Removed += Removed;
            Refresh();
        }
        public void Open()
        {
            Refresh();
            if (!ContainerSwgId.IsNone)
                requestContents(ContainerSwgId);
        }
        public bool IsEquipped(TangibleSceneObject item) => item.ParentSwgId == currentPlayer.SwgId;
        void ObjectChanged(SceneObject sceneObject, SceneObjectChange changes)
        {
            if ((changes & (SceneObjectChange.Appearance | SceneObjectChange.Containment | SceneObjectChange.Inventory | SceneObjectChange.Replaced)) == 0)
                return;
            if (sceneObject.SwgId == currentPlayer.SwgId || sceneObject.ParentSwgId == currentPlayer.SwgId || sceneObject.ParentSwgId == ContainerSwgId || Items.Any(item => item.SwgId == sceneObject.SwgId))
                Refresh();
        }
        void Removed(SceneObject sceneObject)
        {
            if (sceneObject.SwgId == ContainerSwgId || Items.Any(item => item.SwgId == sceneObject.SwgId))
                Refresh();
        }
        void Refresh()
        {
            var children = state.SceneObjects.ChildrenOf(currentPlayer.SwgId);
            var container = children.FirstOrDefault(item => item.TemplatePath?.StartsWith("object/tangible/inventory/", StringComparison.Ordinal) == true);
            ContainerSwgId = container?.SwgId ?? default;
            Capacity = container == null ? 0 : state.Templates.ContainerCapacity(container.TemplatePath);
            Items = state.SceneObjects.ChildrenOf(ContainerSwgId).OfType<TangibleSceneObject>()
                .Concat(children.OfType<TangibleSceneObject>().Where(item => item.TemplatePath?.StartsWith("object/tangible/wearables/", StringComparison.Ordinal) == true))
                .OrderBy(item => item.SwgId.Value).ToArray();
            Changed?.Invoke();
        }
        public void Receive(ClientOpenContainerPacket packet) => context.Post(_ =>
        {
            if (!disposed && packet.SwgId == ContainerSwgId.Value)
                Refresh();
        }, null);
        public void Dispose()
        {
            disposed = true;
            state.SceneObjects.ObjectChanged -= ObjectChanged;
            state.SceneObjects.Removed -= Removed;
        }
    }
}
