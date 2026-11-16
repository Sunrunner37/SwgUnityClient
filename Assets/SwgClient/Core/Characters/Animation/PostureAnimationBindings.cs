using System;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Core.Characters.Animation
{
    /// <summary>Maps replicated postures to native logical animation loops and transitions.</summary>
    internal static class PostureAnimationBindings
    {
        static readonly (CreaturePosture Posture, string State)[] postures =
        {
            (CreaturePosture.Upright, "standing"),
            (CreaturePosture.Crouched, "kneeling"),
            (CreaturePosture.Prone, "prone"),
            (CreaturePosture.Sitting, "sitting_ground")
        };

        public static void Apply(SkeletonData skeleton, Func<string, string> clip, Func<string, SkeletalAnimation> load)
        {
            string fallen = clip("loop_incapacitated_face_up") ?? clip("loop_incapacitated");
            if (fallen != null)
                BindFallen(skeleton, clip, fallen, load);

            skeleton.Postures.Add(CreaturePosture.Upright, skeleton.Idle);
            if (TryBindTransport(skeleton, clip, load))
                return;

            foreach (var posture in postures)
            {
                string idle = clip("loop_" + posture.State);
                if (posture.Posture != CreaturePosture.Upright && idle != null)
                    skeleton.Postures.Add(posture.Posture, load(idle));
                foreach (var next in postures)
                {
                    string transition = clip("trn_" + posture.State + "_to_" + next.State);
                    if (transition != null)
                        skeleton.Transitions.Add((posture.Posture, next.Posture), load(transition));
                }
            }
        }

        static void BindFallen(SkeletonData skeleton, Func<string, string> clip, string fallen, Func<string, SkeletalAnimation> load)
        {
            string fall = clip("trn_combat_standing_hit_to_incapacitated_face_up");
            foreach (var posture in new[] { CreaturePosture.Incapacitated, CreaturePosture.Dead, CreaturePosture.KnockedDown })
            {
                skeleton.Postures.Add(posture, load(fallen));
                if (fall != null)
                    skeleton.Transitions.Add((CreaturePosture.Upright, posture), load(fall));
            }
        }

        static bool TryBindTransport(SkeletonData skeleton, Func<string, string> clip, Func<string, SkeletalAnimation> load)
        {
            string sky = clip("loop_sky");
            string takeoff = clip("take_off");
            string land = clip("land");
            if (sky == null || takeoff == null || land == null)
                return false;

            // Core3 ShuttleDepartureTask uses Upright for grounded and Prone for departed.
            // The native transport LAT supplies the sky loop and complete flight/gear transitions.
            skeleton.Postures.Add(CreaturePosture.Prone, load(sky));
            skeleton.Transitions.Add((CreaturePosture.Upright, CreaturePosture.Prone), load(takeoff));
            skeleton.Transitions.Add((CreaturePosture.Prone, CreaturePosture.Upright), load(land));
            return true;
        }
    }
}
