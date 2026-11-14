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
            Wounds,
            Base,
            Encumbrance
        }

        public static void Apply(HamPoolUpdate update, ref HamPool ham)
        {
            ApplyVector(ref ham, update.Base, update.BaseDelta, ValueKind.Base);
            if (update.Encumbrances != null || update.EncumbrancesDelta != null)
            {
                var values = update.Encumbrances ?? new[] { ham.Strength.Encumbrance, ham.Quickness.Encumbrance, ham.Focus.Encumbrance };
                if (update.EncumbrancesDelta != null) values = update.EncumbrancesDelta.Apply(values);
                var expanded = new int[9];
                // Core3 PlayerManager::fixHAM subtracts each group penalty from its two secondary attributes.
                for (int i = 0; i < Math.Min(3, values.Length); i++)
                    expanded[i * 3 + 1] = expanded[i * 3 + 2] = values[i];
                Assign(ref ham, expanded, ValueKind.Encumbrance);
            }
            ApplyVector(ref ham, update.Current, update.CurrentDelta, ValueKind.Current);
            ApplyVector(ref ham, update.Maximum, update.MaximumDelta, ValueKind.Maximum);
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
                values[i] = kind switch { ValueKind.Current => attribute.Current, ValueKind.Maximum => attribute.Maximum, ValueKind.Wounds => attribute.Wounds, ValueKind.Base => attribute.Base, _ => attribute.Encumbrance };
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
                    kind == ValueKind.Wounds ? value : attribute.Wounds,
                    kind == ValueKind.Base ? value : attribute.Base,
                    kind == ValueKind.Encumbrance ? value : attribute.Encumbrance);
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
