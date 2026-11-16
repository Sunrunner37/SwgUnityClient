using System;

namespace Swg.Client.Core.Characters
{
    /// <summary>
    /// Approximate client estimate of Core3 EntertainingSession's private patron data.
    /// Each ten-second pulse and up to five flourishes per pulse add two minutes and the performance's healShockWound.
    /// The server pulse phase, skill bonuses, deny-service list and targeted-buff eligibility are not replicated.
    /// </summary>
    public sealed class EntertainerBuffEstimate
    {
        public const int PulseSeconds = 10;
        public const int MinimumSeconds = 60;
        public const int MaximumMinutes = 120; // Core3 stores duration in an int, including its 120 + 10/60 cap.
        public const int MasterStrength = 125;
        public double StartedAt { get; }
        public int Minutes { get; private set; }
        public int Strength { get; private set; }
        public int Flourishes { get; private set; }
        public int Contributions { get; private set; }
        int cycle;
        int cycleFlourishes;
        public EntertainerBuffEstimate(double startedAt) => StartedAt = startedAt;
        public double Elapsed(double now) => Math.Max(0, now - StartedAt);
        public bool MinimumReached(double now) => Elapsed(now) >= MinimumSeconds;
        public void Advance(double now, int strengthPerPulse, bool eligible)
        {
            int next = (int)(Elapsed(now) / PulseSeconds);
            if (next <= cycle) return;
            if (eligible) Add(next - cycle, strengthPerPulse);
            cycle = next;
            cycleFlourishes = 0;
        }
        public void Flourish(double now, int strengthPerPulse, bool eligible)
        {
            Advance(now, strengthPerPulse, eligible);
            Flourishes++;
            if (cycleFlourishes++ < 5 && eligible) Add(1, strengthPerPulse);
        }
        void Add(int count, int strengthPerPulse)
        {
            Contributions += count;
            Minutes = Math.Min(MaximumMinutes, Minutes + count * 2);
            Strength = Math.Min(MasterStrength, Strength + count * strengthPerPulse);
        }
        public double SecondsToFull(double now, int strengthPerPulse)
        {
            double remaining = Math.Max(Math.Ceiling((MaximumMinutes - Minutes) / 2.0),
                strengthPerPulse > 0 ? Math.Ceiling((double)(MasterStrength - Strength) / strengthPerPulse) : double.PositiveInfinity);
            double rate = Contributions / Math.Max(PulseSeconds, Elapsed(now));
            return Math.Max(Math.Max(0, MinimumSeconds - Elapsed(now)), remaining / Math.Max(1.0 / PulseSeconds, rate));
        }
    }
}
