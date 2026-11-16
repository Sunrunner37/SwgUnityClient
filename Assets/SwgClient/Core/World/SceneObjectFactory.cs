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
            if (path.StartsWith("object/group/", StringComparison.OrdinalIgnoreCase))
                return typeof(GroupSceneObject);
            if (path.StartsWith("object/mission/", StringComparison.OrdinalIgnoreCase))
                return typeof(MissionSceneObject);
            if (path.StartsWith("object/manufacture_schematic/", StringComparison.OrdinalIgnoreCase))
                return typeof(ManufactureSchematicSceneObject);
            if (path.StartsWith("object/creature/", StringComparison.OrdinalIgnoreCase))
                return typeof(CreatureSceneObject);
            if (path.StartsWith("object/cell/", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/shared_cell.iff", StringComparison.OrdinalIgnoreCase))
                return typeof(CellSceneObject);
            if (path.StartsWith("object/intangible/", StringComparison.OrdinalIgnoreCase))
                return typeof(IntangibleSceneObject);
            if (path.StartsWith("object/waypoint/", StringComparison.OrdinalIgnoreCase))
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
            if (result is CellSceneObject cell) cell.Number = (int)entry.CellSwgId;
            result.Name = entry.TemplatePath;
            result.Position = entry.Position;
            result.Rotation = entry.Rotation;
            return result;
        }

        internal static SceneObject CreateInstance(Type type)
        {
            if (type == typeof(MissionSceneObject))
                return new MissionSceneObject();
            if (type == typeof(ManufactureSchematicSceneObject))
                return new ManufactureSchematicSceneObject();
            if (type == typeof(IntangibleSceneObject))
                return new IntangibleSceneObject();
            if (type == typeof(GroupSceneObject))
                return new GroupSceneObject();
            if (type == typeof(SceneObject))
                return new SceneObject();
            if (type == typeof(CellSceneObject))
                return new CellSceneObject();
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
