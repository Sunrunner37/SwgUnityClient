using System;

namespace Swg.Client.Core.Characters
{
    /// <summary>Nine named HAM attribute amounts, used for migration targets and species limits.</summary>
    public struct HamAttributes
    {
        public int Health;
        public int Strength;
        public int Constitution;
        public int Action;
        public int Quickness;
        public int Stamina;
        public int Mind;
        public int Focus;
        public int Willpower;
        public int Total => Health + Strength + Constitution + Action + Quickness + Stamina + Mind + Focus + Willpower;
        // Core3 attribute order is also the order of rows in the native migration window.
        public int this[int index]
        {
            get => index switch { 0 => Health, 1 => Strength, 2 => Constitution, 3 => Action, 4 => Quickness, 5 => Stamina, 6 => Mind, 7 => Focus, 8 => Willpower, _ => throw new ArgumentOutOfRangeException(nameof(index)) };
            set
            {
                switch (index)
                {
                    case 0: Health = value; break;
                    case 1: Strength = value; break;
                    case 2: Constitution = value; break;
                    case 3: Action = value; break;
                    case 4: Quickness = value; break;
                    case 5: Stamina = value; break;
                    case 6: Mind = value; break;
                    case 7: Focus = value; break;
                    case 8: Willpower = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }
        public static HamAttributes FromArray(int[] values)
        {
            if (values.Length != 9) throw new ArgumentException("Expected nine HAM attributes.", nameof(values));
            var result = new HamAttributes();
            for (int i = 0; i < 9; i++) result[i] = values[i];
            return result;
        }
        public int[] ToArray() { var result = new int[9]; for (int i = 0; i < 9; i++) result[i] = this[i]; return result; }
    }
}
