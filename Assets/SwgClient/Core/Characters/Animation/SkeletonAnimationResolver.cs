using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Core.Characters.Animation
{
    /// <summary>Selects native clips for actions, performances, and moods using the skeleton's shared clip cache.</summary>
    internal sealed class SkeletonAnimationResolver
    {
        readonly object cacheLock;
        readonly AshFile hierarchy;
        readonly IReadOnlyDictionary<string, LatFile.Animation> logicalAnimations;
        readonly Func<string, SkeletalAnimation> loadAnimation;

        public SkeletonAnimationResolver(AshFile hierarchy, IReadOnlyDictionary<string, LatFile.Animation> logicalAnimations,
            Func<string, SkeletalAnimation> loadAnimation, object cacheLock)
        {
            this.hierarchy = hierarchy;
            this.logicalAnimations = logicalAnimations;
            this.loadAnimation = loadAnimation;
            this.cacheLock = cacheLock;
        }

        public SkeletalAnimation ResolveAction(string action, string weapon, CreaturePosture posture, bool combat = true, string performance = "")
        {
            lock (cacheLock)
            {
                string postureState = posture == CreaturePosture.Prone ? "prone" : posture == CreaturePosture.Crouched ? "kneeling"
                    : posture == CreaturePosture.Dead ? "dead" : posture == CreaturePosture.Incapacitated ? "incapacitated_face_up" : "";
                foreach (string candidate in ActionCandidates(action))
                {
                    string name = hierarchy.Resolve(candidate, weapon, combat ? "combat" : "", postureState, performance.Length > 0 ? "skill" : "");
                    if (candidate != null && name == "default") continue;
                    if (!logicalAnimations.TryGetValue(name, out var logical))
                    {
                        if (candidate == null || !logicalAnimations.TryGetValue("cbt_" + candidate, out logical) && !logicalAnimations.TryGetValue("rea_" + candidate, out logical))
                            continue;
                    }
                    if (performance.Length > 0 && logical.Selections.TryGetValue(("skill", performance), out var selected) && selected.Length > 0)
                        return loadAnimation(selected[0]);
                    string path = (action == null ? logical.IdleClips.FirstOrDefault() : null) ?? logical.Clips.FirstOrDefault();
                    if (path != null) return loadAnimation(path);
                }
                return null;
            }
        }

        public SkeletalAnimation ResolveAmbient(string performance, string mood)
        {
            lock (cacheLock)
            {
                string logicalName = hierarchy.Resolve(null, performance.Length > 0 ? "skill" : "");
                if (!logicalAnimations.TryGetValue(logicalName, out var logical)) return null;
                string selector = performance.Length > 0 ? "skill" : "mood";
                string value = performance.Length > 0 ? performance : mood;
                return logical.Selections.TryGetValue((selector, value), out var clips) && clips.Length > 0 ? loadAnimation(clips[0]) : null;
            }
        }

        static IEnumerable<string> ActionCandidates(string action)
        {
            yield return action;
            if (action == null) yield break;
            // Core3 can request a fourth variant and a center-leg strike that older native hierarchies omit.
            // Prefer another native variant at the same height, then the adjacent leg's animation.
            var directions = action.Contains("_low_center") ? new[] { action, action.Replace("_low_center", "_low_left"), action.Replace("_low_center", "_low_right") } : new[] { action };
            foreach (string direction in directions)
            {
                if (direction != action) yield return direction;
                if (direction.StartsWith("attack_") && char.IsDigit(direction[direction.Length - 1]))
                    for (int i = 0; i < 3; i++) yield return direction.Substring(0, direction.Length - 1) + i;
            }
            if (action.StartsWith("get_hit_"))
                yield return action.StartsWith("get_hit_medium") ? "get_hit_medium" : "get_hit_light";
            if (action == "dodge")
            {
                yield return "unarmed_standing_ready_dodge_bob_left";
                yield return "rea_stand_combat_dodge";
                yield return "rea_stand_dodge";
            }
            if (action == "fire_single") yield return "fire_1";
            if (action == "fire_burst") yield return "fire_3";
            if (action == "fire_suppress") yield return "fire_5";
        }

    }
}
