using System;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Core.Networking.Game.Replication
{
    /// <summary>Maps Core3's nine indexed attributes into the named application model.</summary>
    internal static class HamPoolReplication
    {
        enum ValueKind
        {
            Current,
            Maximum,
            Wounds
        }

        public static void Apply(CreatureSceneObjectUpdate update, ref HamPool ham)
        {
            ApplyVector(ref ham, update.Ham, update.HamDelta, ValueKind.Current);
            ApplyVector(ref ham, update.MaxHam, update.MaxHamDelta, ValueKind.Maximum);
            ApplyVector(ref ham, update.Wounds, update.WoundsDelta, ValueKind.Wounds);
        }

        static void ApplyVector(ref HamPool ham, int[] baseline, IntVectorDelta delta, ValueKind kind)
        {
            if (baseline != null)
                Assign(ref ham, baseline, kind);
            if (delta == null)
                return;
            var values = new int[9];
            for (int i = 0; i < values.Length; i++)
            {
                var attribute = At(ref ham, i);
                values[i] = kind == ValueKind.Current ? attribute.Current : kind == ValueKind.Maximum ? attribute.Maximum : attribute.Wounds;
            }
            Assign(ref ham, delta.Apply(values), kind);
        }

        static void Assign(ref HamPool ham, int[] values, ValueKind kind)
        {
            for (int i = 0; i < 9; i++)
            {
                int value = i < values.Length ? values[i] : 0;
                ref var attribute = ref At(ref ham, i);
                attribute = new HamElement(
                    kind == ValueKind.Current ? value : attribute.Current,
                    kind == ValueKind.Maximum ? value : attribute.Maximum,
                    kind == ValueKind.Wounds ? value : attribute.Wounds);
            }
        }

        // Wire indices belong here; application consumers use Health, Constitution, and so on.
        static ref HamElement At(ref HamPool ham, int index)
        {
            switch (index)
            {
                case 0: return ref ham.Health;
                case 1: return ref ham.Strength;
                case 2: return ref ham.Constitution;
                case 3: return ref ham.Action;
                case 4: return ref ham.Quickness;
                case 5: return ref ham.Stamina;
                case 6: return ref ham.Mind;
                case 7: return ref ham.Focus;
                case 8: return ref ham.Willpower;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
