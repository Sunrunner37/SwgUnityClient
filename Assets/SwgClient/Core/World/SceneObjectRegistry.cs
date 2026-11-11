using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.World.SceneObjects;
using UnityEngine;

namespace Swg.Client.Core.World
{
    public sealed class SceneObjectRegistry : IReadOnlyCollection<SceneObject>
    {
        readonly Dictionary<SwgId, SceneObject> objects = new Dictionary<SwgId, SceneObject>();
        readonly Dictionary<SwgId, List<SceneObject>> waitingForParent = new Dictionary<SwgId, List<SceneObject>>();
        readonly Dictionary<SwgId, Action<SceneObjectChange>> subscriptions = new Dictionary<SwgId, Action<SceneObjectChange>>();
        public long Revision { get; private set; }
        public int Count => objects.Count;

        public event Action<SceneObject> Removed;
        public event Action<SceneObject, SceneObjectChange> ObjectChanged;
        public IEnumerator<SceneObject> GetEnumerator() => objects.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool TryGet(SwgId swgId, out SceneObject value) => objects.TryGetValue(swgId, out value);
        public bool TryGet<T>(SwgId swgId, out T value)
            where T : SceneObject
        {
            if (objects.TryGetValue(swgId, out var found) && found is T typed)
            {
                value = typed;
                return true;
            }

            value = null;
            return false;
        }

        public IReadOnlyList<SceneObject> ChildrenOf(SwgId swgId)
        {
            if (TryGet(swgId, out var parent))
                return parent.Children;
            return waitingForParent.TryGetValue(swgId, out var children) ? children.AsReadOnly() : Array.Empty<SceneObject>();
        }

        public void Subscribe(SwgId swgId, Action<SceneObjectChange> callback)
        {
            subscriptions.TryGetValue(swgId, out var listeners);
            subscriptions[swgId] = listeners + callback;
        }

        public void Unsubscribe(SwgId swgId, Action<SceneObjectChange> callback)
        {
            if (!subscriptions.TryGetValue(swgId, out var listeners))
                return;
            listeners -= callback;
            if (listeners == null)
                subscriptions.Remove(swgId);
            else
                subscriptions[swgId] = listeners;
        }

        void Dispatch(SceneObject value, SceneObjectChange changes)
        {
            if (subscriptions.TryGetValue(value.SwgId, out var listeners))
                listeners(changes);
            ObjectChanged?.Invoke(value, changes);
        }

        public void Add(SceneObject value)
        {
            ValidateParent(value.SwgId, value.ParentSwgId);
            objects.Add(value.SwgId, value);
            Attach(value);
            if (waitingForParent.TryGetValue(value.SwgId, out var children))
            {
                foreach (var child in children)
                {
                    child.Parent = value;
                    value.AttachChild(child);
                }

                waitingForParent.Remove(value.SwgId);
            }

            NotifyChanged(value, SceneObjectChange.All);
        }

        public void Reparent(SwgId swgId, SwgId parentSwgId)
        {
            if (!TryGet(swgId, out var value))
                throw new KeyNotFoundException("Unknown scene object " + swgId);
            if (value.ParentSwgId == parentSwgId)
                return;
            ValidateParent(swgId, parentSwgId);
            var previousParent = value.Parent;
            Detach(value);
            value.ParentSwgId = parentSwgId;
            Attach(value);
            NotifyChanged(value, SceneObjectChange.Containment);
            if (previousParent != null)
                Dispatch(previousParent, SceneObjectChange.Appearance);
        }

        void ValidateParent(SwgId swgId, SwgId parentSwgId)
        {
            var seen = new HashSet<SwgId>();
            while (!parentSwgId.IsNone && seen.Add(parentSwgId))
            {
                if (parentSwgId == swgId)
                    throw new InvalidOperationException("Scene-object containment cannot contain a cycle.");
                if (!TryGet(parentSwgId, out var parent))
                    break;
                parentSwgId = parent.ParentSwgId;
            }
        }

        void Attach(SceneObject value)
        {
            if (value.ParentSwgId.IsNone)
                return;
            if (TryGet(value.ParentSwgId, out var parent))
            {
                value.Parent = parent;
                parent.AttachChild(value);
            }
            else
            {
                if (!waitingForParent.TryGetValue(value.ParentSwgId, out var children))
                    waitingForParent.Add(value.ParentSwgId, children = new List<SceneObject>());
                children.Add(value);
            }
        }

        void Detach(SceneObject value)
        {
            if (value.Parent != null)
                value.Parent.DetachChild(value);
            else if (waitingForParent.TryGetValue(value.ParentSwgId, out var children))
            {
                children.Remove(value);
                if (children.Count == 0)
                    waitingForParent.Remove(value.ParentSwgId);
            }

            value.Parent = null;
        }

        /// <summary>Swaps a model while preserving ID subscriptions and logical graph links.</summary>
        public void Replace(SceneObject value)
        {
            var previous = objects[value.SwgId];
            if (ReferenceEquals(previous, value))
                return;
            value.ParentSwgId = previous.ParentSwgId;
            Detach(previous);
            objects[value.SwgId] = value;
            Attach(value);
            foreach (var child in previous.Children)
            {
                child.Parent = value;
                value.AttachChild(child);
            }

            previous.ClearChildren();
            NotifyChanged(value, SceneObjectChange.All | SceneObjectChange.Replaced);
        }

        public void NotifyChanged(SceneObject value, SceneObjectChange changes)
        {
            if (changes == SceneObjectChange.None)
                return;
            Revision++;
            Publish(value, changes);
            if ((changes & (SceneObjectChange.Appearance | SceneObjectChange.Containment)) != 0 && value.Parent != null)
                Dispatch(value.Parent, SceneObjectChange.Appearance);
        }

        void Publish(SceneObject value, SceneObjectChange changes)
        {
            Dispatch(value, changes);
            // Parent-relative poses and visibility can change without a packet for each child.
            var inherited = changes & (SceneObjectChange.Transform | SceneObjectChange.Containment);
            if (inherited != SceneObjectChange.None)
                foreach (var child in value.Children)
                    Publish(child, inherited);
        }

        /// <summary>Resolves parent-relative poses into absolute SWG server coordinates.</summary>
        public Pose GetServerWorldPose(SwgId swgId)
        {
            if (!TryGet<SpatialSceneObject>(swgId, out var spatial))
                throw new InvalidOperationException("This scene object has no spatial pose.");
            var position = spatial.Position;
            var rotation = spatial.Rotation.normalized;
            var parent = spatial.Parent;
            while (parent != null)
            {
                if (parent is SpatialSceneObject transform)
                {
                    var direction = transform.Rotation.normalized;
                    position = transform.Position + direction * position;
                    rotation = direction * rotation;
                }

                parent = parent.Parent;
            }

            return new Pose(position, rotation);
        }

        public void Remove(SwgId swgId)
        {
            if (!TryGet(swgId, out var value))
                return;
            var previousParent = value.Parent;
            Detach(value);
            objects.Remove(swgId);
            if (value.Children.Count > 0)
            {
                var children = value.Children.ToList();
                foreach (var child in children)
                    child.Parent = null;
                waitingForParent[swgId] = children;
                value.ClearChildren();
                foreach (var child in children)
                    Publish(child, SceneObjectChange.Containment);
            }

            if (previousParent != null)
                Dispatch(previousParent, SceneObjectChange.Appearance);
            Revision++;
            Removed?.Invoke(value);
            subscriptions.Remove(swgId);
        }

        public void Clear()
        {
            foreach (var swgId in objects.Keys.ToArray())
                Remove(swgId);
            waitingForParent.Clear();
            subscriptions.Clear();
        }
    }
}
