namespace Swg.Client.Core.Networking.Game.Replication
{
    /// <summary>Partial HAM replication: absent vectors leave state unchanged; delta values replace entries, including zero.</summary>
    internal sealed class HamPoolUpdate
    {
        public int[] Base;
        public IntVectorDelta BaseDelta;
        public int[] Current;
        public IntVectorDelta CurrentDelta;
        public int[] Maximum;
        public IntVectorDelta MaximumDelta;
        public int[] Wounds;
        public IntVectorDelta WoundsDelta;
        public int[] Encumbrances;
        public IntVectorDelta EncumbrancesDelta;

        public bool HasChanges => Base != null || BaseDelta != null || Current != null || CurrentDelta != null
            || Maximum != null || MaximumDelta != null || Wounds != null || WoundsDelta != null
            || Encumbrances != null || EncumbrancesDelta != null;

        public HamPoolUpdate CopyForQueue()
        {
            var copy = (HamPoolUpdate)MemberwiseClone();
            copy.Base = Base == null ? null : (int[])Base.Clone();
            copy.Current = Current == null ? null : (int[])Current.Clone();
            copy.Maximum = Maximum == null ? null : (int[])Maximum.Clone();
            copy.Wounds = Wounds == null ? null : (int[])Wounds.Clone();
            copy.Encumbrances = Encumbrances == null ? null : (int[])Encumbrances.Clone();
            return copy;
        }
    }
}
