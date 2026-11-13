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
        public static SceneObject Create(SwgId swgId, GameObjectUpdate update, ObjectTemplateCatalog templates)
        {
            string path = templates?.Resolve(update.TemplateCrc.GetValueOrDefault());
            var result = SceneObjectFactory.CreateInstance(TypeFor(update, path) ?? typeof(SpatialSceneObject));
            result.SwgId = swgId;
            result.ParentSwgId = update.ParentSwgId.GetValueOrDefault();
            result.TemplatePath = path;
            result.Apply(update);
            return result;
        }

        public static SceneObject Apply(SceneObject current, GameObjectUpdate update, ObjectTemplateCatalog templates)
        {
            string path = templates?.Resolve(update.TemplateCrc ?? current.TemplateCrc) ?? current.TemplatePath;
            var result = Refine(current, path, TypeFor(update, path));
            result.Apply(update);
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

        static Type TypeFor(GameObjectUpdate update, string path)
        {
            var packetType = TypeForPacket(update);
            var templateType = SceneObjectFactory.TypeForTemplate(path);
            if (templateType == typeof(TangibleSceneObject) && packetType == typeof(CreatureSceneObject))
                return packetType;
            return templateType ?? packetType;
        }

        static Type TypeForPacket(GameObjectUpdate update)
        {
            if (update.BaselineType.HasValue)
                return ((ReplicatedObjectType)update.BaselineType.Value) switch
                {
                    ReplicatedObjectType.Tangible => typeof(TangibleSceneObject),
                    ReplicatedObjectType.Creature => typeof(CreatureSceneObject),
                    ReplicatedObjectType.Building => typeof(TangibleSceneObject),
                    ReplicatedObjectType.Cell => typeof(SpatialSceneObject),
                    ReplicatedObjectType.Player => typeof(SceneObject),
                    ReplicatedObjectType.Guild => typeof(SceneObject),
                    ReplicatedObjectType.ResourceContainer => typeof(TangibleSceneObject),
                    ReplicatedObjectType.Intangible => typeof(SceneObject),
                    ReplicatedObjectType.Mission => typeof(SceneObject),
                    _ => null
                };
            if (update.Equipment != null || update.EquipmentDelta != null || update.Posture.HasValue || update.Height.HasValue)
                return typeof(CreatureSceneObject);
            return update.Customization != null ? typeof(TangibleSceneObject) : null;
        }
    }
}
