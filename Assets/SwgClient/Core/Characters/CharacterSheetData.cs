using System;
using UnityEngine;

namespace Swg.Client.Core.Characters
{
    [Flags]
    public enum CharacterSheetChange
    {
        Status = 1,
        Personal = 2,
        Badges = 4,
        Migration = 8,
        All = Status | Personal | Badges | Migration
    }

    /// <summary>Owner-only character profile and consumption state, shared by the character sheet and other UI.</summary>
    public sealed class CharacterSheetData
    {
        public int Food;
        public int FoodMaximum;
        public int Drink;
        public int DrinkMaximum;
        public string Spouse = "";
        public string ResidencePlanet = "";
        public Vector3 ResidencePosition;
        public string CloningPlanet = "";
        public Vector3 CloningPosition;
        public string BankPlanet = "";
        public int LotsRemaining;
        public uint Faction;
        public uint FactionStatus;
        public int ImperialPoints;
        public int RebelPoints;
        public string Biography = "";
        public uint[] BadgeMasks = Array.Empty<uint>();
        public HamAttributes MigrationTargets;
        public int MigrationBudget;
        public bool HasMigrationTargets;
        public float FoodFullness => FoodMaximum > 0 ? Mathf.Clamp01((float)Food / FoodMaximum) : 0;
        public float DrinkFullness => DrinkMaximum > 0 ? Mathf.Clamp01((float)Drink / DrinkMaximum) : 0;
        public bool HasBadge(int index) => index >= 0 && index / 32 < BadgeMasks.Length && (BadgeMasks[index / 32] & (1u << (index % 32))) != 0;
        public event Action<CharacterSheetChange> Changed;
        internal void Update(Action<CharacterSheetData> update, CharacterSheetChange changes = CharacterSheetChange.All) { update(this); Changed?.Invoke(changes); }
    }
}
