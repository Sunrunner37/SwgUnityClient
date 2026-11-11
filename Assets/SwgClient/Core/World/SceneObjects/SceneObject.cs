using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Swg.Client.Core.World.SceneObjects
{
    /// <summary>The client state of an identified scene object. Containment is maintained by SceneObjectRegistry.</summary>
    public class SceneObject
    {
        List<SceneObject> children = new List<SceneObject>();
        ReadOnlyCollection<SceneObject> childrenView;
        public SwgId SwgId { get; internal set; }
        public SwgId ParentSwgId { get; internal set; }
        public SceneObject Parent { get; internal set; }
        public IReadOnlyList<SceneObject> Children => childrenView;

        public uint TemplateCrc;
        public string TemplatePath;
        public string Name = "";
        public uint Arrangement;
        public SceneObject() => childrenView = children.AsReadOnly();
        internal void AttachChild(SceneObject child) => children.Add(child);
        internal void DetachChild(SceneObject child) => children.Remove(child);
        internal void ClearChildren() => children.Clear();
        internal virtual void CopyTo(SceneObject target)
        {
            target.SwgId = SwgId;
            target.ParentSwgId = ParentSwgId;
            target.TemplateCrc = TemplateCrc;
            target.TemplatePath = TemplatePath;
            target.Name = Name;
            target.Arrangement = Arrangement;
        }

        // Workers get detached values, not references into the live containment graph.
        internal virtual SceneObject Copy()
        {
            var copy = (SceneObject)MemberwiseClone();
            copy.Parent = null;
            copy.children = new List<SceneObject>();
            copy.childrenView = copy.children.AsReadOnly();
            return copy;
        }
    }
}
