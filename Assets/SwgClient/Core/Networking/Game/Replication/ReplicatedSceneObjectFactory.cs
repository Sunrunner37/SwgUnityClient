using System;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.World;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Core.Networking.Game.Replication
{
    /// <summary>Interprets server identifiers without exposing them on application entities.</summary>
    internal static class ReplicatedSceneObjectFactory
    {
        public static CreatureSceneObject Create(CmdStartScenePacket start) => new CreatureSceneObject
        {
            SwgId = start.PlayerSwgId,
            TemplatePath = start.TemplatePath,
            Position = start.Position
        };
        public static SceneObject Create(SwgId swgId, ObjectTemplateCatalog templates, Type requiredType = null, uint? templateCrc = null)
        {
            string path = templates?.Resolve(templateCrc.GetValueOrDefault());
            var result = SceneObjectFactory.CreateInstance(ChooseType(path, requiredType) ?? typeof(SpatialSceneObject));
            result.SwgId = swgId;
            result.TemplateCrc = templateCrc.GetValueOrDefault();
            result.TemplatePath = path;
            return result;
        }

        public static SceneObject Refine(SceneObject current, ObjectTemplateCatalog templates, Type requiredType = null, uint? templateCrc = null)
        {
            string path = templates?.Resolve(templateCrc ?? current.TemplateCrc) ?? current.TemplatePath;
            var result = Refine(current, path, ChooseType(path, requiredType));
            if (templateCrc.HasValue)
                result.TemplateCrc = templateCrc.Value;
            return result;
        }

        public static SceneObject ResolveTemplate(SceneObject current, ObjectTemplateCatalog templates)
        {
            string path = templates.Resolve(current.TemplateCrc);
            return path == null ? current : Refine(current, path, SceneObjectFactory.TypeForTemplate(path));
        }

        static SceneObject Refine(SceneObject current, string path, Type requiredType)
        {
            var result = current;
            // Generic tangible/spatial baselines also apply to their more specific subclasses.
            bool fits = requiredType == null || (requiredType == typeof(SceneObject) ? current.GetType() == requiredType : requiredType.IsInstanceOfType(current));
            if (!fits)
            {
                result = SceneObjectFactory.CreateInstance(requiredType);
                current.CopyTo(result);
            }

            result.TemplatePath = path;
            return result;
        }

        static Type ChooseType(string path, Type requiredType)
        {
            var templateType = SceneObjectFactory.TypeForTemplate(path);
            if (templateType == typeof(TangibleSceneObject) && requiredType == typeof(CreatureSceneObject))
                return requiredType;
            return templateType ?? requiredType;
        }

        internal static Type TypeFor(ReplicatedObjectType objectType) => objectType switch
        {
            ReplicatedObjectType.Tangible => typeof(TangibleSceneObject),
            ReplicatedObjectType.Creature => typeof(CreatureSceneObject),
            ReplicatedObjectType.Building => typeof(TangibleSceneObject),
            ReplicatedObjectType.Cell => typeof(CellSceneObject),
            ReplicatedObjectType.Player => typeof(SceneObject),
            ReplicatedObjectType.Guild => typeof(SceneObject),
            ReplicatedObjectType.ManufactureSchematic => typeof(ManufactureSchematicSceneObject),
            ReplicatedObjectType.ResourceContainer => typeof(TangibleSceneObject),
            ReplicatedObjectType.Intangible => typeof(IntangibleSceneObject),
            ReplicatedObjectType.Mission => typeof(MissionSceneObject),
            _ => null
        };
    }
}
