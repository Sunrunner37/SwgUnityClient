using System;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.World.SceneObjects;

namespace Swg.Client.Core.World
{
    public static class SceneObjectFactory
    {
        internal static Type TypeForTemplate(string templatePath)
        {
            string path = templatePath ?? "";
            if (path.StartsWith("object/creature/", StringComparison.OrdinalIgnoreCase))
                return typeof(CreatureSceneObject);
            if (path.StartsWith("object/cell/", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/shared_cell.iff", StringComparison.OrdinalIgnoreCase))
                return typeof(SpatialSceneObject);
            if (path.StartsWith("object/intangible/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("object/mission/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("object/waypoint/", StringComparison.OrdinalIgnoreCase))
                return typeof(SceneObject);
            if (path.StartsWith("object/building/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("object/tangible/", StringComparison.OrdinalIgnoreCase))
                return typeof(TangibleSceneObject);
            return null;
        }

        public static SceneObject Create(SwgId swgId, string templatePath)
        {
            var result = CreateInstance(TypeForTemplate(templatePath) ?? typeof(SpatialSceneObject));
            result.SwgId = swgId;
            result.TemplatePath = templatePath;
            return result;
        }

        public static SpatialSceneObject Create(SnapshotEntry entry)
        {
            var result = (SpatialSceneObject)Create(entry.SwgId, entry.TemplatePath);
            result.ParentSwgId = entry.ParentSwgId;
            result.Name = entry.TemplatePath;
            result.Position = entry.Position;
            result.Rotation = entry.Rotation;
            return result;
        }

        internal static SceneObject CreateInstance(Type type)
        {
            if (type == typeof(SceneObject))
                return new SceneObject();
            if (type == typeof(SpatialSceneObject))
                return new SpatialSceneObject();
            if (type == typeof(TangibleSceneObject))
                return new TangibleSceneObject();
            if (type == typeof(CreatureSceneObject))
                return new CreatureSceneObject();
            throw new ArgumentException("Unsupported scene-object class: " + type, nameof(type));
        }
    }
}
